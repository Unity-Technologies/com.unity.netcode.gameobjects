# NetworkObject parenting

### Overview

If you aren't completely familiar with transform parenting in Unity, then it's highly recommended to [review over the existing Unity documentation](https://docs.unity3d.com/Manual/class-Transform.html) before reading further to properly synchronize all connected clients with any change in a GameObject component's transform parented status, Netcode for GameObjects requires that the parent and child GameObject components have NetworkObject components attached to them.

### Parenting rules

- Setting the parent of a child's `Transform` directly (that is, `transform.parent = childTransform;`) always uses the default `WorldPositionStays` value of `true`.
  - It's recommended to always use the `NetworkObject.TrySetParent` method when parenting if you plan on changing the `WorldPositionStays` default value.
  - Likewise, it's also recommended to use the `NetworkObject.TryRemoveParent` method to remove a parent from a child.
- When a server parents a spawned NetworkObject component under another spawned NetworkObject component during a Netcode game session this parent child relationship replicates across the network to all connected and future late joining clients.
- If, while editing a scene, you place an in-scene placed NetworkObject component under a GameObject component that doesn't have a NetworkObject component attached to it, Netcode for GameObjects preserves that parenting relationship.
  - During runtime, this parent-child hierarchy remains true unless the user code removes the GameObject parent from the child NetworkObject component.
    - **Note**: Once removed, Netcode for GameObjects won't allow you to re-parent the NetworkObject component back under the same or another GameObject component that with no NetworkObject component attached to it.
- You can perform the same parenting actions with in-scene placed NetworkObjects as you can with dynamically spawned NetworkObject components.
  - Unlike network prefabs that don't allow in-Editor nested NetworkObject component children, in-scene placed NetworkObjects can have multiple generations of in-editor nested NetworkObject component children.
  - You can parent dynamically spawned NetworkObject components under in-scene placed NetworkObject components and vice versa.
- To adjust a child's transform values when parenting or when removing a parent:
  - Override the `NetworkBehaviour.OnNetworkObjectParentChanged` virtual method within a NetworkBehaviour component attached to the child NetworkObject component.
  - When `OnNetworkObjectParentChanged` is invoked, on the server side, adjust the child's transform values within the overridden method.
  - Netcode for GameObjects will then synchronize all clients with the child's parenting and transform changes.

> [!NOTE]
> When a NetworkObject is parented, Netcode for GameObjects synchronizes both the parenting information along with the child's transform values. Netcode for GameObjects uses the `WorldPositionStays` setting to decide whether to synchronize the local or world space transform values of the child NetworkObject component. This means that a NetworkObject component doesn't require you to include a NetworkTransform component if it never moves around, rotates, or changes its scale when it isn't parented. This can be beneficial for world items a player might pickup (parent the item under the player) and the item in question needs to adjustment relative to the player when it's parented or the parent is removed (dropped). This helps to reduce the item's over-all bandwidth and processing resources consumption.

### OnNetworkObjectParentChanged

[`NetworkBehaviour.OnNetworkObjectParentChanged`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html#Unity_Netcode_NetworkBehaviour_OnNetworkObjectParentChanged_Unity_Netcode_NetworkObject_) is a virtual method you can override to be notified when a NetworkObject component's parent has changed. The [`MonoBehaviour.OnTransformParentChanged()`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTransformParentChanged.html) method is used by NetworkObject component to catch `transform.parent` changes and notify its associated NetworkBehaviour components.

```csharp
/// <summary>
/// Gets called when the parent NetworkObject of this NetworkBehaviour's NetworkObject has changed
/// </summary>
virtual void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject) { }
```

> [!NOTE] Multi-generation children and scale
> If you are dealing with more than one generation of nested children where each parent and child have scale values other than `Vector3.one`, then mixing the `WorldPositionStays` value when parenting and removing a parent will impact how the final scale is calculated! If you want to keep the same values before parenting when removing a parent from a child, then you need to use the same `WorldPositionStays` value used when the child was parented.

### Only a server (or a host) can parent NetworkObjects

Similar to [Ownership](../basics/networkobject#ownership), only the server (or host) can control NetworkObject component parenting.

> [!NOTE]
> If you run into a situation where your client must trigger parenting a NetworkObject component, one solution is for the client to send an RPC to the server. Upon receiving the RPC message, the server then handles parenting the NetworkObject component.

### Only parent under a NetworkObject Or nothing (root or null)

You can only parent a NetworkObject under another NetworkObject. The only exception is if you don't want the NetworkObject to have a parent. In this case, you can remove the NetworkObject's parent by invoking `NetworkObject.TryRemoveParent`. If this operation succeeds, the parent of the child will be set to `null` (root of the scene hierarchy).

### Only spawned NetworkObjects can be parented

A NetworkObject component can only be parented if it's spawned and can only be parented under another spawned NetworkObject component. This also means that NetworkObject component parenting can only occur during a network session (netcode enabled game session). Think of NetworkObject component parenting as a Netcode event. In order for it to happen, you must have, at minimum, a server or host instance started and listening.

### Invalid parenting actions are reverted

If an invalid/unsupported NetworkObject component parenting action occurs, the attempted parenting action reverts back to the NetworkObject component's original parenting state.

**For example:**
If you had a NetworkObject component whose current parent was root and tried to parent it in an invalid way (such as under a GameObject without a NetworkObject component), it logs a warning message and the NetworkObject component reverts back to having root as its parent.

### In-scene object parenting and player objects

If you plan on parenting in-scene placed NetworkObject components with a player NetworkObject component when it's initially spawned, you need to wait until the client finishes synchronizing with the server first. Because you can only perform parenting on the server side, ensure you perform the parenting action only when the server has received the `NetworkSceneManager` generated `SceneEventType.SynchronizeComplete` message from the client that owns the player NetworkObject component to be parented (as a child or parent).

For more information, refer to:

- [Real World In-scene NetworkObject Parenting of Players Solution](inscene_parenting_player.md)
- [Scene Event Notifications](../basics/scenemanagement/scene-events#scene-event-notifications)
- [In-Scene NetworkObjects](../basics/scenemanagement/inscene-placed-networkobjects.md)

### WorldPositionStays usage

When using the `NetworkObject.TrySetParent` or `NetworkObject.TryRemoveParent` methods, the `WorldPositionStays` parameter is synchronized with currently connected and late joining clients. When removing a child from its parent, use the same `WorldPositionStays` value that you used to parent the child. More specifically, when `WorldPositionStays` is false, this applies. However, if you're using the default value of `true`, this isn't required (because it's the default).

When the `WorldPositionStays` parameter in `NetworkObject.TrySetParent` is the default value of `true`, this will preserve the world space values of the child `NetworkObject` relative to the parent. However, sometimes you might want to only preserve the local space values (pick up an object that only has some initial changes to the child's transform when parented). Through a combination of `NetworkObject.TrySetParent` and `NetworkBehaviour.OnNetworkObjectParentChanged` you can accomplish this without the need for a NetworkTransform component. To better understand how this works, it's important to understand the order of operations for both of these two methods:

**Server-Side**

- `NetworkObject.TrySetParent` invokes `NetworkBehaviour.OnNetworkObjectParentChanged`
  - You can make adjustments to the child's position, rotation, and scale in the overridden `OnNetworkObjectParentChanged` method.
- The ParentSyncMessage is generated, the transform values are added to the message, and the message is then sent.
  - ParentSyncMessage includes the child's position, rotation, and scale
    - Currently connected or late joining clients will be synchronized with the parenting and the child's associated transform values

**When to use a NetworkTransform** <br />
If you plan on the child NetworkObject component moving around, rotating, or scaling independently (when parented or not) then you will still want to use a NetworkTransform component.
If you only plan on making a one time change to the child NetworkObject component's transform when parented or having a parent removed, then the child doesn't need a NetworkTransform component.

For more information, refer to [Learn More About WorldPositionStays](https://docs.unity3d.com/ScriptReference/Transform.SetParent.html).

### Network prefabs, parenting, and NetworkTransform components

Because the NetworkTransform component synchronizes the transform of a GameObject component (with a NetworkObject component attached to it), it can become tricky to understand the parent-child transform relationship and how that translates when synchronizing late joining clients. Currently, a network prefab can only have one NetworkObject component within the root GameObject component of the prefab. However, you can have a complex hierarchy of GameObject components nested under the root GameObject component and each child GameObject component can have a NetworkBehaviour component attached to it. Because a NetworkTransform component synchronizes the transform of the GameObject component it's attached to, you might be tempted to setup a network prefab like this:

```
Network Prefab Root (GameObject with NetworkObject and NetworkTransform components attached to it)
  ├─ Child #1 (GameObject with NetworkTransform component attached to it)
  │
  └─ Child #2 (GameObject with NetworkTransform component attached to it)
```

While this won't give you any warnings and, depending upon the transform settings of Child #1 & #2, it might appear to work properly (because it synchronizes clients), it's important to understand how the child GameObject component (with no NetworkObject component attached to it) and parent GameObject component (that does have a NetworkObject component attached to it), synchronize when a client connects to an already in-progress network session (late joins or late joining client). If Child #1 or Child #2 have had changes to their respective GameObject component's transform before a client joining, then upon a client late joining the two child GameObject component's transforms won't synchronize during the initial synchronization period because they don't have NetworkObject components attached to them:

```
Network Prefab Root (Late joining client is synchronized with GameObject's current transform state)
  ├─ Child #1 (Late joining client *isn't synchronized* with GameObject's current transform state)
  │
  └─ Child #2 (Late joining client *isn't synchronized* with GameObject's current transform state)
```

This is important to understand because the NetworkTransform component initializes itself, during `NetworkTransform.OnNetworkSpawn`, with the GameObject component's current transform state. Just below, in the parenting examples, there are some valid and invalid parenting rules. As such, take these rules into consideration when using NetworkTransform components if you plan on using a complex parent-child hierarchy. Also make sure to organize your project's assets where any children that have NetworkTransform components attached to them also have NetworkObject components attached to them to avoid late-joining client synchronization issues.

## Parenting examples

### Simple example:

For this example, assume you have the following initial scene hierarchy before implementing parenting:

```
Sun
Tree
Camera
Player (GameObject->NetworkObject)
Vehicle (GameObject->NetworkObject)
```

Both the player and vehicle NetworkObject components are spawned and the player moves towards the vehicle and wants to get into the vehicle. The player's client sends perhaps a **use object** RPC command to the server. In turn, the server then parents the player under the vehicle and changes the player's model pose to sitting. Because both NetworkObject components are spawned and the server is receiving an RPC to perform the parenting action, the parenting action performed by the server is valid and the player is then parented under the vehicle as shown below:

```
Sun
Tree
Camera
Vehicle (GameObject->NetworkObject)
  └─ Player (GameObject->NetworkObject)
```

### Mildly complex invalid example:

```
Sun
Tree
Camera
Player (GameObject->NetworkObject)
Vehicle (GameObject->NetworkObject)
  ├─ Seat1 (GameObject)
  └─ Seat2 (GameObject)
```

In the above example, the vehicle has two GameObject components nested under the vehicle's root GameObject to represent the two available seats. If you tried to parent the player under Seat1:

```
Sun
Tree
Camera
Vehicle (GameObject->NetworkObject)
  ├─Seat1 (GameObject)
  │ └─Player (GameObject->NetworkObject)
  └─Seat2 (GameObject)
```

This is an invalid parenting and will revert.

### Mildly complex valid example:

To resolve the earlier invalid parenting issue, you need to add a NetworkObject component to the seats. This means you need to:

1. Spawn the vehicle and the seats:

```
Sun
Tree
Camera
Player (GameObject->NetworkObject)
Vehicle (GameObject->NetworkObject)
Seat1 (GameObject->NetworkObject)
Seat2 (GameObject->NetworkObject)
```

2. After spawning the vehicle and seats, parent the seats under the vehicle.

```
Sun
Tree
Camera
Player (GameObject->NetworkObject)
Vehicle (GameObject->NetworkObject)
  ├─ Seat1 (GameObject->NetworkObject)
  └─ Seat2 (GameObject->NetworkObject)
```

3. Finally, some time later a player wants to get into the vehicle and the player is parented under Seat1:

```
Sun
Tree
Camera
Vehicle (GameObject->NetworkObject)
  ├─Seat1 (GameObject->NetworkObject)
  │ └─Player (GameObject->NetworkObject)
  └─Seat2 (GameObject->NetworkObject)
```

### Parenting & transform synchronization

It's important to understand that without the use of a NetworkTransform component clients are only synchronized with the transform values when:

- A client is being synchronized with the NetworkObject component in question:
  - During the client's first synchronization after a client has their connection approved.
  - When a server spawns a new NetworkObject component.
- A NetworkObject component has been parented (or a parent removed).
- The server can override the `NetworkBehaviour.OnNetworkObjectParentChanged` method and adjust the transform values when that's invoked.
  - These transform changes will be synchronized with clients via the `ParentSyncMessage`

> [!NOTE] Optional auto synchronized parenting
> The Auto Object Parent Sync property of a NetworkObject component, enabled by default, allows you to disable automatic parent change synchronization in the event you want to implement your own parenting solution for one or more NetworkObjects. It's important to understand that disabling the Auto Object Parent Sync option of a NetworkObject component will treat the NetworkObject component's transform synchronization with clients as if its parent is the hierarchy root (null).
