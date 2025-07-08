# NetworkObject parenting inside Boss Room

:::note
Required reading: [NetworkObject Parenting](../../advanced-topics/networkobject-parenting.md)
:::

Before detailing Boss Room's approach to NetworkObject parenting, it's important to highlight a limitation of Netcode: A dynamically-spawned NetworkObject **can't** contain another NetworkObject in its hierarchy. If you spawn such a NetworkObject, you can't spawn children `NetworkObjects`. You can only add children NetworkObject components to a NetworkObject that is part of a scene.

Boss Room leverages NetworkObject parenting through the server-driven `PickUp` action (see [`PickUpAction.cs`](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/PickUpAction.cs)), where a character has the ability to pick up a specially-tagged, in-scene placed NetworkObject (see [`PickUpPot` prefab](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Prefabs/Game/PickUpPot.prefab)].

At its root, `PickUpPot` has a NetworkObject, a `NetworkTransform`, and a `PositionConstraint` component. `AutoObjectParentSync` is enabled on its `NetworkTransform` (as is by default) so that:

1. The NetworkObject can verify server-side if parenting a Heavy object to another NetworkObject is allowed.
2. The NetworkObject can notify us when the parent has successfully been modified server-side.

To accommodate the limitation highlighted at the beginning of this document, Boss Room leverages the `PositionConstraint` component to simulate an object following a character's position.

A special hand bone has been added to our Character's avatar. Upon a character's successful parenting attempt, this special bone is set as the `PickUpPot`'s `PositonConstraint` target. So while the `PickUpPot` is technically parented to a player, the `PositionConstraint` component allows the `PickUpPot` to follow a bone's position, presenting the **illusion** that the `PickUpPot` is parented to the player's hand bone itself.

Once the `PickUpPot` is parented, local space simulation is enabled on its [`NetworkTransform` component](../../components/networktransform.md).
