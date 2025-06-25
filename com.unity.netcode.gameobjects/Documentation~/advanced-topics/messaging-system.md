# Sending events with RPCs

Netcode for GameObjects has two parts to its messaging system: [remote procedure calls (RPCs)](message-system/rpc.md) and [custom messages](message-system/custom-messages.md). Both types have sub-types that change their behavior, functionality, and performance.

This page provides an introduction to RPCs. For more details, refer to the pages listed in the [RPCs in Netcode for GameObjects section](#rpcs-in-netcode-for-gameobjects)

## Remote procedure calls (RPCs)

RPCs are a standard software industry concept. They're a way to call methods on objects that aren't in the same executable.

![Client can invoke a server RPC on a NetworkObject. The RPC is placed in the local queue and then sent to the server, where it's executed on the server version of the same NetworkObject.](../../images/sequence_diagrams/RPCs/ServerRPCs.png)

When calling an RPC from a client, the SDK takes note of the object, component, method, and any parameters for that RPC and sends that information over the network. The server receives that information, finds the specified object, finds the specified method, and calls it on the specified object with the received parameters.

Netcode for GameObjects includes multiple RPC variations that you can use to execute logic with various remote targets.

![Server can invoke a client RPC on a NetworkObject. The RPC is placed in the local queue and then sent to a selection of clients (by default this selection is all clients). When received by a client, RPCs are executed on the client's version of the same NetworkObject.](../../images/sequence_diagrams/RPCs/ClientRPCs.png)

### RPCs in Netcode for GameObjects

Refer to the following pages for more information about how RPCs are implemented in Netcode for GameObjects.

- [Remote procedure calls (RPCs)](message-system/rpc.md)
- [Reliability](message-system/reliability.md)
- [RPC parameters](message-system/rpc-params.md)
  - [Serialization types and RPCs](message-system/../serialization/serialization-intro.md)

There's also some additional design advice on RPCs and some usage examples on the following pages:

- [RPC vs NetworkVariable](../learn/rpcvnetvar.md)
- [RPC vs NetworkVariables Examples](../learn/rpcnetvarexamples.md)

> [!NOTE] Migration and compatibility
> Refer to [RPC migration and compatibility](message-system/rpc-compatibility.md) for more information on updates, cross-compatibility, and deprecated methods for Unity RPC.

## RPC method calls

A typical SDK user (Unity developer) can declare multiple RPCs under a `NetworkBehaviour` and inbound/outbound RPC calls will be replicated as a part of its replication in a network frame.

A method turned into an RPC is no longer a regular method; it will have its own implications on direct calls and in the network pipeline.

### RPC usage checklist

To use RPCs, make sure:

-  `[Rpc]` attributes are on your method
- Your method name ends with `Rpc` (for example, `DoSomethingRpc()`)
- Your method is declared in a class that inherits from `NetworkBehaviour`
  - Your GameObject has a NetworkObject component attached

## Serialization types and RPCs

Instances of serializable types are passed into an RPC as parameters and are serialized and replicated to the remote side.

Refer to the [serialization page](serialization/serialization-intro.md) for more information.
