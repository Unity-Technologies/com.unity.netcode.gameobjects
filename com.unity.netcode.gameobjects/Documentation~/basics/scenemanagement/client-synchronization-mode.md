# Client synchronization mode

Client synchronization occurs when immediately after a client connects to a host or server and the connection is approved. During the client synchronization process, the server will determine everything that a client might need to know about in order to join an in progress network session (networked game session). Netcode for GameObjects provides you with the ability to select the client synchronization mode that best suits your project's needs.  During client synchronization a client will automatically set its local client synchronization mode to match that of the server or host setting.

The client synchronization mode should be set when the server or host is first provided via the `NetworkSceneManager.SetClientSynchronizationMode` method that takes a [`LoadSceneMode`](https://docs.unity3d.com/ScriptReference/SceneManagement.LoadSceneMode.html) value as a parameter. Each client synchronization mode behaves in a similar way to loading a scene based on the chosen `LoadSceneMode`.

## Single Client Synchronization Mode
_(Existing Client-Side Scenes Unloaded During Synchronization)_

If you are already familiar with Netcode for GameObjects, then this is most likely the client synchronization mode you are familiar with.  When client synchronization mode is set to `LoadSceneMode.Single`, a client treats the synchronization similar to that of loading a scene using `LoadSceneMode.Single`.  When loading a scene in `LoadSceneMode.Single`, any currently loaded scene will be unloaded and then the new scene is loaded.  From a client synchronization mode perspective, the host or server will always have a default active scene. As such, the default active scene is loaded first in `LoadSceneMode.Single` mode and then any other additional scenes that need to be synchronized are loaded as `LoadSceneMode.Additive`.


This means any scene that is loaded on the client side prior to being synchronized will be unloaded when the first/default active scene is loaded.

![image](../../images/ClientSynchronizationModeSingle.png)

The above image shows a high level overview of single mode client synchronization. For this scenario, a client had just left a network session where the game session was on Level 1. The client still has two common scenes to all levels, Enemy Spawner and World Items scenes, and the Level scenes contain unique assets/settings based on the level number reached. The client then joins a new game session that is on Level 3. With single mode client synchronization, the currently active scene on the server-side is the first scene loaded in `LoadSceneMode.Single` on the client-side (thus why we refer to the client synchronization mode as "single"). By default, loading a new scene in `LoadSceneMode.Single` mode will unload all currently loaded scenes before loading the new scene. Then, for each additional scene that the server requires the client to have loaded in order to be fully synchronized, all remaining scenes are loaded additively (`LoadSceneMode.Additive`).

For some games/projects this type of client synchronization is all that is needed.  However, there are scenarios where you might want to have more control over which scenes are unloaded (if any) during the client synchronization process or you might want to reduce the client synchronization time by having certain commonly shared scenes pre-loaded (and possibly never unloaded) prior to connecting. If your project's needs falls into this realm, then the recommended solution is to use additive client synchronization mode.

## Additive Client Synchronization Mode
_(Existing Client-Side Scenes Used During Synchronization)_

Additive client synchronization is mode similar to additive scene loading in that any scenes the client has currently loaded, prior to connecting to a server or host, will remain loaded and be taken into consideration as scenes to be used during the synchronization process.

This can be particularly useful if you have common UI element scenes (i.e. menu interfaces) that you would like to persist throughout the duration of the application instance's life cycle (i.e. from the time the application starts to the time it is stopped/exited).  It can also be useful to have certain scenes pre-loaded ahead of time to reduce load times when connecting to an existing network session.

![image](../../images/ClientSynchronizationModeAdditive.png)

In the above image we can see a scenario where a server has a Menu UI Scene that it ignores using `NetworkSceneManager.VerifySceneBeforeLoading`, two scenes that the project is configured to always have pre-loaded _(Enemy Spawner and World Items scenes)_, and the server has reached "level 3" of the networked game session so it has the Level 3 scene loaded. The client side almost mirrors the server side (scenes loaded relative) when it began the synchronization process. The only difference between the server and the client was the Level 3 scene that the client has to load in order to be fully synchronized with the server.

_If the server was running in client synchronization mode `LoadSceneMode.Single`, the client would have had to load (at a minimum) three scenes if the Level3 scene was the currently active scene on the server side (not to mention having to reload the Menu UI scene if the user on the client side wanted to adjust a global setting like the audio level of the game). By using `LoadSceneMode.additive` client synchronization, the synchronizing client only has to load one scene in order to be "scene synchronized" with the server._

While additive client synchronization mode might cover the majority of your project's needs, you could find scenarios where you need to be a bit more selective with the scenes a client should keep loaded and the scenes that should be unloaded. There are two additional settings that you can use (only when client synchronization mode set to `LoadSceneMode.Additive`) to gain further control over this process.

### NetworkSceneManager.PostSynchronizationSceneUnloading
When this flag is set on the client side any scenes that were not synchronized with the server or host will be unloaded. This can be useful if your project primarily uses `LoadSceneMode.Additive` to load the majority of your scenes (i.e. sometimes referred to a "bootstrap scene loading approach").  A client could have additional scenes specific to a portion of your game/project from a previous game network session that are not pertinent in a newly established game network session. Any scenes that are not required to become fully synchronized with the current server or host will get unloaded upon the client completing the synchronization process.

### NetworkSceneManager.VerifySceneBeforeUnloading
When `NetworkSceneManager.PostSynchronizationSceneUnloading` is set to `true` and the client synchronization mode is `LoadSceneMode.Additive`, if this callback is set then for each scene that is about to be unloaded the set callback will be invoked. If it returns `true` the scene will be unloaded and if it returns `false` the scene will remain loaded. This is useful if you have specific scenes you want to persist (menu interfaces etc.) but you also want some of the unused scenes (i.e. not used during synchronization) to be unloaded.

![image](../../images/ClientSynchronizationModeAdditive2.png)

Let's take the previous client synchronization mode scenario into consideration with the client side having `NetworkSceneManager.PostSynchronizationSceneUnloading` set to `true` and registering a callback for `NetworkSceneManager.VerifySceneBeforeUnloading` where it returns false when client synchronization attempts to unload the Menu UI Scene. Let's also assume this game allows clients to join other network sessions while still in a current network session and that the client had just come from a previous game where the server was still on "level 1" (so the client has the level 1 scene still loaded). When the client begins to synchronize the two pre-loaded scenes are used for synchronization, the client still has to load the Level 3 scene, but at the end of the synchronization `NetworkSceneManager` begins to unload any remaining scenes not synchronized by the server and the client. When the Menu UI Scene is checked if it can be unloaded, the client side `NetworkSceneManager.VerifySceneBeforeUnloading` callback returns `false` so that scene remains loaded. Finally, the Level 1 scene is verified to be unloaded via `NetworkSceneManager.VerifySceneBeforeUnloading` which the user logic determines is "ok" to unload so it returns `true` and the Level 1 scene is unloaded.

Additive client synchronization can help you to reduce client synchronization times while providing you with additional capabilities tailored for additively loaded scene usage patterns.
