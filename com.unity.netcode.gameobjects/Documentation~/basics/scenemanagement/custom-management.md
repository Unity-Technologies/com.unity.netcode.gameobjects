# Custom scene management

> [!NOTE]
> If you haven't already read the [Using NetworkSceneManager](using-networkscenemanager.md) section, it's highly recommended to do so before proceeding. Custom Scene Management currently has some drawbacks that we hope to improve upon over time.

## Building Your Own Scene Management Solution
Netcode for GameObjects is designed to provide a scene management solution that should meet most projects' needs. However, some projects might still need to build their own scene management solution. The first step is to disable **Enable Scene Management** in your `NetworkManager`'s properties. All of your scene loading and unloading must be handled via the `UnityEngine.SceneManagement.SceneManager` class.  

### Integrated NetworkSceneManager Comparison (Enabled vs Disabled)
Network Scene Management      | Enabled  | Disabled
------------------------------|----------|----------
Scene Event Synchronizing     | Yes      | No
In-Scene Network Prefab Synchronizing | Yes      | Yes
In-Scene NetworkObject Synchronizing  | Yes      | No
Auto Scene Migration Synchronizing    | Yes      | No

#### Scene Event Synchronizing
When integrated scene management is enabled, scene-related events are automatically synchronized for you. When integrated scene management is disabled, you need to handle the synchronization of scenes yourself.

#### In-Scene Placed Network Prefabs and NetworkObjects
Clients are always synchronized with any in-scene placed Network Prefab instances when they join a session, regardless of whether integrated scene management is enabled. When scene management is disabled, only scenes loaded prior to a client joining are synchronized — any scenes loaded *after* a client is connected won't be synchronized (you would want to only dynamically spawn NetworkObjects at that point).

> [!NOTE]
> Any in-scene defined NetworkObject (i.e. not a network prefab instance) will not get synchronized with a newly-joined client when scene management is disabled.

When the integrated `NetworkSceneManager` is enabled, both in-scene placed network prefab instances and in-scene defined `NetworkObject`s get synchronized, and when new scenes are loaded they get synchronized also.

#### Auto (NetworkObject) Scene Migration Synchronization
When integrated scene management is enabled, you can have the server migrate an already spawned `NetworkObject` from one scene to another and it will automatically be synchronized with connected and late-joining clients.

When integrated scene management is disabled, this is not handled and you need to use either NetworkVariables, Rpcs, or custom messages to handle synchronizing clients with any migration of `NetworkObjects`s from one scene to another.

### Registering In-Scene Placed Network Prefab Instances
While integrated scene management handles the synchronization of in-scene placed `NetworkObject`s, custom scene management treats everything like a dynamically spawned `NetworkObject`.  As such, you can only use network prefabs defined in your NetworkPrefabList assigned to your NetworkManager, and any in-scene defined `NetworkObject`s will not synchronize with clients.

Once you've registered your in-scene placed Network Prefabs with your `NetworkPrefabList` and assigned that to your `NetworkManager`, you can then start a server or host and have a client connect and synchronize with the in-scene placed Network Prefab instances (as long as both client and server have pre-loaded the scene or scenes required).  

> [!NOTE]
> When a client first connects, it deletes any in-scene placed `NetworkObjects` in any of the scenes it has currently loaded.  When using a custom scene management solution, in-scene placed `NetworkObject`s are actually dynamically spawned. This means any changes you make to your in-scene placed Network Prefabs will *not* be synchronized with clients automatically.

### Synchronizing In-Scene Placed Network Prefab Instances
If you want to change an in-scene placed network prefab instance, you need to handle the serialization of these settings yourself. You can do this by overriding `NetworkBehaviour.OnSynchronize` and serializing any property updates you want to have synchronized with clients when they join. [Read More About OnSynchronize Here](../../basics/networkbehaviour.md#pre-spawn-synchronization).

## Starting a Netcode Enabled Game Session
The recommended way of starting session using your own scene management solution is to assure that when a client attempts to join a netcode game session it should already have (as best as possible) any scenes that the server might have loaded.  While this does not assure that your newly connecting client will load any additional scenes that might have been loaded, using this approach initially will get you started so you can then come up with a strategy to handling:
- Scene Synchronization
- Scene Loading and Unloading

### Scene synchronization
 While you might have an initial set of scenes loaded, you are bound to want to load more scenes as your netcode enabled game session progresses.  Once a client has fully connected (you can use `NetworkManager.OnClientConnected` for this), you will want to send the client a list of additional scenes to be loaded.
  - You will want to come up with your own "client state" as it progresses through this synchronization process to determine when a client has loaded all scenes.  
    - For example:  a client might be connected and has synchronized with the default scenes required to connect, but then you have one or more additional scenes you might have loaded (additively) that the client needs to load and synchronize (spawn) any in-scene placed Network Prefab instances.
  - Remember, once a client has connected, if you load additional scenes then any in-scene placed network prefab instances will not be synchronized for you. It's recommended to dynamically spawn network prefab instances after the initial client synchronization if you plan on loading more scenes.

### Scene Loading and Unloading
You can accomplish this using either RPCs or custom messages. You might even use your own `INetworkSerializable` implementation that has a list of scenes and whether they should be loaded or unloaded.  You should always have some form of "complete" response factored into your design so you know when a client has finished loading/unloading a scene. You will also want to devise a strategy for loading a scene in `LoadSceneMode.Additive` and `LoadSceneMode.Single` modes.  

> [!NOTE]
> Creating a global scene management script and attaching it to the same GameObject as the `NetworkManager` is one way to assure your custom netcode scene management solution persists while a game session is in progress.  
