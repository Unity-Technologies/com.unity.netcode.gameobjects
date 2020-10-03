---
title: Networked Object
permalink: /wiki/networked-object/
---

The NetworkedObject is a fairly simple component. It has no settings. It's a component that indicates that a game object is well, networked. 

If you want to use NetworkedBehaviours. You need a NetworkedObject at the same GameObject or in a parent. Each NetworkedObject has a "netId", a networkId for the GameObject. This is used by many parts of the MLAPI. From the message targeting system to the object spawning.

The component's presence should have no performance impact as it has no game loop.