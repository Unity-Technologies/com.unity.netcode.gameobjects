# Network components

Understand the network components involved in a Netcode for GameObjects project.

| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[NetworkObject](basics/networkobject.md)** | A NetworkObject is a [GameObject](https://docs.unity3d.com/Manual/GameObjects.html) with a NetworkObject component and at least one [NetworkBehaviour](basics/networkbehaviour.md) component, which enables the GameObject to respond to and interact with netcode. |
| **[NetworkObject parenting](advanced-topics/networkobject-parenting.md)** | Understand how NetworkObjects are parented in Netcode for GameObjects. |
| **[NetworkBehaviour](networkbehaviour-landing.md)**| Understand how to use NetworkBehaviour components in your Netcode for GameObjects project. |
| **[Physics](advanced-topics/physics.md)**| Netcode for GameObjects has a built in approach which allows for server-authoritative physics where the physics simulation only runs on the server. |
| **[NetworkManager](components/networkmanager.md)**| The `NetworkManager` is a required Netcode for GameObjects component that has all of your project's netcode-related settings. Think of it as the central netcode hub for your netcode-enabled project.   |
| **[NetworkTransform](components/networktransform.md)**| [NetworkTransform](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.Components.NetworkTransform.html) is a concrete class that inherits from [NetworkBehaviour](basics/networkbehaviour.md) and synchronizes [Transform](https://docs.unity3d.com/Manual/class-Transform.html) properties across the network, ensuring that the position, rotation, and scale of a [GameObject](https://docs.unity3d.com/Manual/working-with-gameobjects.html) are replicated to other clients. |
| **[NetworkAnimator](components/networkanimator.md)**| The `NetworkAnimator` component provides you with a fundamental example of how to synchronize animations during a network session. Animation states are synchronized with players joining an existing network session and any client already connected before the animation state changing. |