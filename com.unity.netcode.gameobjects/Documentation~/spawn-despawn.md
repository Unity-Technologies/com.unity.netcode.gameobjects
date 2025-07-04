# Spawning and despawning

Spawn and despawn objects in your project.

| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[Object spawning](basics/object-spawning.md)** | Spawning in Netcode for GameObjects means to instantiate and/or spawn the object that is synchronized between all clients by the server. |
| **[Object pooling](advanced-topics/object-pooling.md)** | Netcode for GameObjects provides built-in support for Object Pooling, which allows you to override the default Netcode destroy and spawn handlers with your own logic. |
| **[Object visibility](basics/object-visibility.md)** | Object (NetworkObject) visibility is a Netcode for GameObjects term used to describe whether a `NetworkObject` is visible to one or more clients as it pertains to a netcode/network perspective. |
| **[Spawning synchronization](basics/spawning-synchronization.md)** | Ensuring that objects spawn in a synchronized manner across clients can be difficult, although the challenges differ depending on which [network topology](terms-concepts/network-topologies.md) you're using. |