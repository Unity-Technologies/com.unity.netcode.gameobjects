# Synchronizing states and events

Netcode for GameObjects has three options for synchronizing game states and events:

- Messaging system
    - [Remote procedure calls (RPCs)](message-system/rpc.md)
    - [Custom messages](message-system/custom-messages.md)
- [NetworkVariables](../basics/networkvariable.md)
    - Handled by the internal messaging system

While each of these options can be used to synchronize states or events, they all have specific use cases and limitations.

## Messaging system

The Netcode for GameObjects messaging system allows you to send and receive messages or events. The system supports the serialization of most primitive value `type`s as well as any classes and/or structures that implement the `INetworkSerializable` interface.

### Remote procedure calls (RPCs)

RPCs are a way of sending an event notification as well as a way of handling direct communication between a server and a client. This is sometimes useful when the ownership scope of the `NetworkBehavior`, that the remote procedure call is declared within, belongs to the server but you still want one or more clients to be able to communicate with the associated `NetworkObject`.  

Usage examples:

- An RPC with `SendTo.Server` can be used by a client to notify the server that the player is trying to use a world object (such as a door or vehicle).
- An RPC with `SendTo.SpecifiedInParams` can be used by a server to notify a specific client of a special reconnection key or some other player-specific information that doesn't require its state to be synchronized with all current and any future late-joining client(s).

There are many RPC methods, as outlined on the [RPC page](message-system/rpc.md#rpc-targets). The most commonly used are:

- `Rpc(SendTo.Server)`: A remote procedure call received by and executed on the server side.
- `Rpc(SendTo.NotServer)`: A remote procedure call received by and executed on the client side. Note that this does NOT execute on the host, as the host is also the server.
- `Rpc(SendTo.ClientsAndHost)`: A remote procedure call received by and executed on the client side. If the server is running in host mode, this RPC will also be executed on the server (the host client).
- `Rpc(SendTo.SpecifiedInParams)`: A remote procedure call that will be sent to a list of client IDs provided as parameters at runtime. By default, other `SendTo` values cannot have any client IDs passed to them to change where they are being sent, but this can also be changed by passing `AllowTargetOverride = true` to the `Rpc` attribute.

RPCs have no limitations on who can send them: the server can invoke an RPC with `SendTo.Server`, a client can invoke an RPC with `SendTo.NotServer`, and so on. If an RPC is invoked in a way that would cause it to be received by the same process that invoked it, it will be executed immediately in that process by default. Passing `DeferOverride = true` to the `Rpc` attribute will change this behavior and the RPC will be invoked at the start of the next frame.

Refer to the [RPC page](../advanced-topics/message-system/rpc.md) for more details.

### Custom messages

Custom messages provide you with the ability to create your own message type. Refer to the [custom messages page](../advanced-topics/message-system/custom-messages.md) for more details.

## NetworkVariables

A NetworkVariable is most commonly used to synchronize state between both connected and late-joining clients. The `NetworkVariable` system only supports non-nullable value `type`s, but also provides support for `INetworkSerializable` implementations as well. You can create your own `NetworkVariable` class by deriving from the `NetworkVariableBase` abstract class. If you want something to always be synchronized with current and late-joining clients, then it's likely a good `NetworkVariable` candidate.

Refer to the [NetworkVariable page](../basics/networkvariable.md) for more details.
