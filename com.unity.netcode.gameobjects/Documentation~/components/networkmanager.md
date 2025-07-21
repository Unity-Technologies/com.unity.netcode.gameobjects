# NetworkManager

The `NetworkManager` is a required Netcode for GameObjects component that has all of your project's netcode-related settings. Think of it as the central netcode hub for your netcode-enabled project.

## `NetworkManager` Inspector properties

- **LogLevel**:  Sets the network logging level
- **PlayerPrefab**:  When a Prefab is assigned, the Prefab will be instantiated as the player object and assigned to the newly connected and authorized client. For more information about player prefabs, refer to [Player NetworkObjects](../basics/networkobject.md#player-networkobjects).
- **NetworkPrefabs**: Where you register your network prefabs.  You can also create a single network Prefab override per registered network Prefab here.
- **Protocol Version**: Set this value to help distinguish between builds when the most current build has new assets that can cause issues with older builds connecting.
- **Network Transport**: Where your network specific settings and transport type is set.  This field accepts any INetworkTransport implementation.  However, unless you have unique transport specific needs UnityTransport is the recommended transport to use with Netcode for GameObjects.
- **Tick Rate**: This value controls the network tick update rate.
- **Ensure Network Variable Length Safety**: (Increases cpu processing and bandwidth) When this property is checked, Netcode for GameObjects will prevent user code from writing past the boundaries of a NetworkVariable.
- **Connection Approval**: This enables [connection approval](../basics/connection-approval.md) when this is checked and the `NetworkManager.ConnectionApprovalCallback` is assigned.
- **Client Connection Buffer Timeout**: This value sets the amount of time that has to pass for a connecting client to complete the connection approval process.  If the time specified is exceeded the connecting client will be disconnected.
- **Force Same Prefabs**: When checked it will always verify that connecting clients have the same registered network prefabs as the server.  When not checked, Netcode for GameObjects will ignore any differences.
- **Recycle Network Ids**: When checked this will re-use previously assigned `NetworkObject.NetworkObjectIds` after the specified period of time.
- **Network Id Recycle Delay**: The time it takes for a previously assigned but currently unassigned identifier to be available for use.
- **Enable Scene Management**: When checked, Netcode for GameObjects will handle scene management and client synchronization for you.  When not checked, you will have to create your own scene management scripts and handle client synchronization.
- **Load Scene Time Out**: When Enable Scene Management is checked, this specifies the period of time the `NetworkSceneManager` will wait while a scene is being loaded asynchronously before `NetworkSceneManager` considers the load/unload scene event to have failed/timed out.

## `NetworkManager` sub-systems
`NetworkManager` is also where you can find references to other Netcode related management systems:<br/>

> [!NOTE]
> All `NetworkManager` sub-systems are instantiated once the `NetworkManager` is started (that is, `NetworkManager.IsListening == true`).  A good general "rule of thumb" is to not attempt to access the below sub-systems before starting the `NetworkManager`, otherwise they won't yet be initialized.

- [NetworkManager.PrefabHandler](../advanced-topics/object-pooling.md): This provides access to the NetworkPrefabHandler that is used for NetworkObject pools and to have more control overriding network prefabs.
- [NetworkManager.SceneManager](../basics/scenemanagement/using-networkscenemanager.md): When scene management is enabled, this is used to load and unload scenes, register for scene events, and other scene management related actions.
- [NetworkManager.SpawnManager](../basics/object-spawning.md): This handles NetworkObject spawn related functionality.
- [NetworkManager.NetworkTimeSystem](../advanced-topics/networktime-ticks.md): a synchronized time that can be used to handle issues with latency between a client and the server.
- [NetworkManager.NetworkTickSystem](../advanced-topics/networktime-ticks.md#network-ticks): Use this to adjust the frequency of when NetworkVariables are updated.
- [NetworkManager.CustomMessagingManager](../advanced-topics/message-system/custom-messages.md): Use this system to create and send custom messages.

## Starting a server, host, or client

To perform any netcode-related action that involves sending messages, you must first have a server started and listening for connections with at least one client (_a server can send RPCs to itself when running as a host_) that is connected. To accomplish this, you must first start your `NetworkManager` as a server, host, or client. There are three `NetworkManager` methods you can invoke to accomplish this:

```csharp
NetworkManager.Singleton.StartServer();      // Starts the NetworkManager as just a server (that is, no local client).
NetworkManager.Singleton.StartHost();        // Starts the NetworkManager as both a server and a client (that is, has local client)
NetworkManager.Singleton.StartClient();      // Starts the NetworkManager as just a client.
```

> [!NOTE]
> Don't start a NetworkManager within a NetworkBehaviour's Awake method as this can lead to undesirable results depending upon your project's settings!

 When starting a Server or joining an already started session as client, the `NetworkManager` can spawn a "Player Object" belonging to the client. For more information about player prefabs, refer to:

 - [NetworkObject Player Prefab Documentation](../basics/networkobject.md)
 - [Connection Approval](../basics/connection-approval)

## Connecting

When starting a client, the `NetworkManager` uses the IP and the Port provided in your `Transport` component for connecting. While you can set the IP address in the editor, many times you might want to be able to set the IP address and port during runtime.

The below examples use [Unity Transport](https://docs.unity3d.com/Packages/com.unity.transport@latest?subfolder=/manual/index.html) to show a few ways you can gain access to the `UnityTransport` component via the `NetworkManager.Singleton` to configure your project's network settings programmatically:

If you are only setting the IP address and port number, then you can use the `UnityTransport.SetConnectionData` method:
```csharp
NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
    "127.0.0.1",  // The IP address is a string
    (ushort)12345 // The port number is an unsigned short
);
```

If you are using the same code block to configure both your server and your client and you want to configure your server to listen to all IP addresses assigned to it, then you can also pass a 'listen address' of "0.0.0.0" to the `SetConnectionData` method, like so:
```csharp
NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
    "127.0.0.1",  // The IP address is a string
    (ushort)12345, // The port number is an unsigned short
    "0.0.0.0" // The server listen address is a string.
);
```

> [!NOTE]
> Using an IP address of 0.0.0.0 for the server listen address will make a server or host listen on all IP addresses assigned to the local system. This can be particularly helpful if you are testing a client instance on the same system as well as one or more client instances connecting from other systems on your local area network. Another scenario is while developing and debugging you might sometimes test local client instances on the same system and sometimes test client instances running on external systems.

It is possible to access the current connection data at runtime, via `NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData`. This will return a [`ConnectionAddressData` **struct**](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/11922a0bc100a1615c541aa7298c47d253b74937/com.unity.netcode.gameobjects/Runtime/Transports/UTP/UnityTransport.cs#L239-L286), holding this info. You are strongly advised to use the `SetConnectionData` method to update this info.

If you are using Unity Relay to handle connections, however, **don't use `SetConnectionData`**. The host should call [`SetHostRelayData`](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/11922a0bc100a1615c541aa7298c47d253b74937/com.unity.netcode.gameobjects/Runtime/Transports/UTP/UnityTransport.cs#L575), and clients should call [`SetClientRelayData`](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/11922a0bc100a1615c541aa7298c47d253b74937/com.unity.netcode.gameobjects/Runtime/Transports/UTP/UnityTransport.cs#L588). Attempting to join a **Relay**-hosted game via entering IP/port number (via `SetConnectionData`) **won't work**.


[More information about Netcode for GameObjects Transports](../advanced-topics/transports.md)

## Disconnecting and shutting down

### Disconnect from a network

When you disconnect from a network you can't use or access any subsystems, for example `NetworkSceneManager`, because they become unavailable when `NetworkManager` stops. For client, host, or server modes, call the `NetworkManager.Shutdown` method to disconnect and shut down at the same time.

> [!NOTE]
> When no network session is active and the `NetworkManager` has been shutdown, you will need to use `UnityEngine.SceneManagement` to load any non-network session related scene.

```csharp
public void Disconnect()
{
    NetworkManager.Singleton.Shutdown();
    // At this point we must use the UnityEngine's SceneManager to switch back to the MainMenu
    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
}
```

### Shut down a network session

When you invoke `NetworkManager.Shutdown` it closes any existing transport connections. The shutdown sequence for a client finishes before the next player loop update or frame. However, the shutdown sequence can take a bit longer for a server or host (server-host).

The server-host notifies all clients when it shuts down. To do this, when you invoke `NetworkManager.Shutdown` the server-host sends out a disconnect notification to all currently connected clients and populates the `NetworkManager.Disconnect` reason with one of the two following messages, depending upon whether it's a server or host instance:

- "Disconnected due to server shutting down."
- "Disconnected due to host shutting down."

The server-host attempts to wait for all client connections to close before it finishes the shutdown sequence. This additional step on the server-host side helps to assure that clients receive a disconnect notification and prevents the client transport connection from timing out. If some client connections take longer than five seconds to close after `NetworkManager.Shutdown` is invoked, the server-host automatically finishes the shutdown sequence and closes any remaining connections.

## Disconnecting clients (server only)

At times you might need to disconnect a client for various reasons without shutting down the server. To do this, you can call the `NetworkManager.DisconnectClient` method while passing the identifier of the client you wish to disconnect as the only parameter.  The client identifier can be found within:

- The `NetworkManager.ConnectedClients` dictionary that uses the client identifier as a key and the value as the [`NetworkClient`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkClient.html).
- As a read only list of `NetworkClients`  via the `NetworkManager.ConnectedClientsList`.
- A full list of all connected client identifiers can be accessed via `NetworkManager.ConnectedClientsIds`.
- The client identifier is passed as a parameter to all subscribers of the `NetworkManager.OnClientConnected` event.
- The player's `NetworkObject` has the `NetworkObject.OwnerClientId` property.

> [!NOTE]
> One way to get a player's primary `NetworkObject` is via `NetworkClient.PlayerObject`.


```csharp
void DisconnectPlayer(NetworkObject player)
{
    // Note: If a client invokes this method, it will throw an exception.
    NetworkManager.DisconnectClient(player.OwnerClientId);
}
```

### Client disconnection notifications

Both the client and the server can subscribe to the `NetworkManager.OnClientDisconnectCallback` event to be notified when a client is disconnected.

**When disconnect notifications are triggered:**
- Clients are notified when they're disconnected by the server.
- The server is notified when any client disconnects from the server, whether the server disconnects the client or the client disconnects itself.
- Both the server and clients are notified when their network connection is unexpectedly disconnected (network interruption).

**Client notification identifiers**
- On the server-side, the client identifier parameter is the identifier of the client that disconnects.
- On the client-side, the client identifier parameter is the identifier assigned to the client.
  - _The exception to this is when a client is disconnected before its connection is approved._

You can also use the `NetworkManager.OnServerStopped` and `NetworkManager.OnClientStopped` callbacks to get local notifications when the server or client stops respectively.

### Connection notification manager example
Below is one example of how you can provide client connect and disconnect notifications to any type of NetworkBehaviour or MonoBehaviour derived component.

> [!NOTE]
> The `ConnectionNotificationManager` example below should only be attached to the same GameObject as `NetworkManager` to assure it persists as long as the `NetworkManager.Singleton` instance.

```csharp
using System;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Only attach this example component to the NetworkManager GameObject.
/// This will provide you with a single location to register for client
/// connect and disconnect events.
/// </summary>
public class ConnectionNotificationManager : MonoBehaviour
{
    public static ConnectionNotificationManager Singleton { get; internal set; }

    public enum ConnectionStatus
    {
        Connected,
        Disconnected
    }

    /// <summary>
    /// This action is invoked whenever a client connects or disconnects from the game.
    ///   The first parameter is the ID of the client (ulong).
    ///   The second parameter is whether that client is connecting or disconnecting.
    /// </summary>
    public event Action<ulong, ConnectionStatus> OnClientConnectionNotification;

    private void Awake()
    {
        if (Singleton != null)
        {
            // As long as you aren't creating multiple NetworkManager instances, throw an exception.
            // (***the current position of the callstack will stop here***)
            throw new Exception($"Detected more than one instance of {nameof(ConnectionNotificationManager)}! " +
                $"Do you have more than one component attached to a {nameof(GameObject)}");
        }
        Singleton = this;
    }

    private void Start()
    {
        if (Singleton != this){
            return; // so things don't get even more broken if this is a duplicate >:(
        }

        if (NetworkManager.Singleton == null)
        {
            // Can't listen to something that doesn't exist >:(
            throw new Exception($"There is no {nameof(NetworkManager)} for the {nameof(ConnectionNotificationManager)} to do stuff with! " +
                $"Please add a {nameof(NetworkManager)} to the scene.");
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    private void OnDestroy()
    {
        // Since the NetworkManager can potentially be destroyed before this component, only
        // remove the subscriptions if that singleton still exists.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Connected);
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Disconnected);
    }
}
```
