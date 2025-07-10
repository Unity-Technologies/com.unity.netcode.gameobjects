# Connection events

When you need to react to connection or disconnection events for yourself or other clients, you can use `NetworkManager.OnConnectionEvent` as a unified source of information about changes in the network.

`OnConnectionEvent` receives a `ConnectionEventData` struct detailing the relevant information about the connection state change:

```csharp
public enum ConnectionEvent
{
    ClientConnected,
    PeerConnected,
    ClientDisconnected,
    PeerDisconnected
}

public struct ConnectionEventData
{
    public ConnectionEvent EventType;
    public ulong ClientId;
    public NativeArray<ulong> PeerClientIds;
}
```

There are four types of event you can receive. The events are the same for both clients and servers/hosts, but they indicate slightly different things depending on the context.

|Event   |Server or host   |Client   |
|---|---|---|
|`ConnectionEvent.ClientConnected`   |Indicates that a new client has connected. The ID of the newly connected client is `ClientId`. `PeerClientIds` is uninitialized and shouldn't be used.|Indicates that the local client has completed its connection to the server. The ID of the client is `LocalClientId`, and `PeerClientIds` contains a list of client IDs of other clients currently connected to the server.|
|`ConnectionEvent.PeerConnected`     |Not applicable for servers. For host clients running in host mode, works the same as for clients.|Indicates that another client has connected to the server. The ID of the newly connected client is `ClientId`. `PeerClientIds` is uninitialized and shouldn't be used. |
|`ConnectionEvent.ClientDisconnected`|Indicates that a client has disconnected. The ID of the disconnected client is `ClientId`. `PeerClientIds` is uninitialized and shouldn't be used.|Indicates that the local client has disconnected from the server. The ID of the client is `LocalClientId`. `PeerClientIds` is uninitialized and shouldn't be used.    |
|`ConnectionEvent.PeerDisconnected`  |Not applicable for servers. For host clients running in host mode, works the same as for clients.| Indicates that another client has disconnected from the server. The ID of the disconnected client is `ClientId`. `PeerClientIds` is uninitialized and shouldn't be used.|
