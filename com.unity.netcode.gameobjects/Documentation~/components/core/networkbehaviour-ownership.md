# NetworkBehaviour ownership

Before reading these docs, ensure you understand the concepts of [ownership](../terms-concepts/ownership.md) and [NetworkObject ownership](./networkobject-ownership.md). It's also important to be familiar with the [NetworkBehaviour](./networkbehaviour.md)

The owner of each NetworkBehaviour in your game is decided by the owner of that NetworkBehaviour's NetworkObject. The NetworkObject is found as a property on the NetworkBehaviour: [`NetworkBehaviour.NetworkObject`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html#Unity_Netcode_NetworkBehaviour_NetworkObject).

## Helpful properties

> [!NOTE]
> The following properties are only valid if the NetworkBehaviour has been spawned. Use [`NetworkBehaviour.IsSpawned`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html#Unity_Netcode_NetworkBehaviour_IsSpawned) to check the spawned status of the NetworkBehaviour

To identify whether the local client is the owner of a NetworkBehaviour, you can check the[`NetworkBehaviour.IsOwner`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html#Unity_Netcode_NetworkBehaviour_IsOwner) property.

To identify whether the server owns a NetworkBehaviour, you can check the [`NetworkBehaviour.IsOwnedByServer`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html#Unity_Netcode_NetworkBehaviour_IsOwnedByServer) property.

To identify whether the local client has authority of a NetworkBehaviour, you can check the[`NetworkBehaviour.HasAuthority`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html#Unity_Netcode_NetworkBehaviour_HasAuthority) property.

## Detecting ownership changes

There are three functions that can be implemented to detect ownership changes on a NetworkBehaviour. These functions are invoked in the order they are listed here.

### [OnLostOwnership](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html#Unity_Netcode_NetworkBehaviour_OnLostOwnership)

When using a [client-server network topology](../../terms-concepts/client-server.md) `OnLostOwnership`  is invoked on both the server any time a connected client loses ownership of this NetworkBehaviour. It is also invoked on the game client who just lost ownership.

In a [distributed authority network topology](../../terms-concepts/distributed-authority.md) `OnLostOwnership` is invoked on all connected game clients.

```csharp
void OnLostOwnership()
{
    var newOwner = OwnerClientId;

    // Take action on lost ownership here
}
```

### [OnGainedOwnership](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html#Unity_Netcode_NetworkBehaviour_OnGainedOwnership)

When using a client-server network topology `OnGainedOwnership` is invoked on the server any time ownership is gained. It is also be invoked on the game client who just gained ownership.

In a distributed authority network topology `OnGainedOwnership` is invoked on all connected game clients.

`OnGainedOwnership` is invoked after `OnLostOwnership`.

```csharp
void OnGainedOwnership()
{
    var newOwner = OwnerClientId;
    var newOwnerIsMe = IsOwner;

    // Take action on ownership gain here
}
```

### [OnOwnershipChanged](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html#Unity_Netcode_NetworkBehaviour_OnOwnershipChanged_System_UInt64_System_UInt64_)

Whenever you want notification on any and all ownership changes, implement the `OnOwnershipChanged` method. `OnOwnershipChanged` is invoked on all connected game clients whenever the ownership of the NetworkBehaviour it is implemented on changes.

`OnOwnershipChanged` is invoked after `OnLostOwnership` and `OnGainedOwnership`.

```csharp
void OnOwnershipChanged(ulong previousOwnerId, ulong currentOwnerId)
{
    var newOwnerIsMe = IsOwner;

    // Take action on ownership change here
}
```
