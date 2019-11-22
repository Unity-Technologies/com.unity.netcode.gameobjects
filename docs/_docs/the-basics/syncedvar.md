---
title: SyncedVar
permalink: /wiki/syncedvar/
---

SyncedVars are simple ways to have syncronized fields in NetworkedBehaviours. SyncedVars are similar to NetworkedVars but have a few differences:

1. They are slower
2. They are less customizable
3. They only sync from server to client
4. They require less code
5. They support serialization

### Example
Creating a SyncedVar is as easy as creating an attribute on the field. Note that properties are not supported.

```csharp
[SyncedVar]
public float mySyncedFloat = 5f;
```

### Single Sync Values
If you want values to be synced only once (at spawn), the send rate can be set to a negative value.