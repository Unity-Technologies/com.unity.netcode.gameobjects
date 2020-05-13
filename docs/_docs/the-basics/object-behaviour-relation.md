---
title: Object & Behaviour Relation
permalink: /wiki/object-behaviour-relation/
---

The MLAPI's high level components (The RPC system and the Object Spawning System) relies on two concepts. One of them is the concept of NetworkedObjects, and the other one is the concept of NetworkedBehaviours.

### NetworkedObject
For an object to be replicated across the network it needs to have a NetworkedObject component. It also has to be registered as a NetworkedPrefab. When a NetworkedObject is considered "Spawned", it's replicated across the network so that everyone has their own version of the object. Each NetworkedObject gets assigned a NetworkId at runtime which is used to associate two NetworkedObjects across the network. Ex: One peer can say, Send this RPC to the object with the NetworkId 103 and everyone knows what object that is.

### NetworkedBehaviour
NetworkedBehaviour is an abstract class that inherits MonoBehaviour and adds additional functionality. Each NetworkedBehaviour is owned by a NetworkedObject. NetworkedBehaviours can contain RPC methods and NetworkedVars. When you call InvokeRPC, a message is sent containing your parameters, the networkId of the NetworkedObject that owns the NetworkedBehaviour that the Invoke was called on, and the "index" of the NetworkedBehaviour on the NetworkedObject. This means that if you run multiple projects, it's important that the order and amount of NetworkedBehaviours on each NetworkedObject is the same. It also means that NetworkedBehaviour's can only exist as a child or on the same object as a NetworkedObject that is actively Spawned.


#### RPC Flow
This is what happens when you send a RPC:

```
InvokeRpc:
    - Writes NetworkId
    - Writes Index of Behaviour
    - Writes Hash of the RPC Method Name
    - Writes Parameters or Body

Receive:
    - Reads NetworkId
    - Finds Object With Given NetworkId
    - Reads Behaviour Index
    - Finds The Behaviour At Index Provided On Object
    - Reads RPC Method Name Hash
    - Finds The Method With The Hash Provided
    - Invokes It's Delegate With Parameters Or Body
```
