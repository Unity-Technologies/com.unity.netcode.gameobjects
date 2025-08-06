# Physics

There are many different ways to manage physics simulation in multiplayer games. Netcode for GameObjects (Netcode) has a built in approach which allows for server-authoritative physics where the physics simulation only runs on the server. To enable network physics, add a `NetworkRigidbody` component to your object.

## NetworkRigidbody

NetworkRigidbody is a component that sets the Rigidbody of the GameObject into kinematic mode on all non-authoritative instances (except the instance that has authority). Authority is determined by the NetworkTransform component (required) attached to the same GameObject as the NetworkRigidbody. Whether the NetworkTransform is server authoritative (default) or owner authoritative, the NetworkRigidBody authority model will mirror it. That way, the physics simulation runs on the authoritative instance, and the resulting positions synchronize on the non-authoritative instances, each with their RigidBody being Kinematic, without any interference.

To use NetworkRigidbody, add a Rigidbody, NetworkTransform, and NetworkRigidbody component to your NetworkObject.

Some collision events aren't fired when using `NetworkRigidbody`.
- On the `server`, all collision and trigger events (such as `OnCollisionEnter`) fire as expected and you can access (and change) values of the `Rigidbody` (such as velocity).
- On the `clients`, the `Rigidbody` is kinematic. Trigger events still fire but collision events won't fire when colliding with other networked `Rigidbody` instances.

> [!NOTE]
> If there's a need for a gameplay event to happen on a collision, you can listen to `OnCollisionEnter` function on the server and synchronize the event via `Rpc(SendTo.ClientsAndHost)` to all clients.

## NetworkRigidbody2D

`NetworkRigidbody2D` works in the same way as NetworkRigidbody but for 2D physics (`Rigidbody2D`) instead.

## NetworkRigidbody and ClientNetworkTransform

You can use NetworkRigidbody with the [`ClientNetworkTransform`](../components/networktransform.md) package sample to allow the owner client of a NetworkObject to move it authoritatively. In this mode, collisions only result in realistic dynamic collisions if the object is colliding with other NetworkObjects (owned by the same client).

> [!NOTE]
> Add the ClientNetworkTransform component to your GameObject first. Otherwise the NetworkRigidbody automatically adds a regular NetworkTransform.

## Physics and latency

A common issue with physics in multiplayer games is lag and how objects update on basically different timelines. For example, a player would be on a timeline that's offset by the network latency relative to your server's objects. One way to prepare for this is to test your game with artificial lag. You might catch some weird delayed collisions that would otherwise make it into production.

The best way to address the issue of physics latency is to create a custom `NetworkTransform` with a custom physics-based interpolator. You can also use the [Network Simulator tool](https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@latest?subfolder=/manual/network-simulator) to spot issues with latency.

## Message processing vs. applying changes to state (timing considerations)

When handling the synchronization of changes to certain physics properties, it's important to understand the order of operations involved in message processing relative to the update stages that occur within a single frame. The stages occur in this order:

- Initialization _(Awake and Start are invoked here)_
- EarlyUpdate _(Inbound messages are processed here)_
- FixedUpdate _(Physics simulation is run and results)_
- PreUpdate _(NetworkTime and Tick is updated)_
- Update _(NetworkBehaviours/Components are updated)_
- PreLateUpdate: _(Useful for handling post-update tasks prior to processing and sending pending outbound messages)_
- LateUpdate: _(Useful for changes to camera, detecting input, and handling other post-update tasks)_
- PostLateUpdate: _(Dirty NetworkVariables processed and pending outbound messages are sent)_

From this list of update stages, the `EarlyUpdate` and `FixedUpdate` stages have the most impact on NetworkVariableDeltaMessage and RpcMessages processing. Inbound messages are processed during the `EarlyUpdate` stage, which means that Rpc methods and NetworkVariable.OnValueChanged callbacks are invoked at that point in time during any given frame. Taking this into consideration, there are certain scenarios where making changes to a Rigidbody could yield undesirable results.

### Rigidbody interpolation example

While `NetworkTransform` offers interpolation as a way to smooth between delta state updates, it doesn't get applied to the authoritative instance. You can use `Rigidbody.interpolation` for your authoritative instance while maintaining a strict server-authoritative motion model.

To have a client control their owned objects, you can use either [RPCs](message-system/rpc.md) or [NetworkVariables](../basics/networkvariable.md) on the client-side. However, this often results in the host-client's updates working as expected, but with slight jitter when a client sends updates. You might be scanning for key or device input during the `Update` to `LateUpdate` stages. Any input from the host player is applied after the `FixedUpdate` stage (i.e. physics simulation for the frame has already run), but input from client players is sent via a message and processed, with a half RTT delay, on the host side (or processed 1 network tick + half RTT if using NetworkVariables). Because of when messages are processed, client input updates run the risk of being processed during the `EarlyUpdate` stage which occurs just before the current frame's `FixedUpdate` stage.

To avoid this kind of scenario, it's recommended that you apply any changes received via messages to a Rigidbody _after_ the FixedUpdate has run for the current frame. If you [look at how `NetworkTransform` handles its changes to transform state](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/a2c6f7da5be5af077427eef9c1598fa741585b82/com.unity.netcode.gameobjects/Components/NetworkTransform.cs#L3028), you can see that state updates are applied during the `Update` stage, but are received during the `EarlyUpdate` stage. Following this kind of pattern when synchronizing changes to a Rigidbody via messages will help you to avoid unexpected results in your Netcode for GameObjects project.

The best way to address the issue of physics latency is to create a custom `NetworkTransform` with a custom physics-based interpolator. You can also use the [Network Simulator tool](https://docs.unity3d.com/Packages/com.unity.multiplayer.tools@latest?subfolder=/manual/network-simulator) to spot issues with latency.
