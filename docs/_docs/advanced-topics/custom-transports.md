---
title: Custom Transports
permalink: /wiki/custom-transports/
---

The MLAPI supports custom transports. It uses UNET by default. You can also write custom transports. The MLAPI has a LiteNetLib transport example you can use (See the SampleTransports folder), or write your own one. 

To do so, you need a class implementing the IUDPTransport interface. The flow works like this

GetSettings gets invoked, you can give it any object.

if Server, RegisterServerListenSocket get's invoked and gives the settings object.

Usually, transports doesn't support support all channel types and event types. Sometimes they have more, in that case you manually have to do translation between them. See the LiteNetLib transport for examples.

In order to use your own transport, you have to set the Transport to "Custom" and set the NetworkConfig NetworkTransport to your own transport. To get started writing transport interfaces, the current implementations for Unet and LiteNetLib are great starting points for learning their flow. If you do write a transport for a well known transport, feel free to open a PR to add it to the default supported.