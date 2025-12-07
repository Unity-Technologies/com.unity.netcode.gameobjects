# Spawning synchronization

Ensuring that objects spawn in a synchronized manner across clients can be difficult, although the challenges differ depending on which [network topology](../terms-concepts/network-topologies.md) you're using.

## Spawning synchronization in client-server

Due to the restrictive nature of only the server having the authority to spawn objects, visual anomalies between clients are common in [client-server](../terms-concepts/client-server.md) contexts. This typically occurs when using an owner-authoritative motion model, also known as a ClientNetworkTransform, and trying to visually synchronize the spawning of an object relative to a local player's current position (for example, a player shooting a projectile).

![Client server spawn synchronization](../images/client-server-spawn-sync.jpg)

In the diagram above, a client-owned object (a player) is in motion while firing a projectile. The code implementing this behavior involves sending a message (typically an [RPC](../advanced-topics/messaging-system.md)) from the client to the server to get an object to spawn based on the player's input. Due to the latency involved in sending this message to the server and then propagating the resulting spawn message across all clients, including back to the initiating client, the spawn projectile message reaches the initiating client after the client's local player has moved, resulting in a visual disconnect between the local player and the projectile.

These types of issues are commonly resolved using a client prediction system or by separating the visual representation of the projectile from the NetworkObject and writing a script that moves the visual representation in the desired direction of the yet-to-be-spawned NetworkObject and then synchronizing the two over time once the NetworkObject is spawned. Both approaches are complex to implement in a client-server topology.

## Spawning synchronization in distributed authority

Managing spawning synchronization is easier in [distributed authority](../terms-concepts/distributed-authority.md) contexts, because the authority to spawn objects is shared between clients. Instead of a client having to send an RPC to the server, wait roughly the round trip time (RTT) between itself and the server-host, and then receive and process the spawn message, the client just spawns the projectile in the precise position desired relative to the player object, as explained in the diagram below.

![Distributed authority spawn synchronization](../images/distributed-authority-spawn-sync.jpg)

Handling this visual synchronization issue is similar to the traditional Unity development flow, with one additional step:

1. Instantiate a (network) prefab instance.
2. Position/Configure the instantiated prefab instance.
3. (New step) Spawn the prefab instance.

Transform changes on non-authoritative instances are network tick synchronized by the distributed authority service. This allows for remote-client cloned object instances to be visually synchronized with other spawned objects across all clients, since consumption and processing of transform state updates are processed relative to each clients' currently known network tick value.

Distributed authority also provides a simpler way to visually synchronize the despawning of NetworkObjects (relative to a client-server session). Refer to the [deferred despawning page](deferred-despawning.md) for more details.

## Examples

Below are two example scripts, one that handles spawning using the client-server mode, and one that uses distributed authority mode.

* The client-server script requires splitting the logic between client and server. The client has to send a message to the server in order to spawn the projectile, which introduces the latency previously described between a player's motion and the projectile being spawned.
* With distributed authority, spawning follows a more traditional single-player usage pattern. The notable difference from the client-server approach is that there's no need to send a message to a server in order to spawn the NetworkObject.

### Client-server

```
/// <summary>
/// Client-Server Spawning
/// Pseudo player weapon that is assumed to be
/// attached to a fixed child node of the player.
/// (i.e. like a hand node or the like)
/// </summary>
public class PlayerWeapon : NetworkBehaviour
{
    // For example purposes, the projectile to spawn
    public GameObject Projectile;


    // For example purposes, an offset from the weapon
    // to spawn the projectile.
    public GameObject WeaponFiringOffset;


    public void FireWeapon()
    {
        if (!IsOwner)
        {
            return;
        }


        if (IsServer)
        {
            OnFireWeapon();
        }
        else
        {
            OnFireWeaponServerRpc();
        }
    }


    [ServerRpc]
    private void OnFireWeaponServerRpc()
    {
        OnFireWeapon();
    }


    private void OnFireWeapon()
    {
        var instance = Instantiate(Projectile);
        var instanceNetworkObject = instance.GetComponent<NetworkObject>();
        instance.transform.position = WeaponFiringOffset.transform.position;
        instance.transform.forward = transform.forward;
        instanceNetworkObject.Spawn();
    }
}
```

### Distributed authority

```
/// <summary>
/// Pseudo player weapon that is assumed to be
/// attached to a fixed child node of the player.
/// (i.e. like a hand node or the like)
/// </summary>
public class PlayerWeapon : NetworkBehaviour
{
    // For example purposes, the projectile to spawn
    public GameObject Projectile;


    // For example purposes, an offset from the weapon
    // to spawn the projectile.
    public GameObject WeaponFiringOffset;


    public void FireWeapon()
    {
        var instance = Instantiate(Projectile);
        var instanceNetworkObject = instance.GetComponent<NetworkObject>();
        instance.transform.position = WeaponFiringOffset.transform.position;
        instance.transform.forward = transform.forward;
        instanceNetworkObject.Spawn();
    }
}
```
