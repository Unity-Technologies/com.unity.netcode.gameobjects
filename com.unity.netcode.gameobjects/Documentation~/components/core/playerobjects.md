# PlayerObjects and player prefabs

PlayerObjects are an optional feature in Netcode for GameObjects that you can use to assign a [NetworkObject](networkobject.md) to a specific client. A client can only have one PlayerObject.

PlayerObjects are instantiated by reference to a player prefab, which defines the characteristics of the PlayerObject.

## Defining defaults for player prefabs

If you're using `UnityEngine.InputSystem.PlayerInput` or `UnityEngine.PhysicsModule.CharacterController` components on your player prefab(s), you should disable them by default and only enable them for the local client's PlayerObject. Otherwise, you may get events from the most recently instantiated player prefab instance, even if it isn't the local client instance.

You can disable these components in the **Inspector** view on the prefab itself, or disable them during `Awake` in one of your NetworkBehaviour components. Then you can enable the components only on the owner's instance using code like the example below:

```csharp
PlayerInput m_PlayerInput;
private void Awake()
{
    m_PlayerInput = GetComponent<PlayerInput>();
    m_PlayerInput.enabled = false;
}

public override void OnNetworkSpawn()
{
    m_PlayerInput.enabled = IsOwner;
    base.OnNetworkSpawn();
}

public override void OnNetworkDespawn()
{
    m_PlayerInput.enabled = false;
    base.OnNetworkDespawn();
}
```

## Spawning PlayerObjects

## Session-mode agnostic methods

Netcode for GameObjects can spawn a default PlayerObject for you. If you enable **Create Player Prefab** in the [NetworkManager](networkmanager.md) and assign a valid prefab, then Netcode for GameObjects spawns a unique instance of the designated player prefab for each connected and approved client, referred to as the PlayerObject.

To manually spawn an object as PlayerObject, use the following method:

```csharp
GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
```

If the player already had a prefab instance assigned, then the client owns the NetworkObject of that prefab instance unless there's additional server-side specific user code that removes or changes the ownership.

Alternatively, you can choose not to spawn anything immediately after a client connects and instead use a [NetworkBehaviour component](networkbehaviour.md) to handle avatar/initial player prefab selection. This NetworkBehaviour component could be configured by the server or initiating session owner, or be associated with an [in-scene](../../basics/scenemanagement/inscene-placed-networkobjects.md) or [dynamically spawned](../../basics/object-spawning.md#dynamically-spawned-network-prefabs) NetworkObject, as suits the needs of your project.

### Client-server contexts only

In addition to the [session-mode agnostic spawning methods](#session-mode-agnostic-methods) above, you can assign a unique player prefab on a per-client connection basis using a client [connection approval process](../../basics/connection-approval.md) when in [client-server contexts](../../terms-concepts/client-server.md).

### Distributed authority contexts only

In addition to the [session-mode agnostic spawning methods](#session-mode-agnostic-methods) above, you can use the [`OnFetchLocalPlayerPrefabToSpawn`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkManager.html#Unity_Netcode_NetworkManager_OnFetchLocalPlayerPrefabToSpawn) method to assign a unique player prefab on a per-client basis when in [distributed authority contexts](../../terms-concepts/distributed-authority.md).

To use `OnFetchLocalPlayerPrefabToSpawn` in your project, assign a callback handler to `OnFetchLocalPlayerPrefabToSpawn` and whatever the client script returns is what will be spawned for that client. Ensure that the prefab being spawned is in a NetworkPrefabList [registered with the NetworkManager](../../basics/object-spawning.md#registering-a-network-prefab).

If you don't assign a callback handler to `OnFetchLocalPlayerPrefabToSpawn`, then the default behavior is to return the `NetworkConfig.PlayerPrefab` (or null if neither are set).

:::note `AutoSpawnPlayerPrefabClientSide` required
For `OnFetchLocalPlayerPrefabToSpawn` to work, [`AutoSpawnPlayerPrefabClientSide`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkManager.html#Unity_Netcode_NetworkManager_AutoSpawnPlayerPrefabClientSide) must be enabled.
:::

## PlayerObject spawning timeline

When using automatic methods of PlayerObject spawning (such as enabling **Create Player Prefab** in the [NetworkManager](networkmanager.md)), PlayerObjects are spawned at different times depending on whether you have [scene management](../../basics/scenemanagement/scene-management-overview.md) enabled.

- When scene management is disabled, PlayerObjects are spawned when a joining client's connection is approved.
- When scene management is enabled, PlayerObjects are spawned when a joining client finishes initial synchronization.

If you choose not to automatically spawn a PlayerObject when a client joins, then the timing of when a PlayerObject is spawned is up to you based on your own implemented code.

## Finding PlayerObjects

To find a PlayerObject for a specific client ID, you can use the following methods:

Within a NetworkBehaviour, you can check the local `NetworkManager.LocalClient` to get the local PlayerObjects:

```csharp
NetworkManager.LocalClient.PlayerObject
```

Conversely, on the server-side, if you need to get the PlayerObject instance for a specific client, you can use the following:

```csharp
NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
```

To find your own player object just pass `NetworkManager.Singleton.LocalClientId` as the `clientId` in the sample above.

## Additional resources

- [NetworkObject](networkobject.md)
- [NetworkManager](networkmanager.md)
- [Distributed authority topologies](../../terms-concepts/distributed-authority.md)
- [Client-server topologies](../../terms-concepts/client-server.md)
- [Object spawning](../../basics/object-spawning.md)
