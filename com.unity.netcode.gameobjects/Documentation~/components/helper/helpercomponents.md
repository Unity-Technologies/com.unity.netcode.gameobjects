# Helper Components

Understand the helper components available to use in your Netcode for GameObjects project.

 **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[AttachableBehaviour](attachablebehaviour.md)**| Use the AttachableBehaviour component to manage [ComponentController](componentcontroller.md) components and to attach a child GameObject to an [AttachableNode](attachablenode.md). The AttachableBehaviour component provides an alternative to NetworkObject parenting, allowing you to attach and detach child objects dynamically during runtime. |
| **[AttachableNode](attachablenode.md)**| Use an AttachableNode component to provide an attachment point for an [AttachableBehaviour](attachablebehaviour.md) component. |
| **[ComponentController](componentcontroller.md)**| Use a [ComponentController](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.ComponentController.html) component to enable or disable one or more components depending on the authority state of the ComponentController and have those changes synchronized with non-authority instances. |
| **[NetworkAnimator](networkanimator.md)**| The NetworkAnimator component provides you with a fundamental example of how to synchronize animations during a network session. Animation states are synchronized with players joining an existing network session and any client already connected before the animation state changing. |
| **[NetworkTransform](networktransform.md)**| [NetworkTransform](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.Components.NetworkTransform.html) is a concrete class that inherits from [NetworkBehaviour](../core/networkbehaviour.md) and synchronizes [Transform](https://docs.unity3d.com/Manual/class-Transform.html) properties across the network, ensuring that the position, rotation, and scale of a [GameObject](https://docs.unity3d.com/Manual/working-with-gameobjects.html) are replicated to other clients. |
| **[Physics](../../advanced-topics/physics.md)**| Netcode for GameObjects has a built in approach which allows for server-authoritative physics where the physics simulation only runs on the server. |
