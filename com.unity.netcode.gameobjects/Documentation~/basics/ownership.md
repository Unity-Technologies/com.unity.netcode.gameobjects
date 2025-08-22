# Understanding ownership and authority

By default, Netcode for GameObjects assumes a [client-server topology](../terms-concepts/client-server.md), in which the server owns all NetworkObjects (with [some exceptions](../components/core/networkobject.md#ownership)) and has ultimate authority over [spawning and despawning](object-spawning.md).

Netcode for GameObjects also supports building games with a [distributed authority topology](../terms-concepts/distributed-authority.md), which provides more options for ownership and authority over NetworkObjects.

## Ownership and distributed authority

In a distributed authority setting, authority over NetworkObjects isn't bound to a single server, but distributed across clients depending on a NetworkObject's [ownership permission settings](#ownership-permission-settings-distributed-authority-only). NetworkObjects with the distributable permission set are automatically distributed amongst clients as they connect and disconnect.

When a client starts a distributed authority session it spawns its player, locks the local player's permissions so that no other client can take ownership, and then spawns some NetworkObjects. At this point, Client-A has full authority over the distributable spawned objects and its player object.

![Distributed authority start](../images/distributed-authority-start.jpg)

When another player joins, as in the diagram below, authority over distributable objects is split between both clients. Distributing the NetworkObjects in this way reduces the overall processing and bandwidth load for both clients. The same distribution happens when a player leaves, either gracefully or unexpectedly. The ownership and last known state of the subset of objects owned by the leaving player is transferred over to the remaining connected clients with no interruption in gameplay.

![Distributed authority new client](../images/distributed-authority-new-client.jpg)

### Ownership permission settings (distributed authority only)

The following ownership permission settings, defined by `NetworkObject.OwnershipStatus`, only take effect when using a distributed authority network topology:

* `None`: Ownership of this NetworkObject is considered static and can't be redistributed, requested, or transferred (a Player would have this, for example).
* `Distributable`: Ownership of this NetworkObject is automatically redistributed when a client joins or leaves, as long as ownership is not locked or a request is pending.
* `Transferable`: Ownership of this NetworkObject can be transferred immediately, as long as ownership is not locked and there are no pending requests.
* `RequestRequired`: Ownership of this NetworkObject must be requested before it can be transferred and will always be locked after transfer.
* `SessionOwner`: This NetworkObject is always owned by the [session owner](../terms-concepts/distributed-authority.md#session-ownership) and can't be transferred or distributed. If the session owner changes, this NetworkObject is automatically transferred to the new session owner.

You can also use `NetworkObject.SetOwnershipLock` to lock and unlock the permission settings of a NetworkObject for a period of time, preventing ownership changes on a temporary basis.

> [!NOTE]
> The ownership permissions are only visible when the Multiplayer Services SDK package is installed and you're inspecting a NetworkObject within the editor. Ownership permissions have no impact when using a client-server network topology, since the server always has authority. For ownership permissions to be used, you must be using a distributed authority network topology.

## Checking for authority

### `HasAuthority`

The `HasAuthority` property, which is available on both NetworkObjects and NetworkBehaviours, is session-mode agnostic and works in both distributed authority and client-server contexts. It's recommended to use `HasAuthority` whenever you're working with individual objects, regardless of whether you're using a distributed authority or client-server topology.

```
public class MonsterAI : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!HasAuthority)
        {
            return;
        }
        // Authority monster init script here
        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!IsSpawned || !HasAuthority)
        {
            return;
        }
        // Authority updates monster AI here
    }
}
```

Using distributed authority with Netcode for GameObjects requires a shift in the understanding of authority: instead of authority belonging to the server in all cases, it belongs to whichever client instance currently has authority. This necessitates a shift away from using local, non-replicated properties to store pertinent states; instead, [NetworkVariables](networkvariable.md) should be used to keep states synchronized and saved when all clients disconnect from a session or ownership is transferred to another client.

Distributed authority supports all built-in NetworkVariable data types. Since there's no concept of an authoritative server in a distributed authority session, all NetworkVariables are automatically configured with owner write and everyone read permissions.

### `IsServer`

`IsServer` or `!IsServer` is the traditional client-server method of checking whether the current context has authority, and is only available in client-server topologies (because distributed authority games have no single authoritative server). You can use a mix of `HasAuthority` and `IsServer` when building a client-server game: `HasAuthority` is recommended when performing object-specific operations, while `IsServer` can be useful to check for authority when performing global actions.

```
public class MonsterAI : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }
        // Server-side monster init script here
        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer)
        {
            return;
        }
        // Server-side updates monster AI here
    }
}
```
