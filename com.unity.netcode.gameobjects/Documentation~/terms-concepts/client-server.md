# Client-server topologies

Client-server is one possible [network topology](network-topologies.md) you can use for your multiplayer game.

## Defining client-server

A client-server topology consists of two distinct types of game instances: a single server game instance, and one or more client game instances. The server always has [authority](authority.md) and runs the main simulation of the game, including simulating physics, spawning and despawning objects, and authorizing client requests. Client game instances can then connect to the server to interact with and respond to the server's game simulation.

Client-server encompasses multiple potential network arrangements. The most common is a dedicated game server, in which a specialized server manages the game and exists solely for that purpose.

An alternative client-server arrangement is a [listen server](../learn/listenserverhostarchitecture.md), in which the game server runs on the same machine as a client. In this arrangement, the server game instance is referred to as a host. A host game instance runs as both a server and a client simultaneously.

## Checking for game instance type

### `IsServer`

`IsServer` or `!IsServer` is the traditional client-server method of checking whether the current game instance is running as a server instance. This is useful for ensuring that the server instance is the only instance running authoritative game logic, such as spawning objects, processing game rules, and validating client actions.

Use `IsServer` to ensure that only the server executes authoritative or global code. For example, spawning enemies, handling core game logic, or updating shared state should only happen on the server. This prevents clients from making unauthorized changes and helps maintain a consistent game state across all connected players.

```csharp
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

### `IsHost`

Use the `IsHost` property to determine if the current game instance is running as both a server and a client simultaneously, a configuration known as a host. In Netcode for GameObjects, this is common when using a [listen server](../learn/listenserverhostarchitecture.md), where the server and one client share the same process.

`IsHost` can be useful for branching resource heavy logic so that a game running as a listen-server can use code-paths optimized for running on end devices.

```csharp
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
        if (IsHost)
        {
          // Monster AI that is optimized for user devices using a listen-server here.
          return
        }

        // Monster AI that is optimized for the dedicated game server here.
    }
}
```

### `IsClient`

Use the `IsClient` property to check if the current game instance is running as a client. This is helpful for executing logic that should only run on client machines, such as updating UI elements, handling local input, or playing client-specific effects. Use `IsClient` to ensure that code only runs on the client side, preventing unintended execution on the server or in non-client contexts.

`IsClient` will be `true` for host instances as a host game instance is running as both a server and a client simultaneously.

```csharp
public class MonsterAI : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            // Play client side effects here

            if (!IsHost)
            {
               // exit early if the game instance is only a client
               return;
            }
        }

        // Server-side monster init script here
        base.OnNetworkSpawn();
    }
}
```

## Use cases for client-server

Dedicated servers are often the most expensive network topology, but also offer the highest performance and can provide additional functionality such as competitive client prediction, rollback, and a centralized authority to manage any potential client conflicts. However, this comes at the cost of added latencies when communicating state changes from one client to another, as all state changes must be sent from client to server, processed, and then sent back out to other clients.

Client-server is primarily used by performance-sensitive games, such as first-person shooters, or competitive games where having a central server authority is necessary to minimize cheating and the effects of bad actors.

## Additional resources

- [Network topologies](network-topologies.md)
- [Distributed authority](distributed-authority.md)
- [Listen server architecture](../learn/listenserverhostarchitecture.md)