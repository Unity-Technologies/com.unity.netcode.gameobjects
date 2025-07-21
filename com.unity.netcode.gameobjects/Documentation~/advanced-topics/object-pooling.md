# Object pooling

Netcode for GameObjects (Netcode) provides built-in support for Object Pooling, which allows you to override the default Netcode destroy and spawn handlers with your own logic.  This allows you to store destroyed network objects in a pool to reuse later. This is useful for often used objects, such as projectiles, and is a way to increase the application's overall performance. By pre-instantiating and reusing the instances of those objects, object pooling removes the need to create or destroy objects at runtime, which can save a lot of work for the CPU. This means that instead of creating or destroying the same object over and over again, it's simply deactivated after use, then, when another object is needed, the pool recycles one of the deactivated objects and reactivates it.

See [Introduction to Object Pooling](https://learn.unity.com/tutorial/introduction-to-object-pooling) to learn more about the importance of pooling objects.

## NetworkPrefabInstanceHandler

You can register your own spawn handlers by including the `INetworkPrefabInstanceHandler` interface and registering with the `NetworkPrefabHandler`.
```csharp
    public interface INetworkPrefabInstanceHandler
    {
        NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation);
        void Destroy(NetworkObject networkObject);
    }
```
Netcode will use the `Instantiate` and `Destroy` methods in place of default spawn handlers for the `NetworkObject` used during spawning and despawning.  Because the message to instantiate a new `NetworkObject` originates from a Host or Server, both won't have the Instantiate method invoked. All clients (excluding a Host) will have the instantiate method invoked if the `INetworkPrefabInstanceHandler` implementation is  registered with `NetworkPrefabHandler` (`NetworkManager.PrefabHandler`) and a Host or Server spawns the registered/associated `NetworkObject`.

<!-- Commenting this out until we can get external code references working

The following example is from the Boss Room Sample. It shows how object pooling is used to handle the different projectile objects. In that example, the class `NetworkObjectPool` is the data structure containing the pooled objects and the class `PooledPrefabInstanceHandler` is the handler implementing `INetworkPrefabInstanceHandler`.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/Infrastructure/NetworkObjectPool.cs
```

Let's have a look at `NetworkObjectPool` first. `PooledPrefabsList` has a list of prefabs to handle, with an initial number of instances to spawn for each. The `RegisterPrefabInternal` method, called in `OnNetworkSpawn`, initializes the different pools for each Prefab as `ObjectPool`s inside the `m_PooledObjects` dictionary. It also instantiates the handlers for each Prefab and registers them. To use these objects, a user then needs to obtain it via the `GetNetworkObject` method before spawning it, then return the object to the pool after use with `ReturnNetworkObject` before despawning it. This only needs to be done on the server, as the `PooledPrefabInstanceHandler` will handle it on the client(s) when the network object's `Spawn` or `Despawn` method is called, via its `Instantiate` and `Destroy` methods. Inside those methods, the `PooledPrefabInstanceHandler` simply calls the pool to get the corresponding object, or to return it.

-->