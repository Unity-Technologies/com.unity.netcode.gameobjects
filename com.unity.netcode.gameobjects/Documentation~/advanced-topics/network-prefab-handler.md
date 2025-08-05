# Network prefab handler

The network prefab handler system provides advanced control over how network prefabs are instantiated and destroyed during runtime. This allows overriding the default Netcode for GameObjects [object spawning](../basics/object-spawning.md) behavior by implementing custom prefab handlers.

The network prefab handler system is accessible from the [NetworkManager](../components/networkmanager.md) as `NetworkManager.PrefabHandler`.

## Overview

For an overview of the default object spawning behavior, see the [object spawning](../basics/object-spawning.md) page. The default spawning behavior should cover the majority of spawning use cases, however there are scenarios where you may need more control:

- **Object pooling**: Reusing objects to reduce memory allocation and initialization costs.
- **Performance optimization**: Using different prefab variants on different platforms (e.g. using a simpler object for server simulation).
- **Custom initialization**: Setting up objects with game client specific data or configurations.
- **Conditional spawning**: Initializing different prefab variants based on runtime conditions.

The prefab handler system addresses these needs through a interface-based architecture. The system relies on two key methods, `Instantiate` and `Destroy`. `Instantiate` is called on non-authority clients when an [authority](../terms-concepts/authority.md) spawns a new [NetworkObject](../basics/networkobject.md) that has a registered network prefab handler. `Destroy` is called on all game clients whenever a registered [NetworkObject](../basics/networkobject.md) is destroyed.

## Creating a prefab handler

Prefab handlers are classes which implement on of the Netcode for GameObjects prefab handler descriptions. There are currently two such descriptions:

- **INetworkPrefabInstanceHandler**: This is the simplest interface for custom prefab handlers.
- **NetworkPrefabInstanceHandlerWithData**: This specialized handler receives custom data from the authority during spawning, enabling dynamic prefab customization.

Netcode will use the `Instantiate` and `Destroy` methods in place of default spawn handlers for the `NetworkObject` used during spawning and de-spawning. The authority instance will use the traditional spawning approach where it will, via user script, instantiate and spawn a network prefab (even for those registered with a prefab handler). However, all non-authority clients will automatically use the instantiate method defined by the `INetworkPrefabInstanceHandler` implementation if the network prefab spawned has a registered `INetworkPrefabInstanceHandler` implementation with the `NetworkPrefabHandler` (`NetworkManager.PrefabHandler`).

### INetworkPrefabInstanceHandler

When all you want to do is handle overriding a network prefab, implementing the `INetworkPrefabInstanceHandler` interface and registering an instance of that implementation with the `NetworkPrefabHandler` (`NetworkManager.PrefabHandler`) is all you would need to do. Use the `INetworkPrefabInstanceHandler` for situations where the prefab override behavior is consistent and known.

```csharp
    public interface INetworkPrefabInstanceHandler
    {
        NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation);
        void Destroy(NetworkObject networkObject);
    }
```

### NetworkPrefabInstanceHandlerWithData

If you are looking to provide serialized data to be used during the instantiation process, then the `NetworkPrefabInstanceHandlerWithData` class is what you would want to derive from and register with the `NetworkPrefabHandler` (`NetworkManager.PrefabHandler`). The `NetworkPrefabInstanceHandlerWithData` class allows for sending custom data from the authority during object spawning. This extra data can then be used to change the behavior of the `Instantiate` method. An implementation of `NetworkPrefabInstanceHandlerWithData` allows for sending any custom type that is serializable using [INetworkSerializable](advanced-topics/serialization/inetworkserializable.md).

```csharp
public abstract class NetworkPrefabInstanceHandlerWithData<T> : INetworkPrefabInstanceHandlerWithData
    where T : struct, INetworkSerializable
{
    public abstract NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, T instantiationData);
    public abstract void Destroy(NetworkObject networkObject);
}
```

## Prefab handler registration

Once you have created your prefab handler (_derived class or interface implementation_), you will need to register any new instance of your prefab handler with the network prefab handler system using `NetworkManager.PrefabHandler.AddHandler`. Prefab handlers are registered against a NetworkObject's [GlobalObjectIdHash](../basics/networkobject.md#using-networkobjects).

```csharp
public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject prefabToSpawn;

    void Start()
    {
        var customHandler = new MyPrefabHandler();
        NetworkManager.PrefabHandler.AddHandler(prefabToSpawn, customHandler);
    }
}
```

In order to un-register a prefab handler, you can [invoke the `NetworkManager.PrefabHandler.RemoveHandler` method](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.4/api/Unity.Netcode.NetworkPrefabHandler.html#Unity_Netcode_NetworkPrefabHandler_RemoveHandler_System_UInt32_) (_There are several override versions of this method_).

## Object spawning with prefab handlers

Once a prefab handler is registered Netcode will automatically use the defined `Initialize` and `Destroy` methods to manage the object lifecycle. [Spawn the network prefab as usual](../basics/object-spawning.md#spawning-a-network-prefab-overview) and the `Initialize` method will be called on whichever handler is registered with the spawned network prefab.

Note that the `Initialize` method is only called on non-authority clients. To customize network prefab behavior on the authority, you can use [prefab overrides](../basics/object-spawning.md#taking-prefab-overrides-into-consideration).

### Object spawning with custom data

When using a handler derived from `NetworkPrefabInstanceHandlerWithData`, you must manually set the instantiation data after instantiating the instance but before spawning. To do this, invoke the `NetworkPrefabInstanceHandlerWithData.SetInstantiationData` method before invoking the `NetworkObject.Spawn` method. If `SetInstantiationData` is not called, the `default` implementation will be sent to the `Instantiate` call.

#### Simple Example 

Below we can find a simple "pseudo" example script where the `InstantiateData` structure implements the `INetworkSerializable` interface and it will be used to serialize the instantiation data for the network prefab defined within the below `SpawnPrefabWithColor` NetworkBehaviour.

```csharp
/// <summary>
/// The instantiation data that is serialized and sent with the
/// spawn object message and provided prior to instantiating.
/// </summary>
public struct InstantiateData : INetworkSerializable
{
    // For example purposes, the color of the material of a MeshRenderer
    public Color Color;

    // Add additional pre-spawn configuration fields here:

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Color);

        // Add addition (field) value serialization here for each new field:
    }
}
```

The below `SpawnPrefabWithColor` is an example of a NetworkBehavior component, to be placed on an in-scene placed NetworkObject, that handles the instantiation of a prefab handler (`SpawnWithColorHandler`) and the means to configure the network prefab to register with the handler. It also has a `SpawnWithColorSystem.SpawnObject` method that can be used to instantiate an instance of the assigned network prefab instance that will also have instantiation data associated with it that contains the color to be applied to the instance's `MeshRenderer`s. 

_While there are much easier ways to synchronize the color of MeshRenderer instances across clients, this is only for example purposes._

```csharp
/// <summary>
/// Add to an in-scene placed <see cref="NetworkObject"/>.
/// </summary>
public class SpawnPrefabWithColor : NetworkBehaviour
{
    /// <summary>
    /// The network prefab used to register the <see cref="SpawnWithDataSystem"/> handler.
    /// </summary>
    public GameObject NetworkPrefab;

    /// <summary>
    /// The <see cref="SpawnWithDataSystem"/> handler instance.
    /// </summary>
    private SpawnWithColorHandler m_SpawnWithColorHandler;

    protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
    {
        m_SpawnWithColorHandler = new SpawnWithColorHandler(networkManager, NetworkPrefab);
        base.OnNetworkPreSpawn(ref networkManager);
    }

    /// <summary>
    /// Invoked by some other component or additional script logic controls
    /// when an object is spawned.
    /// </summary>
    /// <param name="color">The color to apply (pseudo example purposes)</param>
    /// <returns>The spawned <see cref="NetworkObject"/> instance</returns>
    public NetworkObject SpawnObject(Vector3 position, Quaternion rotation, Color color)
    {
        if (!IsSpawned || !HasAuthority || m_SpawnWithColorHandler == null)
        {
            return null;
        }
        // Instantiate, set the instantiation data, and then spawn the network prefab.
        return m_SpawnWithColorHandler.InstantiateSetDataAndSpawn(position, rotation, new InstantiateData() { Color = color });
    }
}
```
Above, we can see the `SpawnWithColorSystem` invokes the `SpawnWithColorHandler.InstantiateSetDataAndSpawn` method to create a new network prefab instance, set the instantiation data, and then spawn the network prefab instance.

Below we will find the slightly more complex, of the three scripts, `SpawnWithColorHandler` with a constructor that automatically registers itself and the network prefab with the `NetworkManager.PrefabHandler`:

```csharp
/// <summary>
/// The prefan instance handler that uses instantiation data to handle updating
/// the instance's <see cref="MeshRenderer"/>s material's color. (example purposes only)
/// </summary>
public class SpawnWithColorHandler : NetworkPrefabInstanceHandlerWithData<InstantiateData>
{
    private GameObject m_RegisteredPrefabToSpawn;
    private NetworkManager m_NetworkManager;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="registeredPrefab">The prefab used to register this handler instance.</param>
    /// <param name="networkPrefabGroup">The prefab group this handler will be able to spawn.</param>
    public SpawnWithColorSystem(NetworkManager networkManager, GameObject registeredPrefab)
    {
        m_NetworkManager = networkManager;
        m_RegisteredPrefabToSpawn = registeredPrefab;

        // Register this handler with the NetworkPrefabHandler
        m_NetworkManager.PrefabHandler.AddHandler(m_RegisteredPrefabToSpawn, this);
    }

    /// <summary>
    /// Used by the server or a client when using a distributed authority network topology,
    /// instantiate the prefab, set the instantiation data, and then spawn.
    /// </summary>
    public NetworkObject InstantiateSetDataAndSpawn(Vector3 position, Quaternion rotation, InstantiateData instantiateData)
    {            
        var instance = GetPrefabInstance(position, rotation, instantiateData);

        // Set the spawndata before spawning
        m_NetworkManager.PrefabHandler.SetInstantiationData(instance, instantiateData);

        instance.GetComponent<NetworkObject>().Spawn();
        return instance;
    }

    /// <summary>
    /// Returns an instance of the registered prefab (no instantiation data set yet)
    /// </summary>
    public NetworkObject GetPrefabInstance(Vector3 position, Quaternion rotation, InstantiateData instantiateData)
    {
        // Optional to include your own position and/or rotation within the InstantiateData.
        var instance = Object.Instantiate(m_RegisteredPrefabToSpawn, position, rotation);
        var meshRenderers = instance.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in meshRenderers)
        {
            // Assign the color to each MeshRenderer (just a psuedo example)
            renderer.material.color = instantiateData.Color;
        }

        return instance.GetComponent<NetworkObject>();
    }

    /// <inheritdoc/>
    public override NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, InstantiateData instantiateData)
    {
        // For non-authority instances, we can just get an instance based off of the passed in InstantiateData
        return GetPrefabInstance(position, rotation, instantiateData);
    }

    /// <inheritdoc/>
    public override void Destroy(NetworkObject networkObject)
    {
        Object.DestroyImmediate(networkObject.gameObject);
    }
}
```
When instantiating from user script for a host, server, or distributed authority client, the above `InstantiateSetDataAndSpawn` method is used. When instantiating on non-authority instances the `GetPrefabInstance` is used since the authority provides the instantiation data.


While setting the color of a `MeshRenderer` doesn't really provide a broad spectrum use case scenario for `NetworkPrefabInstanceHandlerWithData`, the above example does provide you with a simple implementation in order to better understand:

- Creating a serializable structure to be serialized with the spawned network prefab.
- Creating a component (this case a `NetworkBehaviour`) that is used to instantiate the handler and configure the network prefab to be registered.
- Creating a `NetworkPrefabInstanceHandlerWithData` derived class that:
  - Handles instantiating and destroying instances of the registered prefab.
  - Provides an example of instantiating the network prefab, setting the instantiation data for the instance, and then spawning the instance.
  - Provides an example of taking the de-serialized instantiation data and using one (or more) fields to configure the network prefab instance prior to it being spawned.

### Pre-instantiation data vs spawn data

While the `NetworkPrefabInstanceHandlerWithData` feature provides you with the ability to include pre-spawn instantiation data that you can then use to define the instance before the instance is instantiated and/or spawned, there are some subtle differences between pre-spawn serialized data and spawn serialized data.

- *Pre-spawn serialized data*:
  - Is included in the `NetworkObject` serialized data.
  - Is extracted and de-serialized prior to invoking the `NetworkPrefabInstanceHandlerWithData.Instantiate` method.
  - Can be used to identify pre-instantiated objects and/or the type of network prefab to be spawned.
  - Has no context for the "to-be spawned" network prefab, unless you provide that in the instantiation data.
  - Can include any kind of serialized data types supported by Netcode for GameObjects.
  - Can be used to spawn pre-determined instances that are created prior to connecting to the session or created while synchronizing with a session.
- *Spawn serialized data*:
  - Is not available until the network prefab is already instantiated and is in the middle of or has finished the spawn process.
  - Cannot be used to define what network prefab to instantiate.
  - Typically will include other netcode related states (NetworkVariables, RPCs, etc.).

When it comes to including instantiation data you should be cautious about including data from already spawned objects. It is not that you are not allowed to do this, but that you need to assure that the serialized information of already spawned objects, like a `NetworkBehaviourReference` or `NetworkObjectReference`, exists prior to being used.


## Further Reading

- [Object pooling](./object-pooling.md)
- [Authority prefab overrides](../basics/object-spawning.md#taking-prefab-overrides-into-consideration)
