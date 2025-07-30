# NetworkBehaviour spawning and despawning

[`NetworkBehaviour`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.html) is an abstract class that derives from [`MonoBehaviour`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) and is primarily used to create unique netcode or game logic. To replicate any netcode-aware properties or send and receive RPCs, a [GameObject](https://docs.unity3d.com/Manual/GameObjects.html) must have a [NetworkObject](networkobject.md) component and at least one `NetworkBehaviour` component.

A `NetworkBehaviour` requires a `NetworkObject` component on the same relative GameObject or on a parent of the GameObject with the `NetworkBehaviour` component assigned to it. If you add a `NetworkBehaviour` to a GameObject that doesn't have a `NetworkObject` (or any parent), then Netcode for GameObjects automatically adds a `NetworkObject` component to the `GameObject` in which the `NetworkBehaviour` was added.

`NetworkBehaviour`s can use `NetworkVariable`s and RPCs to synchronize states and send messages over the network. When you call an RPC function, the function isn't called locally. Instead, a message is sent containing your parameters, the `networkId` of the NetworkObject associated with the same GameObject (or child) that the `NetworkBehaviour` is assigned to, and the index of the NetworkObject-relative `NetworkBehaviour` (NetworkObjects can have several `NetworkBehaviours`, the index communicates which one).

For more information about serializing and synchronizing `NetworkBehaviour`s, refer to the [NetworkBehaviour synchronization page](networkbehaviour-synchronize.md).

> [!NOTE]
> It's important that the `NetworkBehaviour`s on each NetworkObject remain the same for the server and any client connected. When using multiple projects, this becomes especially important so the server doesn't try to call a client RPC on a `NetworkBehaviour` that might not exist on a specific client type (or set a `NetworkVariable` that doesn't exist, and so on).

## Spawning

`OnNetworkSpawn` is invoked on each `NetworkBehaviour` associated with a NetworkObject when it's spawned. This is where all netcode-related initialization should occur.

You can still use `Awake` and `Start` to do things like finding components and assigning them to local properties, but if `NetworkBehaviour.IsSpawned` is false then don't expect netcode-distinguishing properties (like `IsClient`, `IsServer`, `IsHost`, for example) to be accurate within `Awake` and `Start` methods.

For reference purposes, below is a table of when `NetworkBehaviour.OnNetworkSpawn` is invoked relative to the `NetworkObject` type:

Dynamically spawned | In-scene placed
------------------- | ---------------
`Awake`               | `Awake`
`OnNetworkSpawn`      | `Start`
`Start`               | `OnNetworkSpawn`

For more information about `NetworkBehaviour` methods and when they are invoked, see the [Pre-Spawn and MonoBehaviour Methods](networkbehaviour.md#pre-spawn-and-monobehaviour-methods) section.

### Disabling `NetworkBehaviour`s when spawning

If you want to disable a specific `NetworkBehaviour` but still want it to be included in the `NetworkObject` spawn process (so you can still enable it at a later time), you can disable the individual `NetworkBehaviour` instead of the entire `GameObject`.

`NetworkBehaviour` components that are disabled by default and are attached to in-scene placed NetworkObjects behave like `NetworkBehaviour` components that are attached to dynamically spawned NetworkObjects when it comes to the order of operations for the `NetworkBehaviour.Start` and `NetworkBehaviour.OnNetworkSpawn` methods. Since in-scene placed NetworkObjects are spawned when the scene is loaded, a `NetworkBehaviour` component (that is disabled by default) will have its `NetworkBehaviour.OnNetworkSpawn` method invoked before the `NetworkBehaviour.Start` method, since `NetworkBehaviour.Start` is invoked when a disabled `NetworkBehaviour` component is enabled.

Dynamically spawned | In-scene placed (disabled `NetworkBehaviour` components)
------------------- | ---------------
`Awake`               | `Awake`
`OnNetworkSpawn`      | `OnNetworkSpawn`
`Start`               | `Start` (invoked when disabled `NetworkBehaviour` components are enabled)

> [!NOTE] Parenting, inactive GameObjects, and `NetworkBehaviour` components
> If you have child GameObjects that are not active in the hierarchy but are nested under an active GameObject with an attached NetworkObject component, then the inactive child GameObjects will not be included when the NetworkObject is spawned. This applies for the duration of the NetworkObject's spawned lifetime. If you want all child `NetworkBehaviour` components to be included in the spawn process, then make sure their respective GameObjects are active in the hierarchy before spawning the `NetworkObject`. Alternatively, you can just disable the NetworkBehaviour component(s) individually while leaving their associated GameObject active.
> It's recommended to disable a `NetworkBehaviour` component rather than the GameObject itself.


### Dynamically spawned NetworkObjects

For dynamically spawned NetworkObjects (instantiating a network prefab during runtime) the `OnNetworkSpawn` method is invoked before the `Start` method is invoked. This means that finding and assigning components to a local property within the `Start` method exclusively will result in that property not being set in a `NetworkBehaviour` component's `OnNetworkSpawn` method when the NetworkObject is dynamically spawned. To circumvent this issue, you can have a common method that initializes the components and is invoked both during the `Start` method and the `OnNetworkSpawned` method like the code example below:

```csharp
public class MyNetworkBehaviour : NetworkBehaviour
{
    private MeshRenderer m_MeshRenderer;
    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (m_MeshRenderer == null)
        {
            m_MeshRenderer = FindObjectOfType<MeshRenderer>();
        }
    }

    public override void OnNetworkSpawn()
    {
        Initialize();
        // Do things with m_MeshRenderer

        base.OnNetworkSpawn();
    }
}
```

### In-scene placed NetworkObjects

For in-scene placed NetworkObjects, the `OnNetworkSpawn` method is invoked after the `Start` method, because the SceneManager scene loading process controls when NetworkObjects are instantiated. The previous code example shows how you can design a `NetworkBehaviour` that ensures both in-scene placed and dynamically spawned NetworkObjects will have assigned the required properties before attempting to access them. Of course, you can always make the decision to have in-scene placed `NetworkObjects` contain unique components to that of dynamically spawned `NetworkObjects`. It all depends upon what usage pattern works best for your project.

## Despawning

`NetworkBehaviour.OnNetworkDespawn` is invoked on each `NetworkBehaviour` associated with a `NetworkObject` when it's despawned. This is where all netcode cleanup code should occur, but isn't to be confused with destroying ([see below](#destroying)). When a `NetworkBehaviour` component is destroyed, it means the associated GameObject, and all attached components, are in the middle of being destroyed. Regarding the order of operations, `NetworkBehaviour.OnNetworkDespawn` is always invoked before `NetworkBehaviour.OnDestroy`.

### Destroying

Each `NetworkBehaviour` has a virtual `OnDestroy` method that can be overridden to handle clean up that needs to occur when you know the `NetworkBehaviour` is being destroyed.

If you override the virtual `OnDestroy` method it's important to always invoke the base like such:

```csharp
        public override void OnDestroy()
        {
            // Clean up your NetworkBehaviour

            // Always invoked the base
            base.OnDestroy();
        }
```

`NetworkBehaviour` handles other destroy clean up tasks and requires that you invoke the base `OnDestroy` method to operate properly.

## Pre-spawn and `MonoBehaviour` methods

Since `NetworkBehaviour` is derived from `MonoBehaviour`, the `NetworkBehaviour.OnNetworkSpawn` method is treated similar to the `Awake`, `Start`, `FixedUpdate`, `Update`, and `LateUpdate` `MonoBehaviour` methods. Different methods are invoked depending on whether the GameObject is active in the hierarchy.

- When active: `Awake`, `Start`, `FixedUpdate`, `Update`, and `LateUpdate` are invoked.
- When not active: `Awake`, `Start`, `FixedUpdate`, `Update`, and `LateUpdate` are not invoked.

For more information about execution order, refer to [Order of execution for event functions](https://docs.unity3d.com/Manual/ExecutionOrder.html) in the main Unity Manual.

The unique behavior of `OnNetworkSpawn`, compared to the previously listed methods, is that it's not invoked until the associated GameObject is active in the hierarchy and its associated NetworkObject is spawned.

Additionally, the `FixedUpdate`, `Update`, and `LateUpdate` methods, if defined and the GameObject is active in the hierarchy, will still be invoked on `NetworkBehaviour`s even when they're not yet spawned. If you want portions or all of your update methods to only execute when the associated `NetworkObject` component is spawned, you can use the `NetworkBehaviour.IsSpawned` flag to determine the spawned status like the below example:

```csharp
private void Update()
{
    // If the NetworkObject is not yet spawned, exit early.
    if (!IsSpawned)
    {
        return;
    }
    // Netcode specific logic executed when spawned.
}
```
