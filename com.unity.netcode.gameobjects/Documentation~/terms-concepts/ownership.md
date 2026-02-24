# Ownership

Along with [authority](./authority.md), Ownership is a core concept within Netcode for GameObjects. Ownership behaves slightly differently for each [network topology](network-topologies.md).

## Ownership in client-server

In [client-server](client-server.md), ownership behaves as a subset of [authority](./authority.md). The owner of an object can control some aspects of that object, while the authority controls and synchronizes others.

Ownership provides client-side reactivity. By giving individual clients  ownership over objects that are important for their gameplay, clients can locally control some parts of their game. For example, this allows clients to avoid lag in their player controller, while leaving the server as the final game authority.

Here's a table showing what actions the authority vs the owner can do in a client-server game.

|Action                          |Authority*|Owner  |
|--------------------------------|----------|-------|
|Spawn/Despawn objects           |**Yes**   |No     |
|Change Ownership                |**Yes**   |No     |
|Move transform                  |No        |**Yes**|
|Update NetworkVariables**       |**Yes**   |No     |
|Synchronize late joining clients|**Yes**   |No     |
|Update object parenting***      |**Yes**   |No     |

*The authority when using client-server will always be the server.

**This default behaviour can be changed using the [NetworkVariableWritePermission.Owner](../basics/networkvariable.md#write-permissions)
***This default behaviour can be changed by setting [NetworkObject.AllowOwnerToParent](../advanced-topics/networkobject-parenting.md#who-can-parent-networkobjects)

## Ownership in distributed authority

In [distributed authority](./distributed-authority.md) the owner of an object is always the authority of that object. In this way, ownership comes before authority. Authority can be transferred between clients via changing and requesting ownership.

When building your game you can use [ownership permissions](../advanced-topics/networkobject-ownership.md#ownership-permissions-settings) to control how and when ownership of objects can be transferred between clients.

Objects with the `OwnershipStatus.Distributable` permission will have their ownership automatically distributed between all connected game clients whenever a new client joins or an existing client leaves. This is the key mechanism in how the game simulation is distributed between clients in a distributed authority session.

## Checking for ownership

The `IsOwner` property, which is available on both NetworkObjects and NetworkBehaviours, is session-mode agnostic and works in both distributed authority and client-server contexts. It's recommended to use `IsOwner` whenever you're synchronizing transform movements, regardless of whether you're using a distributed authority or client-server topology.

## Additional resources

* [Authority](authority.md)
* [Client-server](client-server.md)
* [Distributed authority](distributed-authority.md)
* [Controlling NetworkObject ownership](../advanced-topics/networkobject-ownership.md)
