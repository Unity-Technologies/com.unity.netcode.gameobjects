# NetworkRigidbody inside Boss Room

> [!NOTE]
> Refer to [Physics](../..//advanced-topics/physics.md) for more information.

Boss Room leverages `NetworkRigidbody` to simulate physics-based projectiles. See the Vandal Imp's tossed projectile, the [`ImpTossedItem` prefab](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Prefabs/Game/ImpTossedItem.prefab). At its root, this Prefab has a `NetworkObject`, a `NetworkTransform`, a `Rigidbody`, and a `NetworkRigidbody` component. Refer to [`TossAction.cs`](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Gameplay/Action/ConcreteActions/TossAction.cs) for more implementation details.

An important note: You must do any modifications to a `Rigidbody` that involve Physics (modifying velocity, applying forces, applying torque, and the like) **after** the `NetworkObject` spawns since `NetworkRigidbody` forces the `Rigidbody`'s `isKinematic` flag to be true on `Awake()`. Once spawned, this flag is modified depending on the ownership status of the `NetworkObject`.
