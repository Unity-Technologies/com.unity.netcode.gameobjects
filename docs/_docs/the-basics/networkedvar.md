---
title: NetworkedVar
permalink: /wiki/networkedvar/
---

NetworkedVar is the way data can be synchronized between peers in abstracted ways. The data can be custom containers and complex structures such as inventory structs.

By default, the MLAPI comes with 3 different containers. NetworkedList, NetworkedDictionary and NetworkedVar. The NetworkedVar container is built to store simple data types such as floats and ints. The List & Dictionary implementations are wrappers around the .NET equivalents. They are event-driven and have a list of events to be synced. The default implementations come with lot's of flexibility in terms of settings. Containers can be setup to sync Client To Server, Server To Client or Bidirectional. It can also be set to target specific clients using custom delegates.

Since the NetworkedVar container is a wrapper container around the value, the value has be accessed via the .Value property.


<div class="panel panel-warning">
    <div class="panel-heading">
        <h3 class="panel-title">Disclaimer</h3>
    </div>
    <div class="panel-body">
        The NetworkedVar, NetworkedList and NetworkedDictionary implementations are <b>primarily</b> designed as samples showing how to create INetworkedVar structures. The NetworkedVar container is however considered production ready for simple types.
    </div>
</div>

<div class="panel panel-warning">
    <div class="panel-heading">
        <h3 class="panel-title">Note</h3>
    </div>
    <div class="panel-body">
      You must remember to add the NetworkedObject component to the game object to which your script belongs
    </div>
</div>

To create your own NetworkedVar container, simply create a class with the INetworkedVar interface and declare it as a field of a NetworkedBehaviour. To learn how to write your own containers for more complex structures, see the NetworkedVar implementation. To learn how to do custom delta encoding on complex structures. See the SyncedDictionary and SyncedLIst implementations.

### Permissions
By default NetworkedVar and it's subclasses can only be wrote to by the server (NetworkedVarPermission.ServerOnly). To change that set the permission to the desired value during initialization:
```csharp
private NetworkedVar<float> myFloat = new NetworkedVar(new NetworkedVarSettings {WritePermission = NetworkedVarPermission.OwnerOnly}, 5);
```

### Example
```csharp
private NetworkedVar<float> myFloat = new NetworkedVar<float>(5.0f);

void MyUpdate()
{
    myFloat.Value += 30;
}
```

### Single Sync Values
If you want values to be synced only once (at spawn), the built-in containers send rate can be set to a negative value.

### Serialization
Since the NetworkedVar class is a generic, editor serialization is NOT supported, it's only avalible through editor scripts for viewing the values. To get proper serialization. A clone of the NetworkedVar implementation has to be done for each type you wish to use. Ex: NetworkedVarInt where you replace all the usages of T with int.

The MLAPI provides a few default serializable implementations of the NetworkedVar, they are called NetworkedVar<TYPE> where "<TYPE>" is the type.
