# Authority

Multiplayer games are games that are played between many different game instances. Each game instance has their own copy of the game world and behaviors within that game world. To have a shared game experience, each networked object is required to have an **authority**.

The authority of a networked object has the ultimate power to make definitive decisions about that object. Each object must have one and only one authority. The authority has the final control over all state and behavior of that object.

The authoritative game instance is the game instance that has authority over a given networked object. This game instance is responsible for simulating networked game behavior. The authority is able to mediate the effects of network lag, and is responsible for reconciling behavior if many players are attempting to simultaneously interact with the same object.

## Authority models

Netcode for GameObjects provides two authority models: [server authority](#server-authority) and [distributed authority](#distributed-authority).

### Server authority

The server authority model has a single game instance that is defined as the server. That game instance is responsible for running the main simulation and managing all aspects of running the networked game. Server authority is the authority model used for [client-server games](client-server.md).

The server authority model has the strength of providing a centralized authority to manage any potential game state conflicts. This allows the implementation of systems such as game state rollback and competitive client prediction. However, this can come at the expense of adding latencies, because all state changes must be sent to the server game instance, processed, and then sent out to other game instances.

Server authority games can also be resource intensive. The server runs the simulation for the entire game world, and so the server needs to be powerful enough to handle the simulation and networking of all connected game clients. This resource requirement can become expensive.

Server authority is primarily used by performance-sensitive games, such as first-person shooters, or competitive games where having a central server authority is necessary to minimize cheating and the effects of bad actors.

### Distributed authority

The [distributed authority model](distributed-authority.md) shares authority between game instances. Each game instance is the authority for a subdivision of the networked objects in the game and is responsible for running the simulation for their subdivision of objects. Updates are shared from other game instances for the rest of the simulation.

The authority of each networked object is responsible for simulating the behavior and managing any aspects of running the networked game that relate to the objects it's the authority of.

Because distributed authority games share the simulation between each connected client, they're less resource intensive. Each machine connected to the game processes a subdivision of the simulation, so no single machine needs to have the capacity to process the entire simulation. This results in a multiplayer game experience that can run on cheaper machines and is less resource intensive.

## Checking for authority

The `HasAuthority` property, which is available on both NetworkObjects and NetworkBehaviours, is session-mode agnostic and works in both distributed authority and client-server contexts. It's recommended to use `HasAuthority` whenever you're working with individual objects, regardless of whether you're using a distributed authority or client-server topology.

```csharp
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

Using distributed authority with Netcode for GameObjects requires a shift in the understanding of authority: instead of authority belonging to the server in all cases, it belongs to whichever client instance currently has authority. This necessitates a shift away from using local, non-replicated properties to store pertinent states; instead, [NetworkVariables](../basics/networkvariable.md) should be used to keep states synchronized and saved when all clients disconnect from a session or ownership is transferred to another client.

Distributed authority supports all built-in NetworkVariable data types. Because there's no concept of an authoritative server in a distributed authority session, all NetworkVariables are automatically configured with owner write and everyone read permissions.

## Additional resources

- [Ownership](ownership.md)
