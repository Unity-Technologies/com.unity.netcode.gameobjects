# NetworkObject

A NetworkObject is a [GameObject](https://docs.unity3d.com/Manual/GameObjects.html) with a NetworkObject component and at least one [NetworkBehaviour](networkbehaviour.md) component, which enables the GameObject to respond to and interact with netcode. NetworkObjects are session-mode agnostic and used in both [client-server](../../terms-concepts/client-server.md) and [distributed authority](../../terms-concepts/distributed-authority.md) contexts.

Netcode for GameObjects' high level components, [the RPC system](../../advanced-topics/messaging-system.md), [object spawning](../../basics/object-spawning.md), and [`NetworkVariable`](../../basics/networkvariable.md)s all rely on there being at least two Netcode components added to a GameObject:

  1. NetworkObject
  2. [NetworkBehaviour](networkbehaviour.md)

NetworkObjects require the use of specialized [`NetworkObjectReference`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkObjectReference.html) structures before you can serialize and use them with RPCs and `NetworkVariable`s

Netcode for GameObjects also has [PlayerObjects](playerobjects.md), an optional feature that you can use to assign a NetworkObject to a specific client.

## Using NetworkObjects

To replicate any Netcode-aware properties or send/receive RPCs, a GameObject must have a NetworkObject component and at least one NetworkBehaviour component. Any Netcode-related component, such as a NetworkTransform or a NetworkBehaviour with one or more NetworkVariables or RPCs, requires a NetworkObject component on the same relative GameObject (or on a parent of the GameObject in question).

When spawning a NetworkObject, the `NetworkObject.GlobalObjectIdHash` value initially identifies the associated network prefab asset that clients instantiate to create a client-local clone. After instantiated locally, each NetworkObject is assigned a NetworkObjectId that's used to associate NetworkObjects across the network. For example, one peer can say "Send this RPC to the object with the NetworkObjectId 103," and everyone knows what object it's referring to. A NetworkObject is spawned on a client when it's instantiated and assigned a unique NetworkObjectId.

You can use [NetworkBehaviours](networkbehaviour.md) to add your own custom Netcode logic to the associated NetworkObject.

### Component order

The order of components on a networked GameObject matters. When adding netcode components to a GameObject, ensure that the NetworkObject component is ordered before any NetworkBehaviour components.

The order in which NetworkBehaviour components are presented in the **Inspector** view is the order in which each associated `NetworkBehaviour.OnNetworkSpawn` method is invoked. Any properties that are set during `NetworkBehaviour.OnNetworkSpawn` are set in the order that each NetworkBehaviour's `OnNetworkSpawned` method is invoked.

#### Avoiding execution order issues

You can avoid execution order issues in any NetworkBehaviour component scripts that have dependencies on other NetworkBehaviour components associated with the same NetworkObject by placing those scripts in an overridden `NetworkBehaviour.OnNetworkPostSpawn` method. The `NetworkBehaviour.OnNetworkPostSpawn` method is invoked on each NetworkBehaviour component after all NetworkBehaviour components associated with the same NetworkObject component have had their `NetworkBehaviour.OnNetworkSpawn` methods invoked (but they will still be invoked in the same execution order defined by their relative position to the NetworkObject component when viewed within the Unity Editor **Inspector** view).

## Ownership

Either the server (default) or any connected and approved client owns each NetworkObject. By default, Netcode for GameObjects is [server-authoritative](../../terms-concepts/client-server.md), which means only the server can spawn and despawn NetworkObjects, but you can also build [distributed authority](../../terms-concepts/distributed-authority.md) games where clients have the authority to spawn and despawn NetworkObjects as well.

If you're creating a client-server game and you want a client to control more than one NetworkObject, use the following ownership methods.

The default `NetworkObject.Spawn` method assumes server-side ownership:

```csharp
GetComponent<NetworkObject>().Spawn();
```

To spawn NetworkObjects with ownership use the following:

```csharp
GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
```

To change ownership, use the `ChangeOwnership` method:

```csharp
GetComponent<NetworkObject>().ChangeOwnership(clientId);
```

To give ownership back to the server use the `RemoveOwnership` method:

```csharp
GetComponent<NetworkObject>().RemoveOwnership();
```

To see if the local client is the owner of a NetworkObject, you can check the [`NetworkBehaviour.IsOwner`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.IsOwner.html) property.

To see if the server owns the NetworkObject, you can check the [`NetworkBehaviour.IsOwnedByServer`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkBehaviour.IsOwnedByServer.html) property.

> [!NOTE]
> When you want to despawn and destroy the owner but you don't want to destroy a specific NetworkObject along with the owner, you can set the `NetworkObject.DontDestroyWithOwner` property to `true` which ensures that the owned NetworkObject isn't destroyed with the owner.

## Network prefabs

Network prefabs are registered to a `NetworkPrefabsList` object (a type of `ScriptableObject`). By default, a default prefabs list containing every network prefab in your project.

However, when you want to limit which prefabs are available (for example, to reduce memory usage), you can disable this behavior in **Project Settings** > **Netcode For GameObjects** > **Project Settings**. You can also manually create a `NetworkPrefabsList` by right-clicking in the assets view and selecting **Create** > **Netcode** > **Network Prefabs List** and adding your prefabs to it. That prefab list can then be assigned to a NetworkManager to allow that NetworkManager to create those prefabs.

> [!NOTE]
> You can only have one NetworkObject at the root of a prefab. Don't create prefabs with nested `NetworkObjects`.

## Spawning with (or without) observers

![image](../../images/SpawnWithObservers.png)

The `NetworkObject.SpawnWithObservers` property (default is true) enables you to spawn a NetworkObject with no initial observers. This is the recommended alternative to using `NetworkObject.CheckObjectVisibility` when you just want it to be applied globally to all clients (only when spawning an instance of the NetworkObject in question). If you want more precise per-client control then `NetworkObject.CheckObjectVisibility` is recommended. `NetworkObject.SpawnWithObservers` is only applied upon the initial server-side spawning and once spawned it has no impact on object visibility.

## Transform synchronization

![image](../../images/NetworkObject-TransformSynchronization.png)

There are times when you want to use a NetworkObject for something that doesn't require the synchronization of its transform. You might have an [in-scene placed NetworkObject](../../basics/scenemanagement/inscene-placed-networkobjects.md) that's only used to manage game state and it doesn't make sense to incur the initial client synchronization cost of synchronizing its transform. To prevent a NetworkObject from initially synchronizing its transform when spawned, deselect the **Synchronize Transform** property. This property is enabled by default.

> [!NOTE]
> If you're planning to use a NetworkTransform, then you always want to make sure the **Synchronize Transform** property is enabled.

## Active scene synchronization

![image](../../images/ActiveSceneMigration.png)

When a GameObject is instantiated, it gets instantiated in the current active scene. However, sometimes you might find that you want to change the currently active scene and would like specific NetworkObject instances to automatically migrate to the newly assigned active scene. While you could keep a list or table of the NetworkObject instances and write the code/logic to migrate them into a newly assigned active scene, this can be time consuming and become complicated depending on project size and complexity. The alternate and recommended way to handle this is by enabling the **Active Scene Synchronization** property of each NetworkObject you want to automatically migrate into any newly assigned scene. This property defaults to disabled.

Refer to the [NetworkSceneManager active scene synchronization](../../basics/scenemanagement/using-networkscenemanager.md#active-scene-synchronization) page for more details.

## Scene migration synchronization

![image](../../images/SceneMigrationSynchronization.png)

Similar to [`NetworkObject.ActiveSceneSynchronization`](#active-scene-synchronization), [`NetworkObject.SceneMigrationSynchronization`](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest?subfolder=/api/Unity.Netcode.NetworkObject.html#Unity_Netcode_NetworkObject_SceneMigrationSynchronization) automatically synchronizes client-side NetworkObject instances that are migrated to a scene via [`SceneManager.MoveGameObjectToScene`](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.MoveGameObjectToScene.html) on the host or server side. This can be useful if you have a specific scene you wish to migrate NetworkObject instances to that is not the currently active scene.

`NetworkObject.ActiveSceneSynchronization` can be used with `NetworkObject.SceneMigrationSynchronization` as long as you take into consideration that if you migrate a NetworkObject into a non-active scene via `SceneManager.MoveGameObjectToScene` and later change the active scene, then the NetworkObject instance will be automatically migrated to the newly set active scene.

:::warning
Scene migration synchronization is enabled by default. For NetworkObjects that don't require it, such as those that generate static environmental objects like trees, it's recommended to disable scene migration synchronization to avoid additional processing overheads.
:::

## Additional resources

- [NetworkBehaviour](networkbehaviour.md)
- [NetworkVariable](../../basics/networkvariable.md)
