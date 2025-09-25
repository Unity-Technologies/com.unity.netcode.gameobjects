#  Distributed authority topologies

The [distributed authority network topology](network-topologies.md#distributed-authority) is one possible [network topology](network-topologies.md) available within Netcode for GameObjects. Distributed authority games use the [distributed authority model](authority.md#distributed-authority).

The traditional [client-server network topology](network-topologies.md#client-server) has a dedicated game instance running the game simulation. This means all state changes must be communicated to the server and then the server communicates those updates to all other connected clients. This design works well when using a powerful dedicated game server, however significant latencies are added when communicating state changes with a [listen server architecture](../learn/listenserverhostarchitecture.md).

> [!NOTE]
> The distributed authority service provided by the [Multiplayer Services package](https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual) has a free tier for bandwidth and connectivity hours, allowing you to develop and test without immediate cost. Refer to the [Unity Gaming Services pricing page](https://unity.com/products/gaming-services/pricing) for complete details.

## Considerations

Using a distributed authority topology is typically not suitable for high-performance competitive games that require an accurate predictive motion model. The distributed authority model successfully addresses a lot of visual and input-related issues, but does have some limitations:

* Because authority and ownership of objects is distributed across clients, there's typically no single physics simulation governing the interaction of all objects. This can require approaching physics-related gameplay differently compared to a traditional client-server context.
* Depending on the platform and design of your product, it can be easier for bad actors to cheat. The authority model gives more trust to individual clients. Evaluate your cheating tolerance when developing with distributed authority.

## Session ownership

When using the distributed authority topology, it's necessary to have a single dedicated client that's responsible for managing and synchronizing global game state-related tasks. This client is referred to as the session owner.

The initial session owner is the first client that joins when the session is created. If this client disconnects during the game, a new session owner is automatically selected and promoted from within the clients that are currently connected.

### `IsSessionOwner`

To determine if the current client is the session owner, use the `IsSessionOwner` property provided by Netcode for GameObjects. This property is available on the `NetworkManager.Singleton` instance and returns `true` if the local client is the session owner.

```csharp
public class MonsterAI : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsSessionOwner)
        {
            return;
        }
        // Global monster init behaviour here
        base.OnNetworkSpawn();
    }

    private void Update()
    {
        if (!IsSpawned || !IsSessionOwner)
        {
            return;
        }
        // Global monster AI updates here
    }
}
```

You can use this property to conditionally execute logic that should only run on the session owner, such as managing global game state or handling session-wide events.

## Additional resources

- [Distributed authority quickstart](../learn/distributed-authority-quick-start.md)
- [Understanding ownership and authority](./ownership.md)
- [Spawning synchronization](../basics/spawning-synchronization.md)
- [Deferred despawning](../basics/deferred-despawning.md)
- [Distributed Authority Social Hub sample](../samples/bitesize/bitesize-socialhub.md)
