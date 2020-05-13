---
title: Object Spawning
permalink: /wiki/object-spawning/
---

To spawn an object. Make sure the object is in the spawnable prefabs array. Then simply instantiate the object. And 
invoke the spawn method on the NetworkedObject component that should be attached to the prefab. This should only be done on the server as the object will automatically replicate on the other clients. The object is owned by the server by default.

Here is an example on how to spawn a object (with server ownership)
```csharp
GameObject go = Instantiate(myPrefab, Vector3.zero, Quaternion.identity);
go.GetComponent<NetworkedObject>().Spawn();
```

The .Spawn() method takes 2 parameters, both with default values, so they are optional.
```csharp
public void Spawn(Stream spawnPayload = null, bool destroyWithScene = false);
```
The first parameter is a System.IO.Stream and can be retrieved in the NetworkStart() to sync values once when spawning this object. Note however, that the payload data is only available for people that get the spawn call straight away. People that join later on won't get the payload data.

The second parameter speaks for itself. If set to true, the object will be destroyed on scene switching. This can only be set inside the spawn call.

## Spawn with Ownership
Any changes to the Ownership can only be done after calling ```.Spawn()``` and are not instantly available. To spawn an object directly with a certain ownership use ```SpawnWithOwnership(ulong clientId)``` instead of the default spawn method. 

## Scene Objects
Any objects already on the server with NetworkedObject components (static scene objects) will get automatically replicated. 

There are **two** modes that define how scene objects are spawned. The first mode is *PrefabSync*. It can enabled in the NetworkConfig.

#### SoftSync
If PrefabSync is disabled, the MLAPI will use SoftSync. This allows scene objects to be non prefabs and they will not be replaced, thus keeping their serialized data. **This mode is recommended for single project setups**. This is the default since MLAPI 6.

#### PrefabSync
If it's enabled, every scene object has to be a prefab and scene objects are recreated on the client side. This means that serialized data gets lost on the clients. It's thus recommended to place serialized data in NetworkedVars. **This mode is ONLY recommended for Multi project setups**. This was the default before MLAPI 6.
