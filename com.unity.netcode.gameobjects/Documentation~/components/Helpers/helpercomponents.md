# Helper Components

Understand the helper components available to use in your Netcode for GameObjects project.

 **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[AttachableBehaviour](attachablebehaviour.md)**| Provides an alternative to `NetworkObject` parenting. This section includes a usage example with `AttachableBehaviour`, `AttachableNode`, and `ComponentController`. |
| **[AttachableNode](attachablenode.md)**| Target parent for an `AttachableBehaviour`. |
| **[ComponentController](componentcontroller.md)**| Provides the synchronization of and control over enabling or disabling objects. |
| **[NetworkAnimator](networkanimator.md)**| The `NetworkAnimator` component provides you with a fundamental example of how to synchronize animations during a network session. Animation states are synchronized with players joining an existing network session and any client already connected before the animation state changing. |
| **[NetworkTransform](networktransform.md)**| [NetworkTransform](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.Components.NetworkTransform.html) is a concrete class that inherits from [NetworkBehaviour](../foundational/networkbehaviour.md) and synchronizes [Transform](https://docs.unity3d.com/Manual/class-Transform.html) properties across the network, ensuring that the position, rotation, and scale of a [GameObject](https://docs.unity3d.com/Manual/working-with-gameobjects.html) are replicated to other clients. |
| **[Physics](../../advanced-topics/physics.md)**| Netcode for GameObjects has a built in approach which allows for server-authoritative physics where the physics simulation only runs on the server. |
