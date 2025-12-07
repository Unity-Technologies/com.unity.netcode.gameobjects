# Core components

Learn about the three core components of Netcode for GameObjects: NetworkObject, NetworkBehaviour, and NetworkManager.

* [NetworkObject](#networkobject): This component, added to a GameObject, declares a prefab or in-scene placed object as a networked object that can have states synchronized between clients and/or a server. A NetworkObject component allows you to identify each uniquely spawned instance (dynamically or in-scene placed). _You cannot derive from the NetworkObject component class_.
* [NetworkBehaviour](#networkbehaviour): This is the fundamental networked scripting component that allows you to synchronize state and write netcode script(s). _You derive from this class to create your own netcode component scripts_.
* [NetworkManager](#networkmanager): This component is the overall network session configuration and session management component. A NetworkManager component is necessary to start or join a network session.


## NetworkObject

| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[NetworkObject](networkobject.md)** | A NetworkObject is a [GameObject](https://docs.unity3d.com/Manual/GameObjects.html) with a NetworkObject component and at least one [NetworkBehaviour](networkbehaviour.md) component, which enables the GameObject to respond to and interact with netcode. |
| **[NetworkObject parenting](../../advanced-topics/networkobject-parenting.md)** | Understand how NetworkObjects are parented in Netcode for GameObjects. |


## NetworkBehaviour


| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[NetworkBehaviour](networkbehaviour.md)** | [NetworkBehaviour](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html) is an abstract class that derives from [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) and is primarily used to create unique netcode or game logic. To replicate any netcode-aware properties or send and receive RPCs, a [GameObject](https://docs.unity3d.com/Manual/GameObjects.html) must have a [NetworkObject](networkobject.md) component and at least one NetworkBehaviour component. |
| **[Synchronizing](networkbehaviour-synchronize.md)** | Understand a NetworkBehaviour component's order of operations when it comes to spawning, despawning, and adding custom synchronization data. |


## NetworkManager

| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[NetworkManager](networkmanager.md)**| The NetworkManager component is a required Netcode for GameObjects component that has all of your project's netcode-related settings. Think of it as the central netcode hub for your netcode-enabled project.   |
| **[Player prefabs and spawning players](playerobjects.md)**| Learn about spawning player objects and creating player prefabs.|

## Additional resources

* [GameObjects](https://docs.unity3d.com/6000.1/Documentation/Manual/working-with-gameobjects.html)