# Network topologies

Network topology defines how a network is arranged. In the context of multiplayer games, this primarily relates to how clients, hosts, and servers are connected and communicate. Different network topologies have different benefits and drawbacks, depending on the type of game you want to make.

## Network topologies

The network topologies that Netcode for GameObjects supports are based on the two primary [authority models](authority.md).

### Client-server

Client-server games use a [server authority model](authority.md#server-authority). In this network topology the server has final authority on the state of the game. Each client sends updates to the server, and the server runs the main game simulation. The game has a single server and many clients.

Within the client-server network topology there are two main [network architectures](client-server.md). These are the dedicated game server (where a dedicated machine runs only the game simulation), and the listen-server architecture (where a single game instance runs both the game simulation and a playing game client).

For more details about client-server topologies, refer to the [Client-server topologies page](client-server.md).

### Distributed authority

In a distributed authority topology, game instances share responsibility for owning and tracking the state of objects in the network using a [distributed authority model](authority.md#distributed-authority). A small, lightweight central state service tracks a minimal amount of game state and routes network traffic. There is no server simulating the game: each connected game instance runs a subdivision of the simulation.

Distributed authority topologies are useful for keeping costs down, and solve a lot of the input-related issues typically addressed using client prediction systems. However, the lack of a central authority can make them more vulnerable to cheating and bad actors.

For more details about distributed authority topologies, refer to the [Distributed authority page](distributed-authority.md).

## Network topology comparison

The primary differences between [client-server](client-server.md) and [distributed authority](distributed-authority.md) topologies are cost, security, and latency. There are further considerations for these metrics based on the different [client-server network architectures](client-server.md) that can be used.

* **Cost**: Client-server topologies using a dedicated game server are typically an expensive option due to the hardware and running costs involved in maintaining a powerful server machine, although this does allow for higher performance and better [tick rates](../learn/ticks-and-update-rates.md). Distributed authority networks are cheaper, since they lack a dedicated central server and game simulations run solely on clients. Listen servers are running on an already existing game instance and so there is no extra cost involved.
* **Security**: Because the distributed authority topology spreads authority across multiple clients, any one client may be compromised and/or cheating without making the entire experience unplayable. With a client-server architecture, a dedicated game server is much more resilient to compromised clients, while listen servers have a single point of failure if the game instance hosting the server is compromised.
* **Latency**:  In a distributed authority topology, game state updates are sent to the distributed authority service which then proxies those messages directly to the other connected clients. With the client-server topology, latency can depend greatly based on the location and [network architecture](client-server.md) of the server.

## Which network topology will meet your project's needs?

Both of the network topologies that are provided by Netcode for GameObjects are suitable for a wide range of use-cases. The following questions can help you determine if a certain network topology will fit the needs of your project.

Does your project require:

* A centralized, trusted server for the purpose of securing specific game states from being modified by bad actors?
  * If you answered yes, then the client-server topology is likely to be the best fit for your project's needs. The distributed authority topology relies on the assumption that each game instance can be trusted therefore it is not a reliable authority mode for games where local game instances may be attempting to cheat.

* Client-side prediction with rollback capabilities?
  * If you answered yes, then the client-server topology is likely to be the best fit for your project's needs.

If you're looking for a development flow with simple instantiation and spawning of visually synchronized objects, then the distributed authority topology could be a good fit for your project's needs.
