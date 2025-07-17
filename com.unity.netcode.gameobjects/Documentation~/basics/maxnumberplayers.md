# Limiting the maximum number of players

Netcode for GameObjects provides a way to customize the [connection approval process](connection-approval.md) that can reject incoming connections based on any number of user-specific reasons.

<!-- Commenting this out until we can get external code references working

Boss Room provides one example of how to handle limiting the number of players through the connection approval process:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/ConnectionManagement/ConnectionState/HostingState.cs

```
-->

The code below shows an example of an over-capacity check that would prevent more than a certain pre-defined number of players from connecting.

```csharp
if( m_Portal.NetManager.ConnectedClientsIds.Count >= CharSelectData.k_MaxLobbyPlayers )
{
    return ConnectStatus.ServerFull;
}
```

> [!NOTE]​
> In connection approval delegate, Netcode for GameObjects doesn't support sending anything more than a Boolean back.

<!-- Commenting this out until we can get external code references working
> Boss Room shows a way to offer meaningful error code to the client by invoking a client RPC in the same channel that Netcode for GameObjects uses for its connection callback.
-->

When using Relay, ensure the maximum number of peer connections allowed by the host satisfies the logic implemented in the connection approval delegate.
