# In-scene placed NetworkObjects

> [!NOTE]
> If you haven't already read the [Using NetworkSceneManager](using-networkscenemanager.md) section, it's highly recommended to do so before proceeding.

In-scene placed `NetworkObject`s are GameObjects with a `NetworkObject` component that was added to a scene from within the Editor. You can use in-scene placed `NetworkObject`s for:

- Management systems
  - For example, a `NetworkObject` pool management system that dynamically spawns network prefabs.
- Interactive world objects that are typically easier to place in-scene than spawn
  - For example, a door that can only be unlocked by a key. If a player unlocks it then you want other players to know about it being unlocked, and making it an in-scene placed `NetworkObject` simplifies the positioning of the door relative to the surrounding world geometry.
 - Static visual elements
   - For example, a heads up display (HUD) that includes information about other items or players.
   - Or some form of platform or teleporter that moves a player from one location to the next when a player enters a trigger or uses an object.

Another benefit of in-scene placed `NetworkObject`s is that they don't require you to register them with the [`NetworkManager`](../../components/networkmanager.md). In-scene placed `NetworkObjects` are registered internally, when scene management is enabled, for tracking and identification purposes.

> [!NOTE]
> Items that can be picked up are typically better implemented using a [hybrid approach](#hybrid-approach) with both an in-scene placed and a dynamically spawned `NetworkObject`. The in-scene placed `NetworkObject` can be used to configure additional information about the item (what kind, does another one respawn after one is picked up, and if so how much time should it wait before spawning a new item), while the dynamically spawned object is the item itself.

## In-scene placed vs. dynamically spawned `NetworkObject`s (order of operations)

Because in-scene placed `NetworkObject`s are instantiated when a scene loads, they have a different order of operations than dynamically spawned `NetworkObject`s when it comes to spawning. The table below illustrates that spawning occurs after `Awake` but before `Start` for dynamically spawned `NetworkObject`s, but for in-scene placed `NetworkObject`s it occurs after both `Awake` and `Start` are invoked.

Dynamically spawned | In-scene placed
------------------- | ---------------
`Awake`               | `Awake`
`OnNetworkSpawn`      | `Start`
`Start`               | `OnNetworkSpawn`

In-scene placed `NetworkObject`s are instantiated when the scene is loaded, which means that both the `Awake` and `Start` methods are invoked before an in-scene placed `NetworkObject` is spawned. This distinct difference is important to keep in mind when doing any form of dependency-related initializations that might require an active network session. This is especially important to consider when you're using the same `NetworkBehaviour` component with both dynamically spawned and in-scene placed `NetworkObjects`.

## In-scene placed network prefab instances

For frequently used in-scene `NetworkObjects`, you can use a [network prefab](../object-spawning.md#network-prefabs) to simplify the creation process.

### Creating in-scene placed network prefab instances

To create a network prefab that can be used as an in-scene placed `NetworkObject`, do the following:

1. Create a prefab asset (from a GameObject in a scene or creating it directly in a subfolder within Assets).
2. Add only one `NetworkObject` component to the root GameObject of the prefab.
3. Add any other `NetworkBehaviour`s (on the root or on any level of child GameObject under the root prefab GameObject).
4. If you created the prefab asset from an existing scene, then the original in-scene placed object will automatically become a network prefab instance.
5. If you created the prefab asset directly in a subfolder under the Assets directory, then drag and drop the newly created network prefab into the scene of choice.

> [!NOTE]
> You may need to deactivate **Enter Play Mode Options** if your `NetworkBehaviour` components do not spawn.

## Using in-scene placed `NetworkObject`s

There are some common use cases where in-scene placed `NetworkObject`s can prove useful.

### Static objects

You can use an in-scene placed `NetworkObject` to keep track of when a door is opened, a button pushed, a lever pulled, or any other type of state with a simple on/off toggle. These are referred to as static objects, and have the following in common:

 - Static objects are spawned while the originating scene is loaded and only despawned when the originating scene is unloaded.
    - The originating scene is the scene in which the in-scene `NetworkObject` was placed.
 - Static objects are typically associated with some world object that's visible to players (such as a door or switch).
 - Static objects aren't migrated into other scenes or parented under another `NetworkObject`.
    - For example, a drawbridge that comes down when a certain game state is reached. The drawbridge is most likely connected to some castle or perhaps a section of the castle and would never need to be migrated to another scene.

### Netcode managers

You can use an in-scene placed `NetworkObject` as a netcode manager, for tracking game states, or as a `NetworkObject` spawn manager. Typically, a manager stays instantiated and spawned as long as the scene it was placed in remains loaded.  For scenarios where you want to keep a global game state, the recommended solution is to place the in-scene `NetworkObject` in an additively loaded scene that remains loaded for the duration of your network game session.  

If you're using scene switching (that is, loading a scene in `LoadSceneMode.Single`), then you can migrate the in-scene placed `NetworkObject` (used for management purposes) into the DDoL by sending its `GameObject` to the DDoL:

```csharp
private void Start()
{
    // Don't use this for dynamically spawned NetworkObjects
    DontDestroyOnLoad(gameObject);
}
```

> [!NOTE]
> Once migrated into the DDoL, migrating the in-scene placed `NetworkObject` back into a different scene after it has already been spawned will cause soft synchronization errors with late-joining clients. Once in the DDoL it should stay in the DDoL. This is only for scene switching. If you aren't using scene switching, then it's recommended to use an additively loaded scene and keep that scene loaded for as long as you wish to persist the in-scene placed `NetworkObject`(s) being used for state management purposes.

## Complex in-scene `NetworkObject`s

The most common mistake when using an in-scene placed `NetworkObject` is to try and use it like a dynamically spawned `NetworkObject`. When trying to decide if you should use an in-scene placed or dynamically spawned `NetworkObject`, you should ask yourself the following questions:

- Do you plan on despawning and destroying the `NetworkObject`?
    - If yes, then it's highly recommended that you use a dynmically spawned `NetworkObject`.
- Can it be parented, at runtime, under another `NetworkObject`?
- Excluding any special case DDoL scenarios, will it be moved into another scene other than the originating scene?
- Do you plan on having full scene-migrations (that is, `LoadSceneMode.Single` or scene switching) during a network session?

If you answered yes to any of the above questions, then using only an in-scene placed `NetworkObject` to implement your feature might not be the right choice. However, you can use a hybrid approach to get the best of both methods.

### Hybrid approach

Because there are additional complexities involved with in-scene placed `NetworkObject`s, some use cases are more suited to dynamically spawned `NetworkObject`s, or require a combination of both types. Perhaps your project's design includes making some world items that can either be consumed (such as health) or picked up (such as weapons and items) by players. Initially, using a single in-scene placed `NetworkObject` might seem like the best approach for this world item feature.  

However, there's another way to accomplish the same thing while maintaining a clear distinction between dynamically spawned and in-scene placed `NetworkObject`s. Rather than combining everything into a single network prefab and handling the additional complexities involved with in-scene placed `NetworkObject`s, you can create two network prefabs:

1. A world item network prefab: since this will be dynamically spawned, you can re-use this network prefab with any spawn manager (pooled or single).
2. A single-spawn manager (non-pooled): this will spawn the world item network prefab. The single-spawn manager can spawn the dynamically spawned `NetworkObject` at its exact location with an option to use the same rotation and scale.

> [!NOTE]
> Your single-spawn manager can also have a list of GameObjects used as spawn points if you want to spawn world items in various locations randomly and/or based on game state.

Using this approach allows you to:
1. Re-use the same single-spawn manager with any other network prefab registered with a `NetworkPrefabsList`.
2. Not worry about the complexities involved with treating an in-scene placed `NetworkObject` like a dynamically spawned one.

[You can see a hybrid approach example here.](../object-spawning.md#dynamic-spawning-non-pooled)

## Spawning and despawning in-scene placed `NetworkObject`s

By default, an in-scene placed `NetworkObject` is spawned when the scene it's placed in is loaded and a network session is in progress. In-scene placed `NetworkObject`s differ from dynamically spawned `NetworkObject`s when it comes to spawning and despawning: when despawning a dynamically spawned `NetworkObject`, you can always spawn a new instance of the `NetworkObject`'s associated network prefab. So, whether you decide to destroy a dynamically spawned `NetworkObject` or not, you can always make another clone of the same network prefab, unless you want to preserve the current state of the instance being despawned.

With in-scene placed `NetworkObject`s, the scene it's placed in is similar to the network prefab used to dynamically spawn a `NetworkObject`, in that both are used to uniquely identify the spawned `NetworkObject`. The primary difference is that where you use a network prefab to create a new dynamically spawned instance, for in-scene placed objects you need to additively load the same scene to create another in-scene placed `NetworkObject` instance.

### Identifying spawned `NetworkObject`s

Dynamically spawned | In-scene placed
------------------- | ---------------
`NetworkPrefab`       | Scene
`GlobalObjectIdHash`  | Scene handle (when loaded) and `GlobalObjectIdHash`

Once the `NetworkObject` is spawned, Netcode for GameObjects uses the `NetworkObjectId` to uniquely identify it for both types. An in-scene placed `NetworkObject` will continue to be uniquely identified by the scene handle that it originated from and the `GlobalObjectIdHash`, even if the in-scene placed `NetworkObject` is migrated to another additively loaded scene and the originating scene is unloaded.

### Despawning and respawning the same in-scene placed `NetworkObject`

When invoking the `Despawn` method of a `NetworkObject` with no parameters, it defaults to destroying the `NetworkObject`:

```csharp
NetworkObject.Despawn();  // Will despawn and destroy (!!! not recommended !!!)
```

If you want an in-scene placed `NetworkObject` to persist after it's been despawned, it's recommended to always set the first parameter of the `Despawn` method to `false`:

```csharp
NetworkObject.Despawn(false); // Will only despawn (recommended usage for in-scene placed NetworkObjects)
```

Now that you have a despawned `NetworkObject`, you might notice that the associated `GameObject` and all of its components are still active and possibly visible to all players (that is, like a `MeshRenderer` component). Unless you have a specific reason to keep the associated `GameObject` active in the hierarchy, you can override the virtual `OnNetworkDespawn` method in a `NetworkBehaviour`-derived component and set the `GameObject` to inactive:

```csharp
using UnityEngine;
using Unity.Netcode;

public class MyInSceneNetworkObjectBehaviour : NetworkBehaviour
{
    public override void OnNetworkDespawn()
    {
        gameObject.SetActive(false);
        base.OnNetworkDespawn();
    }
}
```

This ensures that when your in-scene placed `NetworkObject` is despawned, it won't consume processing or rendering cycles and will become invisible to all players (either currently connected or that join the session later). Once the `NetworkObject` has been despawned and disabled, you might want to respawn it at some later time. To do this, set the server-side instance's `GameObject` back to being active and spawn it:

```csharp
using UnityEngine;
using Unity.Netcode;

public class MyInSceneNetworkObjectBehaviour : NetworkBehaviour
{
    public override void OnNetworkDespawn()
    {
        gameObject.SetActive(false);
        base.OnNetworkDespawn();
    }

    public void Spawn(bool destroyWithScene)
    {
        if (IsServer && !IsSpawned)
        {
            gameObject.SetActive(true);
            NetworkObject.Spawn(destroyWithScene);
        }
    }
}
```

> [!NOTE]
> You only need to enable the `NetworkObject` on the server-side to be able to respawn it. Netcode for GameObjects only enables a disabled in-scene placed `NetworkObject` on the client-side if the server-side spawns it. This **does not** apply to dynamically spawned `NetworkObjects`. Refer to [the object pooling page](../../advanced-topics/object-pooling.md) for an example of recycling dynamically spawned `NetworkObject`s.

### Setting an in-scene placed `NetworkObject` to a despawned state when instantiating

Since in-scene placed `NetworkObject`s are automatically spawned when their respective scene has finished loading during a network session, you might run into the scenario where you want it to start in a despawned state until a certain condition has been met. To do this, you need to add some additional code in the `OnNetworkSpawn` part of your `NetworkBehaviour` component:

```csharp
using UnityEngine;
using Unity.Netcode;

    public class MyInSceneNetworkObjectBehaviour : NetworkBehaviour
    {
        public bool StartDespawned;

        private bool m_HasStartedDespawned;
        public override void OnNetworkSpawn()
        {
            if (IsServer && StartDespawned && !m_HasStartedDespawned)
            {
                m_HasStartedDespawned = true;
                NetworkObject.Despawn(false);
            }
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            gameObject.SetActive(false);
            base.OnNetworkDespawn();
        }

        public void Spawn(bool destroyWithScene)
        {
            if (IsServer && !IsSpawned)
            {
                gameObject.SetActive(true);
                NetworkObject.Spawn(destroyWithScene);
            }
        }
    }
```

The above example keeps track of whether the in-scene placed `NetworkObject` has started in a despawned state (to avoid despawning after its first time being spawned), and it only allows the server to execute that block of code in the overridden `OnNetworkSpawn` method. The above `MyInSceneNetworkObjectBehaviour` example also declares a public `bool` property `StartDespawned` to provide control over this through the inspector view in the Editor.

### Synchronizing late-joining clients when an in-scene placed `NetworkObject` has been despawned and destroyed

Referring back to the [section on complex in-scene `NetworkObject`s](#complex-in-scene-networkobjects), it's recommended to use dynamically spawned `NetworkObject`s if you intend to destroy the object when it's despawned. However, if either despawning but not destroying or using the [hybrid approach](#a-hybrid-approach-example) don't appear to be options for your project's needs, then there are two other possible (but not recommended) alternatives:

- Have another in-scene placed `NetworkObject` track which in-scene placed `NetworkObject`s have been destroyed and upon a player late-joining (that is, `OnClientConnected`) you would need to send the newly-joined client the list of in-scene placed NetworkObjects that it should destroy. This adds an additional in-scene placed `NetworkObject` to your scene hierarchy and will consume memory keeping track of what was destroyed.
- Disable the visual and physics-related components (in Editor as a default) of the in-scene placed `NetworkObject`(s) in question and only enable them in `OnNetworkSpawn`. This doesn't delete/remove the in-scene placed `NetworkObject`(s) for the late-joining client and can be tricky to implement without running into edge case scenario bugs.

These two alternatives aren't recommended, but are worth briefly exploring to better understand why it's recommend to use a [non-pooled hybrid approach](../object-spawning.md#dynamic-spawning-non-pooled), or just not destroying the in-scene placed `NetworkObject` when despawning it.

## Parenting in-scene placed `NetworkObject`s

In-scene placed `NetworkObject`s follow the same parenting rules as [dynamically spawned `NetworkObject`s](../../advanced-topics/networkobject-parenting.md) with only a few differences and recommendations:

- You can create complex nested `NetworkObject` hierarchies when they're in-scene placed.
- If you plan on using full scene-migration (that is, `LoadSceneMode.Single` or scene switching) then parenting an in-scene placed `NetworkObject` that stays parented during the scene migration isn't recommended.
  - In this scenario, you should use a hybrid approach where the in-scene placed `NetworkObject` dynamically spawns the item to be picked up.
- If you plan on using a bootstrap scene usage pattern with additive scene loading and unloading with no full scene-migration(s), then you can parent in-scene placed `NetworkObject`s.

### Auto object parent sync option

 Already parented in-scene placed `NetworkObject`s auto object parent sync usage:

- When disabled, the `NetworkObject` ignores its parent and considers all of its transform values as being world space synchronized (that is, no matter where you move or rotate its parent, it will keep its current position and rotation).
  - Typically, when disabling this you need to handle synchronizing the client either through your own custom messages or RPCS, or add a `NetworkTransform` component to it. This is only useful if you want to have some global parent that might shift or have transform values that you don't want to impact the `NetworkObject` in question.
- When enabled, the `NetworkObject` is aware of its parent and will treat all of its transform values as being local space synchronized.  
  - This also applies to being pre-parented under a `GameObject` with no `NetworkObject` component.

_**The caveat to the above is scale**:_
Scale is treated always as local space relative for pre-parented in-scene placed `NetworkObjects`. <br />

*For dynamically spawned NetworkObjects:* <br />
It depends upon what WorldPositionStays value you use when parenting the NetworkObject in question.<br />
WorldPositionStays = true: Everything is world space relative. _(default)_<br />
WorldPositionStays = false: Everything is local space relative. _(children offset relative to the parent)_<br />

### Parenting and transform synchronization

Without the use of a `NetworkTransform`, clients are only synchronized with the transform values when:

- A client is being synchronized with the `NetworkObject` in question:
  - During the client's first synchronization after a client has their connection approved.
  - When a server spawns a new `NetworkObject`.
- A `NetworkObject` has been parented (or a parent removed).
- The server can override the `NetworkBehaviour.OnNetworkObjectParentChanged` method and adjust the transform values when that is invoked.
   - These transform changes will be synchronized with clients via the `ParentSyncMessage`.
