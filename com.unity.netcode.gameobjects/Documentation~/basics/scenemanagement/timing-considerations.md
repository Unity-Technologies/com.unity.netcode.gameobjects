# Timing considerations

> [!NOTE]
> If you haven't already read the [Using NetworkSceneManager](using-networkscenemanager.md) and/or [Scene Events](scene-events.md) sections, it's highly recommended to do so before proceeding.

Netcode for GameObjects handles many of the more complicated aspects of scene management.  This section is tailored towards those who want to better understand the client-server communication sequence for scene events as they occur over time.
In each diagram, you will see two types of arrows:
- Horizontal arrows: Denotes a progression to the next state and/or event.
- Diagonal arrows: Denotes a message being sent (server to client or vice versa).
    - These arrows will have the name of the message being sent or the type of the scene event message being sent.

## Client synchronization

The below diagram, Client Synchronization Updates, steps you through the entire client synchronization process from starting the client all the way to the client being fully synchronized and connected.  

![](../../images/sequence_diagrams/SceneManagement/ClientSyncUpdates_Light.png)

Above, we can see that the client will first send a connection request to the server that will process the request, handle the approval process via ConnectionApprovalCallback (if connection approval is enabled), and if approved the server will:
- Send a connection approved message back to the client
    - This only means the connection is approved, but the client isn't yet synchronized.
- Start a `SceneEventType.Synchronize` scene event
    - Build the synchronization message ("Synchronize NetworkObjects") that will contain all of the
    information a client will need to properly synchronize with the current netcode game session.
    - Send the scene event message to the client

Throughout the integrated scene management sections, we have used the term "client synchronization period" to denote the time it takes a client to:
- Receive the synchronization message
- Process the synchronization message
- Send the `SceneEventType.SynchronizeComplete` message back to the server<br/>
If you look at the server part of the timeline in the above diagram, the "client synchronization period" is everything that occurs within the green bracketed section between "Synchronize NetworkObjects" and "Synchronization" on the server-side.  On the client-side, it includes the time it takes to receive the `SceneEventType.Synchronize` message, "Handle (the) SceneEvent", and the time it takes for the server to receive the `SceneEventType.SynchronizeComplete` that is sent by the client once it has completed synchronizing.

Upon receiving the synchronization event message, the client processes it, and when finished the client sends the `SceneEventType.SynchronizeComplete` scene event message.  The client-side will (within the same frame) invoke the following local `NetworkSceneManager` callbacks (if subscribed to):
- `OnSceneEvent` with the scene event type being set to `SceneEventType.SynchronizeComplete`
- `OnSynchronizeComplete`

> [!NOTE]
> Take note that after the client finishes processing the synchronization event, the server lags, in regards to when callbacks are triggered, behind the client. Typically this client-server latency is half RTT, and so you should be aware that just because a client hasn'tified locally that it has finished there is a small period of time (commonly in the 10's to 100's of milliseconds) where the server is still unaware of the client having finished synchronizing.  

### Client-side synchronization timeline

Now that we have covered the high-level synchronization process, we can dive a little deeper into what happens on the client side as it processes the synchronize message.  The below sub-diagram, "Scene Event Synchronization Timeline", provides you with a more detailed view of how the client processes the synchronize message:

![](../../images/sequence_diagrams/SceneManagement/SceneEventSynchronizationTimeline_Light.png)

You can see that upon receiving the message, the client appears to be iterating over portions of the data included in the synchronize message.  This is primarily the asynchronous scene loading phase of the client synchronization process. This means the more scenes loaded, the more a client will be required to load which means the Client Synchronization Period is directly proportional to the number of scene being loaded and the size of each scene being loaded.  Once all scenes are loaded, the client will then locally spawn all `NetworkObject`s.  As a final step, the client sends the list of all `NetworkObjectId`s it spawned as part of its `SceneEventType.SynchronizeComplete` scene event message so the server can determine if it needs to resynchronize the client with a list of any `NetworkObjects` that might have despawned during the Client Synchronization Period.

## Loading Scenes

### LoadSceneMode.Additive
Looking at the timeline diagram below, "Loading an Additive Scene", we can see that it includes a server, two clients, and that the server will always precede all clients when it comes to processing the scene loading event. The big-picture this diagram is conveying is that only when the server has finished loading the scene and spawning any in-scene placed `NetworkObject`s, instantiated by the newly loaded scene, will it send the scene loading event message to the clients.

Another point of interest in the below diagram is how Client 1 receives the scene loading event, processes it, and then responds with a `SceneEventType.LoadComplete` scene event message before client 2. This delta between client 1 and client 2 represents the varying client-server latencies and further enforces the underlying concept behind the `SceneEventType.LoadEventCompleted` message.  Once a server has received all `SceneEventType.LoadComplete` messages from the connected clients, it will then broadcast the `SceneEventType.LoadEventCompleted` message to all connected clients.  At this point, we can consider the scene loading event (truly) complete and all connected clients are able to receive and process netcode messages.

![](../../images/sequence_diagrams/SceneManagement/LoadingAdditiveScene_Light.png)

> [!NOTE]
> While a client can start sending the server messages (including NetworkVariable changes) upon local `SceneEventType.LoadComplete` event notifications, under more controlled testing environments where the network being used has little to no latency (that is, using loopback with multiple instances running on the same system or using your LAN), this approach won't expose latency related issues. Even though the timing might "work out" under controlled low latency conditions you can still run into edge case scenarios where if a client approaches or exceeds a 500ms RTT latency you can potentially run into issues.

> [!NOTE]
> It is recommended that if your project's design requires that one or more `NetworkBehaviour`s immediately send any form of client to server message (that is, changing a `NetworkVariable`, sending an RPC, sending a custom message, etc.) upon a client being locally notified of a `SceneEventType.LoadComplete` then you should test with artificial/simulated network conditions.  
> [Learn More About Simulating NetworkConditions Here](../../tutorials/testing/testing_with_artificial_conditions.md)


### LoadSceneMode.Single (a.k.a. Scene Switching)
Loading a scene in `LoadSceneMode.Single` mode via `NetworkSceneManager` is almost exactly like loading a scene additively.  The primary difference between additively loading and single mode loading is that when loading a scene in single mode:
- all currently loaded scenes are unloaded
- all `NetworkObject`s that have `DestroyWithScene` set to `true` will be despawned and destroyed.

How you load scenes is up to your project/design requirements.

- **Boot Strap Usage Pattern (Additive Loading Only)**
    - Your only single mode loaded scene is the first scene loaded (that is, scene index of 0 in the scenes in build list within your project's build settings).
    - Because your single mode loaded scene is automatically loaded, the server and all clients will already have this scene loaded
        - To prevent clients from loading the bootstrap scene, you should use server-side [scene validation](using-networkscenemanager.md#scene-validation)
    - All other scenes are loaded additively
    - There is no real need to preserve any `NetworkObject` you want to persist when loading a scene additively.  
    - You need to keep track of the scenes loaded by `NetworkSceneManager` to be able to unload them.

- **Scene Switch Usage Pattern (Single and Additive Loading)**
    - Typically this usage pattern is desirable when your project's design separates "areas" by a primary scene that may have companion additively loaded scenes.
    - Any scenes loaded by `NetworkSceneManager`, before scene switching, will be unloaded and any `NetworkObject` that has the `DestroyWithScene` set to `true` will be destroyed.
        - If `DestroyWithScene` is set to `false` it will be "preserved" (_see the sub-diagram "Load New Scene Timeline" below_)

![](../../images/sequence_diagrams/SceneManagement/SwitchingToNewScene_Light.png)

## Load New Scene Timeline (Sub-Diagram)

Both scene loading diagrams (Additive and Single) refer to the below sub-diagram that provides additional details of the scene loading process.
> [!NOTE]
>When looking at the below sub-diagram, both single and additive scene loading modes use close to the exact same flow with the exception that additively loaded scenes only flow through the four middle steps that are grouped together by the light red filled region highlighted by red dashed lines.  When loading a scene additively, no other scenes are unloaded nor are any NetworkObjects moved into the DDoL temporarily. This setting, by default, is true for dynamically spawned `NetworkObject`s unless otherwise specified when using `NetworkObject.Spawn`, `NetworkObject.SpawnWithOwnership`, or `NetworkObject.SpawnAsPlayerObject`.  In-scene placed `NetworkObject`'s, by default, will be destroyed with the scene that instantiated them.  At any point, within a `NetworkBehaviour` you can change the `NetworkObject.DestroyWithScene` property.

![](../../images/sequence_diagrams/SceneManagement/LoadNewSceneTimeline_Light.png)

**Load New Scene Additively**
1. Starts loading the scene
2. During the scene loading process, in-scene placed `NetworkObject`s are instantiated and their `Awake` and then `Start` methods are invoked (in that order).
3. Scene loading completes.
4. All in-scene placed `NetworkObject`s are spawned.

Step 3 above signifies that the `UnityEngine.SceneManagement.SceneManager` has finished loading the scene. If you subscribe to the `UnityEngine.SceneManagement.SceneManager.sceneLoaded` event, then step #3 would happen on the same frame that your subscribed handler is invoked. **Don't use this event** as a way to determine that the current Load SceneEvent has completed. <br/> <center>_Doing so will result in unexpected results that most commonly are associated with "yet-to-be-spawned" (locally) `NetworkObject`'s and/or their related `NetworkBehaviour` dependencies._</center> When using Netcode Integrated SceneManagement, it's recommended to use the `NetworkSceneManager` scene events to determine when the "netcode scene loading event" has completed locally or for all clients.


**Load New Scene Single (a.k.a "Scene Switching")**
1. All `NetworkObjects` that have their `DestroyWithScene` property set to `false` are migrated into the DDoL (temporarily).
2. All currently loaded scenes are unloaded.  If you loaded any scenes additively, they will be automatically unloaded.
3. _(refer to the 4 steps outlined above)_
4. After any newly instantiated `NetworkObject`s are spawned, the newly loaded scene is set as the currently active scene and then the `NetworkObject`s that were previously migrated into th DDoL (from step 1) are now migrated into the newly loaded scene.

## Unloading a Scene
Primarily, this applies to unloading additively loaded scenes via th `NetworkSceneManager.UnloadScene` method. to unload a scene it must:
- have been additively loaded by `NetworkSceneManager`
- still be loaded


### Unloading an Additive Scene
If you look at the below diagram, "Unloading an Additive Scene", you will see a similar flow as that of loading a scene.  The server still initiates the `SceneEventType.Unload` scene event and won't send this message to clients until it has completed the `Unload` scene event locally.

![](../../images/sequence_diagrams/SceneManagement/UnloadingAdditiveScene_Light.png)

### Unloading Scenes Timeline:
Review over the below diagram and take note of the following things:
- **Server Side:**
    - When a server starts the `SceneEventType.Unload` event, Unity will naturally being to destroy all `GameObjects` in the scene being unloaded.  
        - If a `GameObject` has a `NetworkObject` component attached to it and it's still considered spawned at the time the `GameObject` is destroyed, then the `NetworkObject` will be despawned before the `GameObject` being destroyed.
            - This will cause a series of server-to-client despawn messages to be sent to all clients.
- **Client Side:**
    - While a server is unloading a scene, the client can begin to receive a bunch of despawn messages for the `NetworkObject`s being destroyed on the server-side while the scene is being unloaded.
        - By the time a client receives the `SceneEventType.Unload` scene event message, it well can have no remaining `NetworkObject`s in the scene being unloaded.  This won't impact the client-side scene unloading process, but it's useful to know that this will happen.

![](../../images/sequence_diagrams/SceneManagement/UnloadingSceneTimeline_Light.png)
