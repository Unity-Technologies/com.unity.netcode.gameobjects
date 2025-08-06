# Client-server topologies

Client-server is one possible [network topology](network-topologies.md) you can use for your multiplayer game.

## Defining client-server

In a client-server topology, a central server is responsible for running the main simulation and managing all aspects of running the networked game, including simulating physics, spawning and despawning objects, and authorizing client requests. Players connect to the server using separate client programs to see and interact with the game.

Client-server encompasses a number of potential network arrangements. The most common is a dedicated game server, in which a specialized server manages the game and exists solely for that purpose. An alternative client-server arrangement is to have a [listen server](../learn/listenserverhostarchitecture.md), in which the game server runs on the same machine as a client.

## Use cases for client-server

Dedicated servers are often the most expensive network topology, but also offer the highest performance and can provide additional functionality such as competitive client prediction, rollback, and a centralized authority to manage any potential client conflicts. However, this comes at the cost of added latencies when communicating state changes from one client to another, as all state changes must be sent from client to server, processed, and then sent back out to other clients.

Client-server is primarily used by performance-sensitive games, such as first-person shooters, or competitive games where having a central server authority is necessary to minimize cheating and the effects of bad actors.
