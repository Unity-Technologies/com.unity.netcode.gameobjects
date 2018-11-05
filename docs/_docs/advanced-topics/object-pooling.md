---
title: Object Pooling
permalink: /wiki/object-pooling/
---


The MLAPI has built-in support for Object Pooling. This is useful for frequently used objects such as bullets.

_This feature, and it's documentation might be outdated_


### Usage
Here are some examples of how to use the Object Pooling system. Note that all of the calls can only be used on the server.
#### Create a pool
```csharp
//poolName is a string
//spawnablePrefabIndex is the index the object has in your spawnable prefab list
//size is the amount of objects in the pool. More objects require more memory and resources.
NetworkPoolManager.CreatePool(myPoolName, mySpawnablePrefabIndex, mySize);
```

#### Destroy a pool
```csharp
NetworkPoolManager.DestroyPool(myPoolName);
```
#### Spawn object
```csharp
NetworkPoolManager.SpawnPoolObject(myPoolName, myPos, myRot);
```
#### Destroy object
```csharp
NetworkPoolManager.DestroyPoolObject(myNetworkedObject);
```