# Using NetworkSceneManager

## Netcode for GameObjects Integrated Scene Management

### The `NetworkSceneManager` Provides You With:
- An existing framework that supports both the bootstrap and scene transitioning scene management usage patterns.
- Automated client synchronization that occurs when a client is connected and approved.  
  - All scenes loaded via `NetworkSceneManager` will be synchronized with clients during client synchronization.
    - _Later in this document, you will learn more about using scene validation to control what scenes are synchronized on the client, server, or both side(s)_.
  - All spawned Netcode objects are synchronized with clients during client synchronization.
- A comprehensive scene event notification system that provides you with a plethora of options.
  - It provides scene loading, unloading, and client synchronization events ("Scene Events") that can be tracked on both the client and server.
    - The server is notified of all scene events (including the clients')
      - The server tracks each client's scene event progress (scene event progress tracking)
    - Clients receive scene event messages from the server, trigger local notifications as it progresses through a Scene Event's, and send local notifications to the server.
      - _clients **aren't** aware of other clients' scene events_
 - In-Scene placed NetworkObject synchronization

> [!NOTE]
> In-Scene placed `NetworkObject`s can be used in many ways and are treated uniquely from that of dynamically spawned `NetworkObject`s.  An in-scene placed `NetworkObject` is a GameObject with a `NetworkObject` and typically at least one `NetworkBehaviour` component attached to a child of or the same `GameObject`.  it's recommended to read through all integrated scene management materials (this document, [Scene Events](scene-events.md), and [Timing Considerations](timing-considerations.md)) before learning about more advanced [In-Scene (placed) NetworkObjects](inscene-placed-networkobjects.md) topics.

All of these scene management features (and more) are handled by the [`NetworkSceneManager`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkSceneManager.html).

### Accessing `NetworkSceneManager`
The `NetworkSceneManager` lives within the `NetworkManager` and is instantiated when the `NetworkManager` is started.
`NetworkSceneManager` can be accessed in the following ways:
-  From within a `NetworkBehaviour` derived component, you can access it by using `NetworkManager.SceneManager`
-  From anything else that does not already have a reference to `NetworkManager`, you can access it using `NetworkManager.Singleton.SceneManager`.

**Here are some genral rules about accessing and using `NetworkSceneManager`:**
- Don't try to access the `NetworkSceneManager` when the `NetworkManager` is shutdown (it won't exist).
  - The `NetworkSceneManager` is only instantiated when a `NetworkManager` is started.
- As a server:
  -  Any scene you want synchronized with currently connected or late joining clients **must** be loaded via the `NetworkSceneManager`
    - If you use the `UnityEngine.SceneManagement.SceneManager` during a netcode enabled game session, then those scenes won't be synchronized with currently connected clients.
      - A "late joining client" is a client that joins a game session that has already been started and there are already one or more clients connected.
        - _All netcode aware objects spawned before a late joining client will be synchronized with a late joining client._
      - If you load a scene on the server-side using `UnityEngine.SceneManagement.SceneManager` then it will be synchronized with late joining clients unless you use server-side scene validation (see [Scene Validation](using-networkscenemanager.md#scene-validation)).
        - _It is a best practice to use `NetworkSceneManager` when loading scenes _
  -  The server does not require any clients to be currently connected before it starts loading scenes via the `NetworkSceneManager`.
    - Any scene loaded via `NetworkSceneManager` will always default to being synchronized with clients.
      - _Scene validation is explained later in this document._

> [!NOTE]
> Don't try to access the `NetworkSceneManager` when the `NetworkManager` is shutdown.  The `NetworkSceneManager` is only instantiated when a `NetworkManager` is started. _This same rule is true for all Netcode systems that reside within the `NetworkManager`_.

### Loading a Scene
In order to load a scene, there are four requirements:
1. The `NetworkManager` instance loading the scene must be a host or server.
2. The `NetworkSceneManager.Load` method is used to load the scene.
3. The scene being loaded must be registered with your project's [build settings scenes in build list](https://docs.unity3d.com/Manual/BuildSettings.html)
4. A Scene Event can't already be in progress.

#### Basic Scene Loading Example
Imagine that you have an in-scene placed NetworkObject, let's call it "ProjectSceneManager", that handles your project's scene management using the `NetworkSceneManager` and you wanted your server to load a scene when the ProjectSceneManager is spawned.  
In its simplest form, it can look something like:
```csharp
public class ProjectSceneManager : NetworkBehaviour
{      
  /// INFO: You can remove the #if UNITY_EDITOR code segment and make SceneName public,
  /// but this code assures if the scene name changes you won't have to remember to
  /// manually update it.
#if UNITY_EDITOR
  public UnityEditor.SceneAsset SceneAsset;
  private void OnValidate()
  {
      if (SceneAsset != null)
      {
          m_SceneName = SceneAsset.name;
      }
  }
#endif
  [SerializeField]
  private string m_SceneName;

  public override void OnNetworkSpawn()
  {
      if (IsServer && !string.IsNullOrEmpty(m_SceneName))
      {
          var status = NetworkManager.SceneManager.LoadScene(m_SceneName, LoadSceneMode.Additive);
          if (status != SceneEventProgressStatus.Started)
          {
              Debug.LogWarning($"Failed to load {m_SceneName} " +
                    $"with a {nameof(SceneEventProgressStatus)}: {status}");
          }
      }
  }
}
```
In the above code snippet, we have a `NetworkBehaviour` derived class, `ProjectSceneManager`, that has a public `SceneAsset` property (editor only property) that specifies the scene to load. In the `OnNetworkSpawn` method, we make sure that only the server loads the scene and we compare the `SceneEventProgressStatus` returned by the `NetworkSceneManager.Load` method to the `SceneEventProgressStatus.Started` status.  If we received any other status, then typically the name of the status provides you with a reasonable clue as to what the issue might be.

### Scene Events and Scene Event Progress
The term "Scene Event" refers to all subsequent scene events that transpire over time after a server has started a `SceneEventType.Load` ("load") or `SceneEventType.Unload` ("unload") Scene Event. For example, a server might load a scene additively while clients are connected. The following high-level overview provides you with a glimpse into the events that transpire during a load Scene Event(_it assumes both the server and clients have registered for scene event notifications_):
- Server starts a `SceneEventType.Load` event
  - The server begins asynchronously loading the scene additively
    - The server will receive a local notification that the scene load event has started
  - Once the scene is loaded, the server spawns any in-scene placed `NetworkObjects` locally
    - After spawning, the server will send the `SceneEventType.Load` message to all connected clients along with information about the newly instantiated and spawned `NetworkObject`s.
    - The server will receive a local `SceneEventType.LoadComplete` event
      - This only means the server is done loading and spawning `NetworkObjects` instantiated by the scene being loaded.
- The clients receive the scene event message for the `SceneEventType.Load` event and begin processing it.
  - Upon starting the scene event, the clients will receive a local notification for the SceneEventType.Load event.
  - As each client finishes loading the scene, they will generate a `SceneEventType.LoadComplete` event
    - The client sends this event message to the server.
    - The client is notified locally of this scene event.
      - This notification only means that it has finished loading and synchronizing the scene, but does not mean all other clients have!
- The server receives the `SceneEventType.LoadComplete` events from the clients
  - When all `SceneEventType.LoadComplete` events have been received, the server will generate a `SceneEventType.LoadEventCompleted` notification
    - This notification is triggered locally on the server
    - The server broadcasts this scene event to the clients
- Each client receives the `SceneEventType.LoadEventCompleted` event
  - At this point all clients have completed the `SceneEventType.Load` event and are synchronized with all newly instantiated and spawned `NetworkObjects`.

The purpose behind the above outline is to show that a Scene Event can lead to other scene event types being generated and that the entire sequence of events that transpire occur over a longer period of time than if you were loading a scene in a single player game.

> [!NOTE]
> When you receive the `SceneEventType.LoadEventCompleted` or the `SceneEventType.SynchronizeComplete` you know that the server or clients can start invoking netcode specific code (that is, sending Rpcs, updating `NetworkVariable`s, etc.).  Alternately, `NetworkManager.OnClientConnectedCallback` is triggered when a client finishes synchronizing and can be used in a similar fashion.  _However, that only works for the initial client synchronization and not for scene loading or unloading events._

> [!NOTE]
> Because the `NetworkSceneManager` still has additional tasks to complete once a scene is loaded, it isn't recommended to use `UnityEngine.SceneManagement.SceneManager.sceneLoaded` or `UnityEngine.SceneManagement.SceneManager.sceneUnloaded` as a way to know when a scene event has completed.  These two callbacks will occur before `NetworkSceneManager` has finished the final processing after a scene is loaded or unloaded and you can run into timing related issues. If you are using the netcode integrated scene management, then it's highly recommended to subscribe to the `NetworkSceneManager`'s scene event notifications.

#### Scene Event Notifications
You can be notified of scene events by registering in one of two ways:
1. Receive all scene event notification types: `NetworkSceneManager.OnSceneEvent`
2. Receive only a specific scene event notification type: [`NetworkSceneManager`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkSceneManager.html#events) has one for each [`SceneEventType`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.SceneEventType.html)<br/>

> [!NOTE]
> Receiving (via subscribing to the associated event callback) only specific scene event notification types does not change how a server or client receives and processes notifications.  

**Receiving All Scene Event Notifications**
Typically, this is used on the server side to receive notifications for every scene event notification type for both the server and clients. You can receive all scene event type notifications by subscribing to the `NetworkSceneManager.OnSceneEvent` callback handler.  

**Receiving A Specific Scene Event Notification**
Typically, this is used with clients or components that might only need to be notified of a specific scene event type.  There are 9 scene event types and each one has a corresponding "single event type" callback handler in `NetworkSceneManager`.  

**As an example:**
You might want to register for the `SceneEventType.LoadEventCompleted` scene event type to know, from a client perspective, that the server and all other clients have finished loading a scene.  This notification lets you know when you can start performing other netcode related actions on the newly loaded and spawned `NetworkObject`s.

#### Scene Event Progress Status
As we discussed in the earlier code example, it's important to check the status returned by `NetworkSceneManager.Load` to make sure your scene loading event has started.  The following is a list of all [SceneEventProgressStatus](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.SceneEventProgressStatus.html) `enum` values with some additional helpful information:
- Started
  - The scene event has started (success)
- SceneNotLoaded
  - This is returned when you attempt to unload a scene that isn't loaded (or no longer is loaded)
- SceneEventInProgress
  - This is returned if you attempt to start a new scene event (loading or unloading) while one is still in progress
- InvalidSceneName
  - This can happen for one of the two reasons:
    - The scene name isn't spelled correctly or the name of the scene changed
    - The scene name isn't in your project's scenes in build list
- SceneFailedVerification
  - This is returned if you fail scene verification on the server side (more about scene verification later)
- InternalNetcodeError
  - Currently, this error will only happen if the scene you are trying to unload was never loaded by the `NetworkSceneManager`.

### Unloading a Scene
Now that we understand the loading process, scene events, and can load a scene additively, the next step is understanding the integrated scene management scene unloading process.  to unload a scene, here are the requirements:
1. The `NetworkManager` instance unloading the scene should have already been started in host or server mode and already loaded least one scene additively.<br/>
  1.1 Only additively loaded scenes can be unloaded.
  1.2 You can't unload the currently active scene (see info dialog below)
2. The `NetworkSceneManager.Unload` method is used to unload the scene
3. A Scene Event can't already be in progress

> [!NOTE]
> Unloading the currently active scene, in Netcode, is commonly referred to as "scene switching" or loading another scene in `LoadSceneMode.Single` mode.  This will unload all additively loaded scenes and upon the new scene being loaded in `LoadSceneMode.Single` mode it's set as the active scene and the previous active scene is unloaded.

#### Basic Scene Unloading Example
Below we are taking the previous scene loading example, the `ProjectSceneManager` class, and modifying it to handle unloading.  This includes keeping a reference to the `SceneEvent.Scene` locally in our class because `NetworkSceneManager.Unload` requires the `Scene` to be unloaded.  

**Below is an example of how to:**
- Subscribe the server to `NetworkSceneManager.OnSceneEvent` notifications.
- Save a reference to the `Scene` that was loaded.
- Unload a scene loaded additively by `NetworkSceneManager`.
- Detect the scene event types associated with loading and unloading.

```csharp
public class ProjectSceneManager : NetworkBehaviour
{
    /// INFO: You can remove the #if UNITY_EDITOR code segment and make SceneName public,
    /// but this code assures if the scene name changes you won't have to remember to
    /// manually update it.
#if UNITY_EDITOR
    public UnityEditor.SceneAsset SceneAsset;
    private void OnValidate()
    {
        if (SceneAsset != null)
        {
            m_SceneName = SceneAsset.name;
        }
    }
#endif
    [SerializeField]
    private string m_SceneName;
    private Scene m_LoadedScene;

    public bool SceneIsLoaded
    {
        get
        {
            if (m_LoadedScene.IsValid() && m_LoadedScene.isLoaded)
            {
                return true;
            }
            return false;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && !string.IsNullOrEmpty(m_SceneName))
        {
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            var status = NetworkManager.SceneManager.LoadScene(m_SceneName, LoadSceneMode.Additive);
            CheckStatus(status);
        }

        base.OnNetworkSpawn();
    }

    private void CheckStatus(SceneEventProgressStatus status, bool isLoading = true)
    {
        var sceneEventAction = isLoading ? "load" : "unload";
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning($"Failed to {sceneEventAction} {m_SceneName} with" +
                $" a {nameof(SceneEventProgressStatus)}: {status}");
        }
    }

    /// <summary>
    /// Handles processing notifications when subscribed to OnSceneEvent
    /// </summary>
    /// <param name="sceneEvent">class that has information about the scene event</param>
    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        var clientOrServer = sceneEvent.ClientId == NetworkManager.ServerClientId ? "server" : "client";
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadComplete:
                {
                    // We want to handle this for only the server-side
                    if (sceneEvent.ClientId == NetworkManager.ServerClientId)
                    {
                        // *** IMPORTANT ***
                        // Keep track of the loaded scene, you need this to unload it
                        m_LoadedScene = sceneEvent.Scene;
                    }
                    Debug.Log($"Loaded the {sceneEvent.SceneName} scene on " +
                        $"{clientOrServer}-({sceneEvent.ClientId}).");
                    break;
                }
            case SceneEventType.UnloadComplete:
                {
                    Debug.Log($"Unloaded the {sceneEvent.SceneName} scene on " +
                        $"{clientOrServer}-({sceneEvent.ClientId}).");
                    break;
                }
            case SceneEventType.LoadEventCompleted:
            case SceneEventType.UnloadEventCompleted:
                {
                    var loadUnload = sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted ? "Load" : "Unload";
                    Debug.Log($"{loadUnload} event completed for the following client " +
                        $"identifiers:({sceneEvent.ClientsThatCompleted})");
                    if (sceneEvent.ClientsThatTimedOut.Count > 0)
                    {
                        Debug.LogWarning($"{loadUnload} event timed out for the following client " +
                            $"identifiers:({sceneEvent.ClientsThatTimedOut})");
                    }
                    break;
                }
        }
    }

    public void UnloadScene()
    {
        // Assure only the server calls this when the NetworkObject is
        // spawned and the scene is loaded.
        if (!IsServer || !IsSpawned || !m_LoadedScene.IsValid() || !m_LoadedScene.isLoaded)
        {
            return;
        }

        // Unload the scene
        var status = NetworkManager.SceneManager.UnloadScene(m_LoadedScene);
        CheckStatus(status, false);
    }
}
```
Really, if you take away the debug logging code the major differences are:
- We store the Scene loaded when the server receives its local `SceneEventType.SceneEventType.LoadComplete` notification
- When the `ProjectSceneManager.UnloadScene` method is invoked (_assuming this occurs outside of this class_) the `ProjectSceneManager.m_LoadedScene` is checked to make sure it's a valid scene and that it's indeed loaded before we invoke the `NetworkSceneManager.Unload` method.

### Scene Validation
Sometimes you might need to prevent the server or client from loading a scene under certain conditions.  Here are a few examples of when you might do this:
- One or more game states determine if a scene is loaded additively
  - Typically, this is done on the server-side.  
- The scene is already pre-loaded on the client
  - Typically, this is done on the client-side.
- Security purposes
  - As we mentioned earlier, `NetworkSceneManager` automatically considers all scenes in the build settings scenes in build list valid scenes to be loaded.  You might use scene validation to prevent certain scenes from being loaded that can cause some form of security and/or game play issue.
    - it's common to do this on either the server or client side depending upon your requirements/needs.

**Usage Example**
The below example builds upon the previous example's code, and adds some psuedo-code for server-side scene validation:
```csharp
        private bool ServerSideSceneValidation(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            // Comparing against the name or sceneIndex
            if (sceneName == "SomeSceneName" || sceneIndex == 3)
            {
                return false;
            }

            // Don't allow single mode scene loading (that is, bootstrap usage patterns might implement this)
            if (loadSceneMode == LoadSceneMode.Single)
            {
                return false;
            }

            return true;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && !string.IsNullOrEmpty(m_SceneName))
            {
                NetworkManager.SceneManager.VerifySceneBeforeLoading = ServerSideSceneValidation;
                NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
                var status = NetworkManager.SceneManager.LoadScene(m_SceneName, LoadSceneMode.Additive);
                CheckStatus(status);
            }

            base.OnNetworkSpawn();
        }
```
The callback is the first thing invoked on the server-side when invoking the `NetworkSceneManager.Load` method.  If the scene validation fails, `NetworkSceneManager.Load` will return `SceneEventProgressStatus.SceneFailedVerification` and the scene event is cancelled.

> [!NOTE]
> **Client-Side Scene Validation**<br/>
> This is where you need to be cautious with scene validation, because any scene that you don't validate on the client side should not contain Netcode objects that are considered required dependencies for a connecting client to properly synchronize with the current netcode (game) session state.  

### Dynamically Generated Scenes
You might find yourself in a scenario where you just need to dynamically generate a scene.  A common use for dynamically generated scenes is when you need to dynamically generate collision geometry that you wish to only create on the server-host side. For this scenario you most likely would only want the server to have this scene loaded, but you might run into issues when synchronizing clients.  For single player games, you can just create a new scene at runtime, dynamically generate the collision geometry, add the collision geometry to the newly created scene, and everything works out.  With Netcode for GameObjects there are two extra steps you need to take to assure you don't run into any issues:
- Create an empty scene for each scene you plan on dynamically generating and add them to the "Scenes in Build" list found within the "Build Settings".
  - For this example we would only need one (that is, we might call the scene "WorldCollisionGeometry")
- Have the server-host register for `NetworkManager.SceneManager.VerifySceneBeforeLoading` handler and return false when one of the blank scene names is being validated as a valid scene for a client to load.
  - For this example we would return false any time `VerifySceneBeforeLoading` was invoked with the scene name "WorldCollisionGeometry".

 > [!NOTE]
 > This only works under the scenario where the dynamically generated scene is a server-host side only scene unless you write server-side code that sends enough information to a newly connected client to replicate the dynamically generated scene via RPC, a custom message, a `NetworkVariable`, or an in-scene placed NetworkObject that is in a scene, other than the dynamically generated one, and has a `NetworkBehaviour` component with an overridden `OnSynchronize` method that includes additional serialization information about how to construct the dynamically generated scene.  The limitation on this approach is that the scene should not contain already spawned `NetworkObject`s as those won't get automatically synchronized when a client first connects.

### Active Scene Synchronization
Setting `NetworkSceneManager.ActiveSceneSynchronizationEnabled` to true on the host or server instance will automatically synchronize both connected and late joining clients with changes in the active scene by the server or host. When enabled, the server or host instance subscribes to the `SceneManager.activeSceneChanged` event and generates `SceneEventType.ActiveSceneChanged` messages when the active scene is changed. Disabling this property just means the server or host instance unsubscribes from the event and will no longer send `SceneEventType.ActiveSceneChanged` messages to keep client's synchronized with the server or host's currently active scene.

This typically is most useful when the `NetworkSceneManager.ClientSynchronizationMode` is set to `LoadSceneMode.Additive` via the `NetworkSceneManager.SetClientSynchronizationMode` method on a server or host instance.

See Also: [Client Synchronization Mode](client-synchronization-mode.md)


### What Next?
We have covered how to access the `NetworkSceneManager`, how to load and unload a scene, provided a basic overview on scene events and notifications, and even briefly discussed in-scene placed `NetworkObject`s.  You now have the fundamental building-blocks one needs to learn more advanced integrated scene management topics.  
_We recommend proceeding to the next integrated scene management topic, "Client Synchronization Mode", in the link below._

<!-- Explore the [Netcode Scene Management Golden Path](link) for step-by-step examples of additive scene loading and management. -->
