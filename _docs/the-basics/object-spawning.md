---
title: Object Spawning
permalink: /wiki/object-spawning/
---

To spawn an object. Make sure the object is in the spawnable prefabs array. Then simply instantiate the object. And 
invoke the spawn method on the NetworkedObject component that should be attached to the prefab. This should only be done on the server as the object will automatically replicate on the other clients.

Any objects already on the server with NetworkedObject components (static scene objects) will get automatically replicated. When objects are spawned the position and rotation is synced. This means that serialized data gets lost on the clients. It's thus recommended to place serialized data in NetworkedVars.


Here is an example on how to spawn a object
```csharp
GameObject go = Instantiate(myPrefab, Vector3.zero, Quaternion.identity);
go.GetComponent<NetworkedObject>().Spawn();
```