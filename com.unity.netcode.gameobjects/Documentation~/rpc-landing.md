# Remote procedure calls (RPCs)

Manage latency and performance in your Netcode for GameObjects project.

| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[Messaging system](advanced-topics/messaging-system.md)** | Netcode for GameObjects has two parts to its messaging system: [remote procedure calls (RPCs)](advanced-topics/message-system/rpc.md) and [custom messages](advanced-topics/message-system/custom-messages.md). Both types have sub-types that change their behavior, functionality, and performance. |
| **[RPC](advanced-topics/message-system/rpc.md)** | Any process can communicate with any other process by sending a remote procedure call (RPC). |
| **[Reliability](advanced-topics/message-system/reliability.md)** | RPCs are reliable by default.  This means they're guaranteed to be received and executed on the remote side. However, sometimes developers might want to opt-out reliability, which is often the case for non-critical events such as particle effects and sound effects. |
| **[RPC params](advanced-topics/message-system/rpc-params.md)** | Understand how to configure your RPCs. |
| **[RPC vs NetworkVariables](learn/rpcvnetvar.md)** | Understand the different use cases for RPCs and NetworkVariables. |
| **[RPC and NetworkVariable examples](learn/rpcnetvarexamples.md)** | Examples of RPCs and NetworkVariables. |
| **[RPC compatibility](advanced-topics/message-system/rpc-compatibility.md)** | Information on compatibility and support for Unity Netcode for GameObjects features compared to previous Netcode versions. |