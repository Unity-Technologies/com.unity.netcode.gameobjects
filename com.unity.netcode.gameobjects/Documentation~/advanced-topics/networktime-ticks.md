# NetworkTime and ticks

## LocalTime and ServerTime

Why are there two different time values and which one should be used?

Netcode for GameObjects (Netcode) uses a star topology. That means all communications happen between the clients and the server/host and never between clients directly. Messages take time to transmit over the network. That's why `RPCs` and `NetworkVariable` won't happen immediately on other machines. `NetworkTime` allows to use time while considering those transmission delays.

- `LocalTime` on a client is ahead of the server. If a server RPC is sent at `LocalTime` from a client it will roughly arrive at `ServerTime` on the server.
- `ServerTime` on clients is behind the server. If a client RPC is sent at `ServerTime` from the server to clients it will roughly arrive at `ServerTime` on the clients.



<Mermaid chart={`
sequenceDiagram
    participant Owner as Client LocalTime
    participant Server as Server ServerTime & LocalTime
    participant Receiver as Client ServerTime
    Note over Owner: Send message to server at LocalTime.
    Owner->>Server: Delay when sending message
    Note over Server: Message arrives at ServerTime.
    Note over Server: On server: ServerTime == LocalTime.
    Note over Server: Send message to clients at LocalTime.
    Server->>Receiver: Delay when sending message
    Note over Receiver: Message arrives at ServerTime.
`}/>




`LocalTime`
- Use for player objects with client authority.
- Use if just a general time value is needed.

`ServerTime`:
- For player objects with server authority (For example, by sending inputs to the server via RPCs)
- In sync with position updates of `NetworkTransform` for all `NetworkObjects` where the client isn't authoritative over the transform.
- For everything on non client controlled `NetworkObjects`.

## Examples

### Example 1: Using network time to synchronize environments

Many games have environmental objects which move in a fixed pattern. By using network time these objects can be moved without having to synchronize their positions with a `NetworkTransform`.

For instance the following code can be used to create a moving elevator platform for a client authoritative game:

```csharp
using Unity.Netcode;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public void Update()
    {
        // Move up and down by 5 meters and change direction every 3 seconds.
        var positionY = Mathf.PingPong(NetworkManager.Singleton.LocalTime.TimeAsFloat / 3f, 1f) * 5f;
        transform.position = new Vector3(0, positionY, 0);
    }
}
```

### Example 2: Using network time to create a synced event

Most of the time aligning an effect precisely to time isn't needed. But in some cases for important effects or gameplay events it can help to improve consistency especially for clients with bad network connections.

```csharp
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class SyncedEventExample : NetworkBehaviour
{
    public GameObject ParticleEffect;

    // Called by the client to create a synced particle event at its own position.
    public void ClientCreateSyncedEffect()
    {
        Assert.IsTrue(IsOwner);
        var time = NetworkManager.LocalTime.Time;
        CreateSyncedEffectServerRpc(time);
        StartCoroutine(WaitAndSpawnSyncedEffect(0)); // Create the effect immediately locally.
    }

    private IEnumerator WaitAndSpawnSyncedEffect(float timeToWait)
    {
        // Note sometimes the timeToWait will be negative on the server or the receiving clients if a message got delayed by the network for a long time. This usually happens only in rare cases. Custom logic can be implemented to deal with that scenario.
        if (timeToWait > 0)
        {
            yield return new WaitForSeconds(timeToWait);
        }

        Instantiate(ParticleEffect, transform.position, Quaternion.identity);
    }

    [Rpc(SendTo.Server)]
    private void CreateSyncedEffectServerRpc(double time)
    {
        CreateSyncedEffectClientRpc(time); // Call a client RPC to also create the effect on each client.
        var timeToWait = time - NetworkManager.ServerTime.Time;
        StartCoroutine(WaitAndSpawnSyncedEffect((float)timeToWait)); // Create the effect on the server but wait for the right time.
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void CreateSyncedEffectClientRpc(double time)
    {
        // The owner already created the effect so skip them.
        if (IsOwner == false)
        {
            var timeToWait = time - NetworkManager.ServerTime.Time;
            StartCoroutine(WaitAndSpawnSyncedEffect((float)timeToWait)); // Create the effect on the client but wait for the right time.
        }
    }
}
```

<Mermaid chart={`
sequenceDiagram
    participant Owner as Owner
    participant Server as Server
    participant Receiver as Other Client
    Note over Owner: LocalTime = 10.0
    Note over Owner: ClientCreateSyncedEffect()
    Note over Owner: Instantiate effect immediately (LocalTime = 10)
    Owner->>Server: CreateSyncedEffectServerRpc
    Server->>Receiver: CreateSyncedEffectClientRpc
    Note over Server: ServerTime = 9.95 #38; timeToWait = 0.05
    Note over Server: StartCoroutine(WaitAndSpawnSyncedEffect(0.05))
    Server->>Server: WaitForSeconds(0.05);
    Note over Server: Instantiate effect at ServerTime = 10.0
    Note over Receiver: ServerTime = 9.93 #38; timeToWait = 0.07
    Note over Receiver: StartCoroutine(WaitAndSpawnSyncedEffect(0.07))
    Receiver->>Receiver: WaitForSeconds(0.07);
    Note over Receiver: Instantiate effect at ServerTime = 10.0
`}/>

> [!NOTE]
> Some components such as `NetworkTransform` add additional buffering. When trying to align an RPC event like in this example, an additional delay would need to be added.

## Network Ticks

Network ticks are run at a fixed rate. The 'Tick Rate' field on the `NetworkManager` can be used to set the tick rate.

What does changing the network tick affect? Changes to `NetworkVariables` aren't sent immediately. Instead during each network tick changes to `NetworkVariables` are collected and sent out to other peers.

To run custom code once per network tick (before `NetworkVariable` changes are collected) the `Tick` event on the `NetworkTickSystem` can be used.
```cs
public override void OnNetworkSpawn()
{
    NetworkManager.NetworkTickSystem.Tick += Tick;
}

private void Tick()
{
    Debug.Log($"Tick: {NetworkManager.LocalTime.Tick}");
}

public override void OnNetworkDespawn() // don't forget to unsubscribe
{
    NetworkManager.NetworkTickSystem.Tick -= Tick;
}
```

> [!NOTE]
> When using `FixedUpdate` or physics in your game, set the network tick rate to the same rate as the fixed update rate. The `FixedUpdate` rate can be changed in `Edit > Project Settings > Time Fixed Timestep`.

## Network FixedTime

`Network FixedTime` can be used to get a time value representing the time during a network tick. This works similar to `FixedUpdate` where `Time.fixedTime` represents the time during the `FixedUpdate`.

```cs
public void Update()
{
    double time = NetworkManager.Singleton.LocalTime.Time; // time during this Update
    double fixedTime = NetworkManager.Singleton.LocalTime.FixedTime; // time during the previous network tick
}
```

## NetworkTime Precision

Network time values are calculated using double precisions. This allows time to stay accurate on long running servers. For game servers which run sessions for a long time (multiple hours or days) don't convert this value in a float and always use doubles for time related calculations.

For games with short play sessions casting the time to float is safe or `TimeAsFloat` can be used.

## NetworkTimeSystem Configuration

> [!NOTE]
> The properties of the `NetworkTimeSystem` should be left untouched on the server/host. Changing the values on the client is sufficient to change the behavior of the time system.

The way network time gets calculated can be configured in the `NetworkTimeSystem` if needed. Refer to the [API docs](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkTimeSystem.html) for information about the properties which can be modified. All properties can be safely adjusted at runtime. For instance, buffer values can be increased for a player with a bad connection.

<!-- On page code -->
