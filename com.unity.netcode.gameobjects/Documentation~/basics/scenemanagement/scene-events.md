# Scene events

> [!NOTE]
> If you haven't already read the [Using NetworkSceneManager](using-networkscenemanager.md) section, it's highly recommended to do so before proceeding.

## Scene Event Associations
We learned that the term "Scene Event" refers to all (associated) subsequent scene events that transpire over time after a server has initiated a load or unload Scene Event. For most cases this is true, however `SceneEventType.Synchronize` is a unique type of Scene Event that handles much more than loading or unloading a single scene.  to better understand the associations between scene event types, it's better to first see them grouped together:

### Loading:
**Initiating Scene Event**: `SceneEventType.Load`<br/>
**Associated Scene Events**:
- `SceneEventType.LoadComplete`: <br/>
signifies that a scene has been loaded locally.  Clients send this event to the server.
- `SceneEventType.LoadEventCompleted`: <br/>
signifies that the server and all clients have finished loading the scene and signifies that the Scene Event has completed.

### Unloading:
**Initiating Scene Event**: `SceneEventType.Unload`<br/>
**Associated Scene Events**:
- `SceneEventType.UnloadComplete`: <br/>
signifies that a scene has been unloaded locally.  Clients send this event to the server.
- `SceneEventType.UnloadEventCompleted`: <br/>
signifies that the server and all clients have finished unloading the scene and signifies that the Scene Event has completed.

### Synchronization:
This is automatically happens after a client is connected and approved.<br/>
**Initiating Scene Event**: `SceneEventType.Synchronize`<br/>
**Associated Scene Events**:
- `SceneEventType.SynchronizeComplete`: <br/>
signifies that the client has finished loading all scenes and locally spawned all Netcode objects.  The client sends this scene event message back to the server.  This message also includes a list of `NetworkObject.NetworkObjectId`s for all of the `NetworkObject`s the client spawned.
- `SceneEventType.ReSynchronize`: <br/>
signifies that the server determines the client needs to be "re-synchronized" because one or more `NetworkObject`s were despawned while the client was synchronizing.  This message is sent to the client with a `NetworkObject.NetworkObjectId` list for all `NetworkObject`s the client needs to despawn.


## Client Synchronization Details
While client synchronization does fall partially outside of the scene management realm, it ended up making more sense to handle the initial client synchronization via the `NetworkSceneManager` since a large part of the synchronization process involves loading scenes and synchronizing (in-scene placed and dynamically spawned) `NetworkObjects`.  
- Scene synchronization is the first thing a client processes.
  - The synchronization message includes a list of all scenes the server has loaded via the `NetworkSceneManager`.
  - The client will load all of these scenes before proceeding to the `NetworkObject` synchronization.
    - This approach was used to assure all `GameObject`, `NetworkObject`, and `NetworkBehaviour` dependencies are loaded and instantiated before a client attempts to locally spawn a `NetworkObject`.
- Synchronizing with all spawned `NetworkObjects`.
  - Typically this involves both in-scene placed and dynamically spawned `NetworkObjects`.   
    - Learn more about [Object Spawning here](..\object-spawning.md).
  - The `NetworkObject` list sent to the client is pre-ordered, by the server, to account for certain types of dependencies such as when using [Object Pooling](../../advanced-topics/object-pooling.md).
    - Typically object pool managers are in-scene placed and need to be instantiated and spawned before spawning any of its pooled `NetworkObjects` on a client that is synchronizing. As such, `NetworkSceneManager` takes this into account to assure that all `NetworkObjects` spawned via the `NetworkPrefabHandler` will be instantiated and spawned after their object pool manager dependency has been instantiated and spawned locally on the client.        
        - You can have parented in-scene placed NetworkObjects (that is, items that are picked up or consumed by players)
            - `NetworkSceneManager` uses a combination of the `NetworkObject.GlobalObjectIdHash` and the instantiating scene's handle to uniquely identify in-scene placed `NetworkObject`s.

> [!NOTE]
> With additively loaded scenes, you can run into situations where your object pool manager, instantiated when the scene it's defined within is additively loaded by the server, is leaving its spawned `NetworkObject` instances within the [currently active scene](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.GetActiveScene.html).  While assuring that newly connected clients being synchronized have loaded all of the scenes first helps to avoid scene dependency issues, this alone does not resolve issue with the `NetworkObject` spawning order.  The integrated scene management, included in Netcode for GameObjects, takes scenarios such as this into consideration.       

### The Client Synchronization Process

> [!NOTE]
> The following information isn't required information, but can be useful to better understand the integrated scene management synchronization process.

<br/>
Below is a diagram of the client connection approval and synchronization process:

![image](../images/scenemanagement_synchronization_overview.png)

Starting with the "Player" in the top right part of the above diagram, the client (Player) runs through the connection and approval process first which occurs within the `NetworkManager`.  Once approved, the server-side `NetworkSceneManager` begins the client synchronization process by sending the `SceneEventType.Synchronize` Scene Event message to the approved client.  The client then processes through the synchronization message.  Once the client is finished processing the synchronize message, it responds to the server with a `SceneEventType.SynchronizeComplete` message. At this point the client is considered "synchronized".  If the server determines any `NetworkObject` was despawned during the client-side synchronization message processing period, it will send a list of `NetworkObject` identifiers to the client via the `SceneEventType.ReSynchronize` message and the client will locally despawn the `NetworkObject`s.

> [!NOTE]
> When the server receives and processes the `SceneEventType.SynchronizeComplete` message, the client is considered connected (that is, `NetworkManager.IsConnectedClient` is set to `true`) and both the `NetworkManager.OnClientConnected` delegate handler and the scene event notification for `SceneEventType.SynchronizeComplete` are invoked locally. This can be useful to know if your server sends any additional messages to the already connected clients about the newly connected client's status (that is, a player's status needs to transition from joining to joined).

## Scene Event Notifications

### When are Load or Unload SceneEvents Truly Complete?
There are two special scene event types that generate messages for the server and all connected clients:
`SceneEventType.LoadEventCompleted` and `SceneEventType.UnloadEventCompleted`

> [!NOTE]
> Both of these server generated messages will create local notification events (on all clients and the server) that will contain the list of all client identifiers (ClientsThatCompleted) that have finished loading or unloading a scene. This can be useful to make sure all clients are synchronized with each other before allowing any netcode related game logic to begin. If a client disconnects or there is a time out, then any client that did not load or unload the scene will be included in a second list of client identifiers (ClientsThatTimedOut).

### Tracking Event Notifications (OnSceneEvent)
The following code provides an example of how to subscribe to and use `NetworkSceneManager.OnSceneEvent`. Since we want the server or host to receive all scene event notifications, we will want to subscribe immediately after we start the server or host. Each case has additional comments about each scene event type.  Starting the client is fairly straight forward and follows the same pattern by subscribing to `NetworkSceneManager.OnSceneEvent` if the client successfully started.

```csharp
public bool StartMyServer(bool isHost)
{
    var success = false;
    if (isHost)
    {
        success = NetworkManager.Singleton.StartHost();
    }
    else
    {
        success = NetworkManager.Singleton.StartServer();
    }

    if (success)
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
    }            

    return success;
}

public bool StartMyClient()
{
    var success = NetworkManager.Singleton.StartClient();
    if (success)
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
    }     
    return success;
}

private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
{
    // Both client and server receive these notifications
    switch (sceneEvent.SceneEventType)
    {
        // Handle server to client Load Notifications
        case SceneEventType.Load:
            {
                // This event provides you with the associated AsyncOperation
                // AsyncOperation.progress can be used to determine scene loading progression
                var asyncOperation = sceneEvent.AsyncOperation;
                // Since the server "initiates" the event we can simply just check if we are the server here
                if (IsServer)
                {
                    // Handle server side load event related tasks here
                }
                else
                {
                    // Handle client side load event related tasks here
                }                        
                break;
            }
        // Handle server to client unload notifications
        case SceneEventType.Unload:
            {
                // You can use the same pattern above under SceneEventType.Load here
                break;
            }
        // Handle client to server LoadComplete notifications
        case SceneEventType.LoadComplete:
            {
                // This will let you know when a load is completed
                // Server Side: receives thisn'tification for both itself and all clients
                if (IsServer)
                {                            
                    if (sceneEvent.ClientId == NetworkManager.LocalClientId)
                    {
                        // Handle server side LoadComplete related tasks here
                    }
                    else
                    {
                        // Handle client LoadComplete **server-side** notifications here
                    }
                }
                else // Clients generate thisn'tification locally
                {
                    // Handle client side LoadComplete related tasks here
                }

                // So you can use sceneEvent.ClientId to also track when clients are finished loading a scene
                break;
            }
        // Handle Client to Server Unload Complete Notification(s)
        case SceneEventType.UnloadComplete:
            {
                // This will let you know when an unload is completed
                // You can follow the same pattern above as SceneEventType.LoadComplete here

                // Server Side: receives thisn'tification for both itself and all clients
                // Client Side: receives thisn'tification for itself

                // So you can use sceneEvent.ClientId to also track when clients are finished unloading a scene
                break;
            }
        // Handle Server to Client Load Complete (all clients finished loading notification)
        case SceneEventType.LoadEventCompleted:
            {
                // This will let you know when all clients have finished loading a scene
                // Received on both server and clients
                foreach (var clientId in sceneEvent.ClientsThatCompleted)
                {
                    // Example of parsing through the clients that completed list
                    if (IsServer)
                    {
                        // Handle any server-side tasks here
                    }
                    else
                    {
                        // Handle any client-side tasks here
                    }
                }
                break;
            }
        // Handle Server to Client unload Complete (all clients finished unloading notification)
        case SceneEventType.UnloadEventCompleted:
            {
                // This will let you know when all clients have finished unloading a scene
                // Received on both server and clients
                foreach (var clientId in sceneEvent.ClientsThatCompleted)
                {
                    // Example of parsing through the clients that completed list
                    if (IsServer)
                    {
                        // Handle any server-side tasks here
                    }
                    else
                    {
                        // Handle any client-side tasks here
                    }
                }
                break;
            }
    }
}
```

> [!NOTE]
> This code can be applied to a component on your `GameObject` that has a `NetworkManager` component attached to it.  Since the `GameObject`, with the  `NetworkManager` component attached to it, is migrated into the DDOL (Dont Destroy on Load) scene, it will remain active for the duration of the network game session.  
> With that in mind, you can cache your scene events that occurred (for debug or reference purposes) and/or add your own events that other game objects can subscribe to. The general idea is that if you want to receive all notifications from the moment you start `NetworkManager` then you will want to subscribe to `NetworkSceneManager.OnSceneEvent` immediately after starting it.

Scene event notifications provide users with all NetworkSceneManager related scene events (and associated data) through a single event handler. The one exception would be scene loading or unloading progress which users can handle with a coroutine (upon receiving a Load or Unload event) and checking the `SceneEvent.AsyncOperation.progress` value over time.

> [!NOTE]
> You will want to assign the SceneEvent.AsyncOperation to a local property of the subscribing class and have a coroutine use that to determine the progress of the scene being loaded or unloaded.

You can stop the coroutine checking the progress upon receiving any of the following event notifications for the scene and event type in question: `LoadComplete`, `UnloadComplete` to handle local scene loading progress tracking. Otherwise, you should use the `LoadEventCompleted` or `UnloadEventCompleted` to assure that when your "scene loading progress" visual handler stops it will stop at ~roughly~ the same time as the rest of the connected clients.  The server will always be slightly ahead of the clients since it notifies itself locally and then broadcasts this message to all connected clients.

### SceneEvent Properties
The SceneEvent class has values that may or may not be set depending upon the `SceneEventType`.  Below are two quick lookup tables to determine which property is set for each `SceneEventType`.

**Part-1**<br/>
![image](../images/SceneEventProperties-1.png)<br/>

**Part-2** <br/>
![image](../images/SceneEventProperties-2.png)<br/>

So, the big "take-away" from the above table is that you need to understand the `SceneEventType` context of the `SceneEvent` you are processing to know which properties are valid and you can use.  As an example, it wouldn't make sense to provide the AsyncOperation for the following `SceneEventType`s:
- LoadComplete or LoadEventCompleted
- UnloadComplete or UnloadEventCompleted
- Synchronize or Resynchronize

### SceneEventType Specific Notifications
There might be a time where you aren't interested in all of the details for each scene event type that occurs.  As it just so happens, `NetworkSceneManager` includes a single delegate handler for each `SceneEventType` that is only triggered for the associated `SceneEventType`.
You can explore the [NetworkSceneManager](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.SceneEventType.html) for a full listing of the corresponding single `SceneEventType` events.
Some examples:
- NetworkSceneManager.OnLoad:  Triggered when for `OnLoad` scene events.
- NetworkSceneManager.OnUnload:  Triggered when for `OnUnload` scene events.
- NetworkSceneManager.OnSynchronize: Triggered when a client begins synchronizing.

> [!NOTE]
> The general idea was to provide several ways to get scene event notifications. You might have a component that needs to know when a client is finished synchronizing on the client side but you don't want that component to receive notifications for loading or unloading related events.  Under this scenario you would subscribe to the `NetworkManager.OnSynchronizeComplete` event on the client-side.

### When is it "OK" to Subscribe?
Possibly the more important aspect of scene event notifications is knowing when/where to subscribe.  The recommended time to subscribe is immediately upon starting your `NetworkManager` as a client, server, or host.  This will avoid problematic scenarios like trying to subscribe to the `SceneEventType.Synchronize` event within an overridden `NetworkBehaviour.OnNetworkSpawn` method of your `NetworkBehaviour` derived child class.  The reason that is "problematic" is that the `NetworkObject` has to be spawned before you can subscribe to and receive events of type `SceneEventType.Synchronize` because that will occur before anything is spawned.  Additionally, you would only receive notifications of any scenes loaded after the scene that has the `NetworkObject` (or the object that spawns it) is loaded.

An example of subscribing to `NetworkSceneManager.OnSynchronize` for a client:
```csharp
        public bool ConnectPlayer()
        {
            var success = NetworkManager.Singleton.StartClient();
            if (success)
            {
                NetworkManager.Singleton.SceneManager.OnSynchronize += SceneManager_OnSynchronize;
            }           
            return success;
        }

        private void SceneManager_OnSynchronize(ulong clientId)
        {
            Debug.Log($"Client-Id ({clientId}) is synchronizing!");
        }
```
_The general idea is that this would be something you would want to do immediately after you have started the server, host, or client._
