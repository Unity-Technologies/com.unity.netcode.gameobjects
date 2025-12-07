# Dynamic Prefabs sample

# Dynamic addressables Network Prefabs sample

The [DynamicAddressablesNetworkPrefabs Sample](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/main/Basic/DynamicAddressablesNetworkPrefabs) showcases the available ways you can use dynamic Prefabs to dynamically load network Prefabs at runtime, either pre-connection or post-connection. Doing so allows you to add new spawnable NetworkObject Prefabs to Netcode for GameObjects.

This sample covers a variety of possible ways you can use dynamically loaded network Prefabs:

- **Pre-loading**: Add dynamic Prefabs to all peers before starting the connection.
- **Connection approval**: Apply server-side validation to late-joining clients after loading dynamic Prefabs.
- **Server-authoritative pre-load all Prefabs asynchronously**: Simplify server spawn management by having the server send a load request to all clients to load a set of network Prefabs.
- **Server-authoritative try spawn synchronously**: Ensure all connected clients individually load a network Prefab before spawning it on the server.
- **Server-authoritative network visibility spawning**: Spawn a network Prefab server-side as soon as it has loaded it locally, and only change the visibility of the spawned NetworkObject when each client loads the Prefab load.

There's also the **APIPlayground**, which serves as an API playground that implements all post-connection uses together.

:::note
**Note**: This sample leverages [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/index.html) to load dynamic Prefabs.
:::

### Scene 00_Preloading dynamic prefabs

The `00_Preloading Dynamic Prefabs` scene is the simplest implementation of a dynamic Prefab. It instructs all game instances to load a network Prefab (it can be just one, it can also be a set of network Prefabs) and inject them to a NetworkManager's NetworkPrefabs list before starting the server. What's important is that it doesn't matter where the Prefab comes from. It can be a simple Prefab or it can be an Addressable - it's all the same.

This is the lesser intrusive option for your development, as you don't have any extra spawning and Addressables management to perform later in your game.

Here, the sample serializes the AssetReferenceGameObject to this class, but ideally you want to authenticate players when your game starts up and have them fetch network Prefabs from services such as UGS (see [Remote Config](https://docs.unity.com/remote-config)). You should also note that this is a technique that can serve to decrease the install size of your application, since you'd be streaming in networked game assets dynamically.

The entirety of this use-case is performed pre-connection time, and all game instances would execute this bit of code inside the Preloading.cs `Start()` method:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/00_Preloading/Preloading.cs#L27-L31
```

The logic of this method invoked on `Start()` is defined below:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/00_Preloading/Preloading.cs#L33-L56
```

First, the sample waits for the dynamic Prefab asset to load from its address and into memory. After the Prefab is ready, the game instance adds it to NetworkManger's list of NetworkPrefabs, then it marks the NetworkObject as a NetworkManager's PlayerPrefab.

Lastly, the sample forces the NetworkManager to check for matching [NetworkConfig](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/ngo/1.2.0/com.unity.netcode.gameobjects/Runtime/Configuration/NetworkConfig.cs)s between a client and the server by setting ForceSamePrefabs to true. If the server detects a mismatch between the server's and client's NetworkManager's NetworkPrefabs list when a client is trying to connect, it denies the connection automatically.

### Scene 01_Connection Approval Required For Late Joining

The `01_Connection Approval Required For Late Joining` scene uses a class that walks through what a server needs to approve a client when dynamically loading network Prefabs. This is another simple example; it's just the implementation of the connection approval callback, which is an **optional** feature from Netcode for GameObjects. To enable it, enable the **Connection Approval** option on the NetworkManager in your scene. This example enables connection approval functionality to support late-joining clients, and it works best in combination with the techniques from the other example scenes. The other example scenes don't allow for reconciliation after the server loads a Prefab dynamically, except for scene `05_API Playground Showcasing All Post-Connection Uses`, where all post-connection use-cases are integrated in one scene.

To begin, this section walks you through what the client sends to the server when trying to connect. This is done inside of [ClientConnectingState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/Shared/ConnectionStates/ClientConnectingState.cs)' StartClient() method:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/Shared/ConnectionStates/ClientConnectingState.cs#L25-L32
```

Before invoking `NetworkManager.StartClient()`, the client defines the `ConnectionData` to send along with the connection request, gathered from [DynamicPrefabLoadingUtilities.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/Shared/DynamicPrefabLoadingUtilities.cs):

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/Shared/DynamicPrefabLoadingUtilities.cs#L81-L89
```

For simplicity's sake, this method generates a hash that uniquely describes the dynamic Prefabs that a client has loaded. This hash is what the server uses as validation to decide whether a client has approval for a connection.

Next, take a look at how the server handles incoming `ConnectionData`. The sample listens for the NetworkManager's ConnectionApprovalCallback and defines the behavior to invoke inside of [ConnectionApproval.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/01_Connection%20Approval). This is done on `Start()`:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/01_Connection%20Approval/ConnectionApproval.cs#L30-L52
```

Unlike the earlier use-case, `ForceSamePrefab` is to false; this allows you to add NetworkObject Prefabs to a NetworkManager's NetworkPrefabs list after establishing a connection on both the server and clients. Before walking through what this class' connection approval callback looks like, it's worth noting here that the sample forces a mismatch of NetworkPrefabs between server and clients, because as soon as the server starts, it loads a dynamic Prefab and registers it to the server's NetworkPrefabs list:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/01_Connection%20Approval/ConnectionApproval.cs#L54-L69
```

This section walks you through the connection approval defined in this class in steps. First, its worth noting that the connection approval invokes on the host. As a result, you allow the host to establish a connection:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/01_Connection%20Approval/ConnectionApproval.cs#L85-L90
```

Next, a the sample introduces a few more validation steps. First, this sample only allows four connected clients. If the server detects any more connections past that limit, it denies the requesting client a connection. Secondly, if the `ConnectionData` is above a certain size threshold, it denies it outright.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/01_Connection%20Approval/ConnectionApproval.cs#L92-L105
```

A trivial approval for an incoming connection request occurs when the server hasn't yet loaded any dynamic Prefabs. Assuming the client hasn't injected any Prefabs outside of the `DynamicPrefabLoadingUtilities` system, the client is approved for a connection:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/01_Connection%20Approval/ConnectionApproval.cs#L107-L112
```

Another case for connection approval is when the requesting client's Prefab hash is identical to that of the server:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/01_Connection%20Approval/ConnectionApproval.cs#L114-L128
```

If the requesting client has a mismatching Prefab hash, it means that the client hasn't yet loaded the appropriate dynamic Prefabs. When this occurs, the sample leverages the NetworkManager's ConnectionApprovalResponse's `Reason` string field, and populates it with a payload containing the GUIDs of the dynamic Prefabs the client should load locally before re-attempting connection:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/01_Connection%20Approval/ConnectionApproval.cs#L140-L144
```

A client attempting to connect receives a callback from Netcode that it has been disconnected inside of `ClientConnectingState`:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/Shared/ConnectionStates/ClientConnectingState.cs#L39-L68
```

If the parsed DisconnectReason string is valid, and the parsed reason is of type `DisconnectReason.ClientNeedsToPreload`, the client will be instructed to load the Prefabs by their GUID. This is done inside of [ClientPreloadingState.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/Shared/ConnectionStates/ClientPreloadingState.cs):

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/Shared/ConnectionStates/ClientPreloadingState.cs#L30-L37
```

After the client loads the necessary Prefabs, it once again transitions to the `ClientConnectingState`, and retries the connection to the server, sending along a new Prefab hash.

:::note
**Note**: This sample leveraged a state machine to handle connection management. A state machine isn't by any means necessary for connection approvals to work—it serves to compartmentalize connection logic per state, and to be a debug-friendly tool to step through connection steps.

:::

### Scene 02_Server Authoritative Load All Prefabs Asynchronously

The `02_Server Authoritative Load All Prefabs Asynchronously` scene is a simple scenario where the server notifies all clients to pre-load a collection of network Prefabs. The server won't invoke a spawn in this scenario; instead, it incrementally loads each dynamic Prefab, one at a time.

This technique might benefit a scenario where, after all clients connect, the host arrives at a point in the game where it expects that will need to load Prefabs soon. In such a case, the server instructs all clients to preemptively load those Prefabs. Later in the same game session, the server needs to perform a spawn, and can do so knowing all clients have loaded said dynamic Prefab (since it already did so preemptively). This allows for simple spawn management.

This sample is different from the `00_Preloading Dynamic Prefabs` scene, in that it occurs after clients connect and join the game. It allows for more gameplay flexibility and loading different Prefabs based on where players are at in the game, for example.

The logic that drives the behaviour for this use-case resides inside [ServerAuthoritativeLoadAllAsync.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/02_Server%20Authoritative%20Load%20All%20Async/ServerAuthoritativeLoadAllAsync.cs). Its Start() method is as follows:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/02_Server%20Authoritative%20Load%20All%20Async/ServerAuthoritativeLoadAllAsync.cs#L36-L59
```

Similarly to the last use-case, this use-case has `ConnectionApproval` set to `true` and `ForceSamePrefabs` set to `false`. The sample defines a trimmed-down `ConnectionApproval` callback inside of this class resembling that of the last use-case to allow for denying connections to clients that have mismatched NetworkPrefabs lists to that of the server. If `ConnectionApproval` is set to `false`, all incoming connection are automatically approved.

The sample also binds a UI (user interface) button's pressed callback to a method inside this class. This method, shown below, iterates through the serialized list of `AssetReferenceGameObject`s, and generates a set of tasks to asynchronously load the Prefabs on every connected client (and the server).

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/02_Server%20Authoritative%20Load%20All%20Async/ServerAuthoritativeLoadAllAsync.cs#L136-L145
```

The task to load an Addressable individually is as follows:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/02_Server%20Authoritative%20Load%20All%20Async/ServerAuthoritativeLoadAllAsync.cs#L147-L189
```

First, the sample ensures this block of code only executes on the server. Next, a simple check verifies if the dynamic Prefab has already been loaded. If the dynamic Prefab is loaded, you can early return inside this method.

Next, the server sends out a ClientRpc to every client, instructing them to load an Addressable and add it to their NetworkManager's NetworkPrefabs list. After sending out the ClientRpc, the server begins to asynchronously load the same Prefab. The ClientRpc looks like:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/02_Server%20Authoritative%20Load%20All%20Async/ServerAuthoritativeLoadAllAsync.cs#L191-L213
```

This operation should only run on clients. After the Prefab loads on the client, the client sends back an acknowledgement ServerRpc containing the hashcode of the loaded Prefab. The ServerRpc looks like:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/02_Server%20Authoritative%20Load%20All%20Async/ServerAuthoritativeLoadAllAsync.cs#L215-L242
```

The server records that the client has successfully loaded the dynamic Prefab. As hinted by the class name, this use-case only instructs clients to load a set of dynamic Prefabs and doesn't invoke a network spawn.

### Scene 03_Server Authoritative Synchronous Dynamic Prefab Spawn

The `03_Server Authoritative Synchronous Dynamic Prefab Spawn` scene is a dynamic Prefab loading scenario where the server instructs all clients to load a single network Prefab, and only invokes a spawn after all clients have finish loading the Prefab. The server initially sends an [RPC](../../advanced-topics/message-system/rpc.md) to all clients, begins loading the Prefab on the server, awaits a acknowledgement of a load via an RPC from each client, and only spawns the Prefab over the network after it receives an acknowledgement from every client, within a predetermined amount of time.

This example implementation works best for scenarios where you want to guarantee the same world version across all connected clients. Because the server waits for all clients to finish loading the same dynamic Prefab, the spawn of said dynamic Prefab will be synchronous.

The technique demonstrated in this sample works best for spawning game-changing gameplay elements, assuming you want all clients to be able to interact with said gameplay elements from the same point forward. For example, you don't want to have an enemy that's only visible (network-side or visually) to some clients and not others—you want to delay the spawning the enemy until all clients have dynamically loaded it and are able to see it before spawning it server-side.

The logic for this use-case resides inside of [ServerAuthoritativeSynchronousSpawning.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/03_Server%20Authoritative%20Synchronous%20Spawning/ServerAuthoritativeSynchronousSpawning.cs). The Start() method of this class resembles that of the last use-case, however, the method invoked by the UI (user interface) is:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/03_Server%20Authoritative%20Synchronous%20Spawning/ServerAuthoritativeSynchronousSpawning.cs#L134-L149
```

The sample first validates that only the server executes this bit of code. Next, it grabs a `AssetReferenceGameObject` from the serialized list at random, and invokes an asynchronous task that tries to spawn this dynamic Prefab, positioned inside a random point of a circle:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/03_Server%20Authoritative%20Synchronous%20Spawning/ServerAuthoritativeSynchronousSpawning.cs#L151-L235
```

The sample checks if the dynamic Prefab is already loaded and if so, it can just spawn it directly:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/03_Server%20Authoritative%20Synchronous%20Spawning/ServerAuthoritativeSynchronousSpawning.cs#L166-L171
```

Next, the sample resets the variable to track the number of clients that have loaded a Prefab during this asynchronous operation, and the variable to track how long a spawn operation has taken. The server then instructs all clients to load the dynamic Prefab via a ClientRpc:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/03_Server%20Authoritative%20Synchronous%20Spawning/ServerAuthoritativeSynchronousSpawning.cs#L176-L177
```

The server now loads the dynamic Prefab. After successfully loading the dynamic Prefab, the server records the necessary number of acknowledgement ServerRpcs it needs to receive to guarantee that all clients have loaded the dynamic Prefab. The server then halts until that condition is met:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/03_Server%20Authoritative%20Synchronous%20Spawning/ServerAuthoritativeSynchronousSpawning.cs#L189-L203
```

If all clients have loaded the dynamic Prefab, and that condition is met within a predetermined amount of seconds, the server is free to instantiate and spawn a NetworkObject over the network. If this loading condition isn't met, the server doesn't instantiate nor spawn the loaded NetworkObject, and returns a failure for this task:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/03_Server%20Authoritative%20Synchronous%20Spawning/ServerAuthoritativeSynchronousSpawning.cs#L205-L208
```

The ClientRpc in this class is identical to that of the last use-case, but the ServerRpc is different since here is where the acknowledgement variable is incremented:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/03_Server%20Authoritative%20Synchronous%20Spawning/ServerAuthoritativeSynchronousSpawning.cs#L261-L290
```

### Scene 04_Server Authoritative Spawn Dynamic Prefab Using Network Visibility

The `04_Server Authoritative Spawn Dynamic Prefab Using Network Visibility` scene is a dynamic Prefab loading scenario where the server instructs all clients to load a single network Prefab via an RPC, spawns the Prefab as soon as it's loaded on the server, and marks the Prefab as network-visible only to clients that have already loaded that same Prefab. As soon as a client loads the Prefab locally, it sends an acknowledgement RPC, and the server marks that spawned NetworkObject as network-visible for that client.

An important implementation detail to note about this technique is that the server won't wait until all clients load a dynamic Prefab before spawning the corresponding NetworkObject. As a result, a NetworkObject becomes network-visible for a connected client as soon as the client loads it—a client isn't blocked by the loading operation of another client (which might take longer to load the asset or fail to load it at all). A consequence of this asynchronous loading technique is that clients might experience differing world versions momentarily. As a result, it's not recommend to use this technique for spawning game-changing gameplay elements (like a boss fight, for example) if you want all clients to interact with the spawned NetworkObject as soon as the server spawns it.

Take a look at the implementation, observed inside [ServerAuthoritativeNetworkVisibilitySpawning](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs). As was the case with the last use-case, the `Start()` method defines the callback to subscribe to from the UI (user interface). The method invoked on the server looks like:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L134-L149
```

A dynamic Prefab reference is selected at random, and will be spawned by the following method:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L151-L225
```

Similar to the last use-case, this section of code should only run on the server:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L161-L169
```

This method here will first load the dynamic Prefab on the server, and immediately spawn it on the server:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L178-L179
```

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L185
```

After instantiating the NetworkObject, the sample keeps track of the instantiated NetworkObject in a dictionary, keyed by its asset GUID:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L187-L194
```

`NetworkObject.CheckObjectVisibility` is the callback that Netcode uses to decide whether to mark a NetworkObject as network-visible to a client. Here the sample explicitly defines a new callback. In this case, network-visibility is determined by whether the server has received an acknowledgement ServerRpc from a client for having loaded the dynamic Prefab.

It's worth noting that this callback also runs on the host, so a quick server check returns a success:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L201-L205
```

If a client has loaded the dynamic Prefab in question, mark the NetworkObject network-visible to them:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L207-L211
```

If a client hasn't loaded the dynamic Prefab, send the client a ClientRpc instructing them to load the dynamic Prefab:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L216-L218
```

After the server defines this callback, it spawns the NetworkObject:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L221
```

The ClientRpc is identical to that of the last use-case, but this use-case takes a look at the ServerRpc in this class, and makes a distinction unique to this use-case:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L272-L280
```

This marks the NetworkObject network-visible to the non-hosting clients. The specific API used here is:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/DynamicAddressablesNetworkPrefabs/Assets/Scripts/04_Server%20Authoritative%20Network-Visibility%20Spawning/ServerAuthoritativeNetworkVisibilitySpawning.cs#L227-L239
```

### Scene 05_API Playground Showcasing All Post-Connection Uses

The `05_API Playground Showcasing All Post-Connection Uses` scene uses a class that serves as the playground of the dynamic Prefab loading uses. It integrates APIs from this sample to use at post-connection time, such as:

- Connection approval for syncing late-joining clients.
- Dynamically loading a collection of network Prefabs on the host and all connected clients.
- Synchronously spawning a dynamically loaded network Prefab across connected clients.
- Spawning a dynamically loaded network Prefab as network-invisible for all clients until they load the Prefab locally (in which case it becomes network-visible to the client).
