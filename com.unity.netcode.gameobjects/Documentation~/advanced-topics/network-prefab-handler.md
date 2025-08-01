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

Netcode will use the `Instantiate` and `Destroy` methods in place of default spawn handlers for the `NetworkObject` used during spawning and despawning.  Because the message to instantiate a new `NetworkObject` originates from the [authority](../terms-concepts/authority.md), not all game clients will have the Instantiate method. All non-authority clients will have the instantiate method invoked if the `INetworkPrefabInstanceHandler` implementation is registered with `NetworkPrefabHandler` (`NetworkManager.PrefabHandler`) and the authority spawns the registered/associated `NetworkObject`.

### INetworkPrefabInstanceHandler

This is the simplest prefab handler description. Use the `INetworkPrefabInstanceHandler` for situations where the prefab override behaviour is consistent and known.

```csharp
    public interface INetworkPrefabInstanceHandler
    {
        NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation);
        void Destroy(NetworkObject networkObject);
    }
```

### NetworkPrefabInstanceHandlerWithData

the `NetworkPrefabInstanceHandlerWithData` allows for sending custom data from the authority during object spawning. This extra data can then be used to change the behavior of the `Instantiate` method. An implementation of `NetworkPrefabInstanceHandlerWithData` allows for sending any custom type that is serializable using [INetworkSerializable](advanced-topics/serialization/inetworkserializable.md).

```csharp
public abstract class NetworkPrefabInstanceHandlerWithData<T> : INetworkPrefabInstanceHandlerWithData
    where T : struct, INetworkSerializable
{
    public abstract NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, T instantiationData);
    public abstract void Destroy(NetworkObject networkObject);
}
```

## Prefab handler registration

Once you have created a class to be your prefab handler, you can then register the class with the network prefab handler system using `NetworkManager.PrefabHandler.AddHandler`. Prefab handlers are registered against a NetworkObject's [GlobalObjectIdHash](../basics/networkobject.md#using-networkobjects).

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

Prefab handlers can be unregistered using `NetworkManager.PrefabHandler.RemoveHandler`.

## Object spawning with prefab handlers

Once a prefab handler is registered Netcode will automatically use the defined `Initialize` and `Destroy` methods to manage the object lifecycle. [Spawn the network prefab as usual](../basics/object-spawning.md#spawning-a-network-prefab-overview) and the `Initialize` method will be called on whichever handler is registered with the spawned network prefab.

Note that the `Initialize` method is only called on non-authority clients. To customize network prefab behavior on the authority, you can use [prefab overrides](../basics/object-spawning.md#taking-prefab-overrides-into-consideration).

### Object spawning with custom data

For handlers that support custom data, the data to send needs to be manually set. To do this, call `SetInstantiationData` before calling the `Spawn` method. If `SetInstantiationData` is not called, the `default` implementation will be sent to the `Instantiate` call.

```csharp
public struct SpawnData : INetworkSerializable
{
    public int version;
    public string name;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref version);
        serializer.SerializeValue(ref name);
    }
}

public class CountingObject : NetworkBehaviour
{
    [SerializeField] private GameObject prefabToSpawn;
    public string currentName

    public void SpawnObject(int objectTypeToSpawn)
    {
        var instance = Instantiate(prefabToSpawn);

        // Set data before spawning
        var customSpawnData = new SpawnData { version: objectTypeToSpawn, name: currentName}
        NetworkManager.Singleton.PrefabHandler.SetInstantiationData(instance, customSpawnData);

        instance.Spawn();
    }
}
```

All non-authority clients will then receive this data when `Instantiate` is called.

```csharp
public class SpawnWithDataSystem : NetworkPrefabInstanceHandlerWithData<SpawnData>
{
    public override NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation, SpawnData data)
    {
        // Create the client-side prefab using the spawn data from the authority
        var prefabToSpawn = GetPrefabForVersion(data.version);
        var instance = Instantiate(prefabToSpawn);

        var obj = instance.GetComponent<CountingObject>();
        obj.currentName = data.name;

        return instance.GetComponent<NetworkObject>();
    }

        public override void Destroy(NetworkObject networkObject)
    {
        Object.DestroyImmediate(networkObject.gameObject);
    }

    private GameObject GetPrefabForVersion(int version)
    {
        // Here you can implement logic to return a different client-side game object
        // depending on the information sent from the server.
    }
}
```

## Further Reading

- [Object pooling](./object-pooling.md)
- [Authority prefab overrides](../basics/object-spawning.md#taking-prefab-overrides-into-consideration)
