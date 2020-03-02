---
title: Object Visibility
permalink: /wiki/object-visibility/
---

Starting with MLAPI version 6.0.0, clients have no explicit knowledge of all objects or clients that are connected to the server.
This allows you to only show a subset of objects to any client at any given time. To allow this, a visibility API was introduced
to the NetworkedObject component and consists of 4 parts.


The first part is a callback that gets invoked when new clients connect or when the object is about to get spawned. It askes whether the object should be shown to a specific client, if you do not register this callback, it will default to true, meaning visible.

```csharp
NetworkedObject netObject = GetComponent<NetworkedObject>();
netObject.CheckObjectVisibility = ((clientId) => {
    // return true to show the object, return false to hide it


    if (Vector3.Distance(NetworkingManager.Singleton.ConnectedClients[clientId].PlayerObject.position, transform.position) > 5)
    {
        // Only show the object to players that are within 5 meters. Note that this has to be rechecked by your own code
        // If you want it to update as the client and objects distance change.
        // This callback is usually only called once per client
        return true;
    }
    else
    {
        // Dont show this object
        return false;
    }
});
```

To change the visibility during the game, you can use the following API's

```csharp
NetworkedObject netObject = GetComponent<NetworkedObject>();
netObject.NetworkShow(clientIdToShowTo);
```
and
```csharp
NetworkedObject netObject = GetComponent<NetworkedObject>();
netObject.NetworkHide(clientIdToHideFrom);
```
