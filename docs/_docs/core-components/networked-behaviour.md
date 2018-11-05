---
title: Networked Behaviour
permalink: /wiki/networked-behaviour/
---

NetworkedBehaviour is a abstract class that derives from MonoBehaviour, it's the base class all your networked scripts should derive from. It's what provides messaging functionality and much more. Each NetworkedBehaviour will belong to a NetworkedObject, it will be the first parent or first component on the current object that is found.


## Properties
##### isLocalPlayer
This returns true if this is the player object of our own client.
##### isOwner
This returns true if we own this object.
##### isOwnedByServer
Returns true if this object is owned by the server.
##### isServer
Returns true if we are a server.
##### isClient
Returns true if we are a client.
#### isHost
Returns true if we are a host.
#### NetworkId
Returns the NetworkId of the NetworkedObject that owns this Behaviour.
#### OwnerClientId
Returns the clientId of the Owner of this object