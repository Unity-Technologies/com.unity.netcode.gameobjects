---
title: Networked Behaviour
permalink: /wiki/networked-behaviour/
---

NetworkedBehaviour is a abstract class that derives from MonoBehaviour, it's the base class all your networked scripts should derive from. It's what provides messaging functionality and much more. Each NetworkedBehaviour will belong to a NetworkedObject, it will be the first parent or first component on the current object that is found.