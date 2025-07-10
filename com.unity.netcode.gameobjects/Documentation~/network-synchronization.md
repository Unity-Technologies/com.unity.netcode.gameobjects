# Network synchronization

Manage latency and performance in your Netcode for GameObjects project.

| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[Ways to synchronize](advanced-topics/ways-to-synchronize.md)** | Netcode for GameObjects has three options for synchronizing game states and events. |
| **[NetworkVariables](networkvariables-landing.md)** | Use NetworkVariables to synchronize properties between servers and clients in a persistent manner. |
| **[Remote procedure calls (RPCs)](rpc-landing.md)** | Any process can communicate with any other process by sending a remote procedure call (RPC). |
| **[Custom messages](advanced-topics/message-system/custom-messages.md)** | Create a custom message system for your Netcode for GameObjects project. |
| **[Connection events](advanced-topics/connection-events.md)** | When you need to react to connection or disconnection events for yourself or other clients, you can use `NetworkManager.OnConnectionEvent` as a unified source of information about changes in the network. |
| **[Network update loop](network-update-loop.md)** | The Network Update Loop infrastructure utilizes Unity's low-level Player Loop API allowing for registering `INetworkUpdateSystems` with `NetworkUpdate()` methods to be executed at specific `NetworkUpdateStages` which may be either before or after `MonoBehaviour`-driven game logic execution. |
| **[Network time and ticks](advanced-topics/networktime-ticks.md)** | Understand how network time and ticks work while synchronizing your project. |