---
title: TrackedObject
permalink: /wiki/tracked-object/
---

The TrackedObject component is a component part of the lag compensation system. While being fairly simplistic, it's important to note that it might be fairly resource demanding but it's existence plays an important role in the lag compensation system.

The TrackedObject component keeps track of the position and rotation of an object every event tick and stores it in a doubly linked list. Thus it can be fairly resource demanding. The rate at which it will track objects can be defined as the EventTickrate in the NetworkingConfiguration, increasing this will increase precision.

When it's time to lag compensate, we lookup the closest points to the specified time and try to find a middle point between the positions and rotations.