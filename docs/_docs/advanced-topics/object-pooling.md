---
title: Object Pooling
permalink: /wiki/object-pooling/
---


The MLAPI has built-in support for Object Pooling. This is useful for frequently used objects such as bullets.
This can be achieved by registering custom spawn and destroy handlers.


### SpawnHandler
```csharp
SpawnManager.RegisterCustomSpawnHandler(SpawnManager.GetPrefabHash("myPrefabName"), (position, rotation, disabled) =>
{
    // Called when the MLAPI want's to spawn a prefab with the name "myPrefabName"
});
```
### DestroyHandler
```csharp
SpawnManager.RegisterCustomDestroyHandler(SpawnManager.GetPrefabHash("myPrefabName"), (networkedObject) =>
{
    // Called when the MLAPI want's to destroy the given NetworkedObject
});
```