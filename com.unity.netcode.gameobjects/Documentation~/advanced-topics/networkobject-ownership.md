# NetworkObject ownership

Before reading these docs, familiarize yourself with the concepts of [ownership](../terms-concepts/ownership.md) and [authority](../terms-concepts/authority.md) within Netcode for GameObjects. It's also recommended to read the documentation on the [NetworkObject](../components/core/networkobject.md).

To see if the local client is the owner of a NetworkObject, you can check the[`NetworkObject.IsOwner`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkObject.IsOwner.html) or the [`NetworkBehaviour.IsOwner`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.IsOwner.html) property.

To see if the server owns a NetworkObject, you can check the [`NetworkObject.IsOwnedByServer`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkObject.IsOwnedByServer.html) or the [`NetworkBehaviour.IsOwnedByServer`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.IsOwnedByServer.html) property.

> [!NOTE]
> When you want a specific NetworkObject to keep existing after the owner leaves the session, you can set the `NetworkObject.DontDestroyWithOwner` property to `true`. This ensures that the owned NetworkObject isn't destroyed as the owner leaves.

## Authority ownership

If you're creating a client-server game and you want a client to control more than one NetworkObject, or if you're creating a distributed authority game and the authority/current owner of the object would like to change ownership, use the following ownership methods.

The default `NetworkObject.Spawn` method will set server-side ownership when using a client-server topology. When using a distributed authority topology, this method will set the client who calls the method as the owner.

```csharp
GetComponent<NetworkObject>().Spawn();
```

To change ownership, the authority uses the `ChangeOwnership` method:

```csharp
GetComponent<NetworkObject>().ChangeOwnership(clientId);
```

## Client-server ownership

In a client-server game, to give ownership back to the server use the `RemoveOwnership` method:

```csharp
GetComponent<NetworkObject>().RemoveOwnership();
```

> [!NOTE]
> `RemoveOwnership` isn't supported when using a [distributed authority network topology](../../terms-concepts/distributed-authority.md).

## Distributed authority ownership

When building a distributed authority game, it is important to remember that owner of an object is always the authority for that object. As the simulation is shared between clients, it is important that ownership can be easily passed between game clients. This enables:

1. Evenly sharing the load of the game simulation via redistributing objects whenever a player joins or leaves.
2. Allowing ownership to be transferred immediately to a client in situations where a client is interacting with that object and so wants to remove lag from those interactions.
3. Controlling when and object is safe and/or valid to be transferred.

The authority of any object can always change ownership as outlined in [authority ownership](#authority-ownership).

### Ownership permissions settings

The following ownership permission settings, defined by [`NetworkObject.OwnershipStatus`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkObject.OwnershipStatus.html), control how ownership of objects can be changed during a distributed authority session:

| **Ownership setting** | **Description** |
|-------------------|-------------|
| `None` | Ownership of this NetworkObject can't be redistributed, requested, or transferred (a Player might have this, for example). |
| `Distributable` | Ownership of this NetworkObject is automatically redistributed when a client joins or leaves, as long as ownership is not locked or a request is pending. |
| `Transferable` | Any client can change ownership of this NetworkObject at any time, as long as ownership is not locked or a request is pending. |
| `RequestRequired` | Ownership of this NetworkObject must be requested before ownership can be changed. |
| `SessionOwner` | This NetworkObject is always owned by the [session owner](distributed-authority.md#session-ownership) and can't be transferred or distributed. If the session owner changes, this NetworkObject is automatically transferred to the new session owner. |

You can also use `NetworkObject.SetOwnershipLock` to lock and unlock the permission settings of a NetworkObject for a period of time, preventing ownership changes on a temporary basis.

```csharp
// To lock an object from any ownership changes
GetComponent<NetworkObject>().SetOwnershipLock(true);

// To unlock an object so that the underlying ownership permissions apply again
GetComponent<NetworkObject>().SetOwnershipLock(false);
```

### Changing ownership in distributed authority

When a NetworkObject is set with `OwnershipPermissions.Transferable` any client can change ownership to any other client using the `ChangeOwnership` method:

```csharp
GetComponent<NetworkObject>().ChangeOwnership(clientId);

// To change ownership to self
GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.Singleton.LocalClientId);
```

When a non-authoritative game client calls `ChangeOwnership`, the ownership change can fail. On a failed attempt to change ownership, the `OnOwnershipPermissionsFailure` callback will be invoked with a [`NetworkObject.OwnershipPermissionsFailureStatus`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkObject.OwnershipPermissionsFailureStatus.html) to give information on the failure.

```csharp
internal class MyTransferrableBehaviour : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        NetworkObject.OnOwnershipPermissionsFailure = OnOwnershipPermissionsFailure;
        base.OnNetworkSpawn();
    }

    private void OnOwnershipPermissionsFailure(NetworkObject.OwnershipPermissionsFailureStatus ownershipPermissionsFailureStatus)
    {
        // Called on the calling client when NetworkObject.ChangeOwnership() has failed
    }

    public override void OnNetworkDespawn()
    {
        NetworkObject.OnOwnershipPermissionsFailure = null;
        base.OnNetworkDespawn();
    }
}
```

### Requesting ownership in distributed authority

When a NetworkObject is set with `OwnershipPermissions.RequestRequired` any client can request the ownership for themselves using the `RequestOwnership` method:

```csharp
var requestStatus = GetComponent<NetworkObject>().RequestOwnership();
```

The `RequestOwnership` will return an [`OwnershipRequestStatus`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkObject.OwnershipRequestStatus.html) to indicate the initial status of the request. To view the result of the request, `OnOwnershipRequestResponse` callback will be invoked with a [`NetworkObject.OwnershipRequestResponseStatus`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkObject.OwnershipRequestResponseStatus.html).

```csharp
internal class MyRequestableBehaviour : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        NetworkObject.OnOwnershipRequestResponse = OnOwnershipRequestResponse;
        base.OnNetworkSpawn();
    }

    private void OnOwnershipRequestResponse(NetworkObject.OwnershipRequestResponseStatus ownershipRequestResponseStatus)
    {
        // Called when the requesting client has gotten a response to their request
    }

    public override void OnNetworkDespawn()
    {
        NetworkObject.OnOwnershipRequestResponse = null;
        base.OnNetworkDespawn();
    }
}
```

## Spawn with ownership

To spawn a `NetworkObject` that is [owned](../../terms-concepts/ownership.md) by a different game client than the one doing the spawning, use the following:

```csharp
GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
```

> [!NOTE]
> Using `SpawnWithOwnership` can result in unexpected behaviour when the spawning game client makes any other changes on the object immediately after spawning.

Using `SpawnWithOwnership` and then editing the object locally will mean the client doing the spawning will behave as the "spawn authority". The spawn authority will have limited local [authority](../terms-concepts/authority.md) over the object, but will not have [ownership](../terms-concepts/ownership.md) of the object that is spawned. This means any owner-specific checks during the spawn sequence will not be invoked on the spawn authority side.

Any time you would like to spawn an object for another client and then immediately make adjustments on that object, it's instead recommended to use `Spawn`. After adjusting, the spawn authority can immediately follow with a call to `ChangeOwnership`.

```csharp
[Rpc(SendTo.Authority)]
void SpawnForRequestingPlayer(RpcParams rpcParams = default) {
  var instance = Instantiate(myprefab).GetComponent<NetworkObject>();
  instance.Spawn();
  instance.transform.position = transform.position;
  instance.GetComponent<MyNetworkBehaviour>.MyNetworkVar.Value = initialValue;
  instance.ChangeOwnership(rpcParams.Receive.SenderClientId)
}
```

This flow allows the spawning client to completely spawn and finish initializing the object locally before transferring the ownership to another game client.
