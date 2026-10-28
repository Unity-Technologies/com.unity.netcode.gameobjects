# Hybrid objects (GhostObjects)

Understand how to use GhostObjects from Unity Netcode to support client-side prediction.

GhostObjects are a feature of [Unity Netcode](https://docs.unity3d.com/Packages/com.unity.netcode@latest) that let you build networked gameplay with GameObjects and MonoBehaviours, while Unity Netcode handles replication, prediction, and interpolation for you. By adding a GhostObject component to the root of a GameObject prefab, you can use Unity Netcode to synchronize the transform of the GameObject and its children, and optionally synchronize the state of its Rigidbody component. You can also use GhostObjects to implement client-side prediction for your networked gameplay.

A hybrid object is a GameObject that has both a [NetworkObject](components/core/networkobject.md) component and a [GhostObject](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghostobject/ghostobject.html) component. Unity Netcode synchronizes the transform of a hybrid object as part of its snapshot, which makes the object compatible with prediction in Unity Netcode. Netcode for GameObjects continues to synchronize the data in your [`NetworkBehaviour`](xref:Unity.Netcode.NetworkBehaviour)s, such as [`NetworkVariable`](networkvariables-landing.md)s and RPCs.

When the network prefabs list contains a hybrid object, Netcode for GameObjects uses Unity Netcode to drive the connection, and Unity Netcode drives the physics system. The **Parent Replication** setting on the GhostObject component controls how you set the parent GameObject of a hybrid object. Hybrid objects have some [limitations](#hybrid-object-limitations), including incompatibility with the distributed authority topology, in-scene placed objects, and object pooling.

To create a hybrid object, add a GhostObject component to a NetworkObject prefab, or a NetworkObject component to a GhostObject prefab.

## How hybrid objects synchronize data

A hybrid object synchronizes its data through two separate systems:

- Unity Netcode synchronizes the transform of the hybrid object in its snapshot. If you enable **Replicate Rigidbody** on the **GhostObject** component, Unity Netcode also synchronizes the state of the Rigidbody component. Unity enables this option by default.
- Netcode for GameObjects synchronizes the data in the hybrid object's `NetworkBehaviour`s, including `NetworkVariable`s, RPCs, and spawn data. Netcode for GameObjects sends this data separately from the Unity Netcode snapshot.

Because Netcode for GameObjects data isn't part of the snapshot, Unity Netcode doesn't roll back or replay `NetworkVariable`s, RPCs, or spawn data as part of its prediction loop. To use prediction on a hybrid object, write your prediction code in `GhostBehaviour`s. Prediction code in `GhostBehaviour`s on hybrid objects works the same way as on any other GhostObject.

When you convert a NetworkObject into a hybrid object, Netcode for GameObjects doesn't change the GhostObject's ghost mode settings. By default, a GhostObject spawns interpolated and supports all ghost modes. To use prediction, set `DefaultGhostMode` on the **GhostObject** component to a predicted mode. To reduce bandwidth, limit `SupportedGhostModes` to the modes the object uses. For more information about ghost modes, refer to the [Unity Netcode documentation](https://docs.unity3d.com/Packages/com.unity.netcode@latest).

### Effect on existing transform and physics components

When you convert a NetworkObject into a hybrid object, any existing [NetworkTransform](components/helper/networktransform.md) and [NetworkRigidbody](components/helper/networkrigidbody.md) components stay on the prefab so that you don't lose their settings. However, they no longer synchronize data:

- A NetworkTransform component on a hybrid object has no effect, because Unity Netcode always synchronizes the transform.
- A NetworkRigidbody component on a hybrid object has no effect when you enable **Replicate Rigidbody** on the **GhostObject** component.

If you convert the hybrid object back into a NetworkObject, these components resume synchronizing data with their existing settings.

## How Netcode for GameObjects uses Unity Netcode

When the network prefabs list on the [NetworkManager](components/core/networkmanager.md) contains a hybrid object, Netcode for GameObjects configures Unity Netcode and uses it to drive the connection. This changes how Netcode for GameObjects handles snapshot size and physics.

### Hybrid object requirements

Hybrid objects have the following requirements:

- A global `NetcodeConfig` asset. Unity Netcode creates this asset by default. If for some reason no global `NetcodeConfig` asset exists, the session fails to start and Netcode for GameObjects logs an error.
- The `HostWorldModeSelection` setting of the global `NetcodeConfig` asset set to `SingleWorld`. If you use any other host world mode, the session fails to start and Netcode for GameObjects logs an error.

### Snapshot size

By default, Netcode for GameObjects configures the Unity Netcode snapshot size for you. To configure the snapshot size yourself in the `NetcodeConfig` asset, disable **Auto Configure Snapshot Size** in the **Hybrid Settings** section of the **NetworkManager** component.

### Physics simulation rate

When the network prefabs list contains a hybrid object, Unity Netcode drives the physics system. The physics system runs at the rate set in **Tick Rate** in the **Network Settings** section of the **NetworkManager** component. Unity Netcode uses this value instead of the **Simulation Tick Rate** value in the `ClientServerTickRate` settings of the `NetcodeConfig` asset.

For more information about tick rates in Netcode for GameObjects, refer to [Ticks and update rates](learn/ticks-and-update-rates.md).

## Parent replication

The **Parent Replication** setting on the **GhostObject** component controls which package synchronizes the parent GameObject of a hybrid object.

Unity disables **Parent Replication** by default. When you disable it, Netcode for GameObjects synchronizes parenting through its [NetworkObject parenting](advanced-topics/networkobject-parenting.md) methods, with the following behavior:

- Setting a parent with `worldPositionStays` set to `false` works as expected.
- Setting a parent with `worldPositionStays` set to `true` has no effect, and Netcode for GameObjects logs an error.

When you enable **Parent Replication**, Unity Netcode synchronizes parenting, and you set the parent GameObject of a hybrid object in the same way as for any other GameObject. The NetworkObject parenting methods, such as [`NetworkObject.TrySetParent`](xref:Unity.Netcode.NetworkObject.TrySetParent*), have no effect, and Netcode for GameObjects logs an error if you call them.

## Hybrid object limitations

Hybrid objects have the following limitations:

- Distributed authority: Hybrid objects aren't compatible with the [distributed authority](terms-concepts/distributed-authority.md) topology. If the network prefabs list contains a hybrid object, a distributed authority session fails to start.
- In-scene placed objects: [In-scene placed NetworkObjects](basics/scenemanagement/inscene-placed-networkobjects.md) can't be hybrid objects.
- Object pooling: Hybrid objects aren't compatible with `INetworkPrefabInstanceHandler`, so you can't use [object pooling](advanced-topics/object-pooling.md) with hybrid objects.
- Client authority: Unity Netcode doesn't support the client authoritative motion model. You can convert a NetworkObject that uses an owner-authoritative NetworkTransform into a hybrid object, but the owner no longer controls its transform, because the NetworkTransform component stops synchronizing data. To move the converted object, use server-authoritative motion or prediction instead. `NetworkVariable`s on hybrid objects can still use owner write permissions.

## Create and remove hybrid objects

To create a hybrid object, add the missing component to an existing network prefab:

- To convert a NetworkObject prefab into a hybrid object, select **Add Component** in the prefab's **Inspector** window and add a **GhostObject** component.
- To convert a GhostObject prefab into a hybrid object, select **Add Component** in the prefab's **Inspector** window and add a **NetworkObject** component.

To convert a hybrid object back into a prefab that uses only one package, remove one of its components:

- To keep only Netcode for GameObjects synchronization, remove the **GhostObject** component.
- To keep only Unity Netcode synchronization, remove the **NetworkObject** component.

> [!NOTE]
> Unity might prevent you from removing a **GhostObject** or **NetworkObject** component while the GameObject has `GhostBehaviour` or `NetworkBehaviour` components that depend on it. Remove the dependent components first.

## Additional resources

- [GhostObjects in Unity Netcode](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghostobject/ghostobject.html)
- [Unity Netcode documentation](https://docs.unity3d.com/Packages/com.unity.netcode@latest)
- [NetworkObject](components/core/networkobject.md)
- [NetworkObject parenting](advanced-topics/networkobject-parenting.md)
- [Physics](advanced-topics/physics.md)
