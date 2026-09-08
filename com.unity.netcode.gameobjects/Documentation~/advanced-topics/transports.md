# Transports

Use a transport layer to establish communication between your application and different hosts in a network.

Netcode for GameObjects uses [Unity Transport](https://docs.unity3d.com/Packages/com.unity.transport@latest) by default and also provides a [single player transport](#single-player-transport).

## Introduction to transports

A transport layer is a software layer that provides communication services between applications on different hosts in a network. It's responsible for establishing, maintaining, and terminating connections, as well as ensuring reliable data transfer.

Transport layers are essential for networked applications, as they provide the following key features:

- *Connection-oriented communication* to ensure a robust connection before exchanging data with a handshake protocol.
- *Maintain order delivery* for your packets to fix any discrepancies from the network layer in case of packet drops or device interruption.
- *Ensure data integrity* by requesting retransmission of missing or corrupted data through using checksums.
- *Control the data flow* in a network connection to avoid buffer overflow or underflow, which can cause unnecessary network performance issues.
- *Manage network congestion* by mediating flow rates and node overloads.
- *Adjust data streams* to transmit as byte streams or packets.

## Unity Transport package

Netcode for GameObjects uses the [Unity Transport](https://docs.unity3d.com/Packages/com.unity.transport@latest) package as its default transport layer. Unity Transport is a low-level networking library that provides a high-performance, cross-platform transport layer for multiplayer games and applications. It's designed to be flexible and extensible, allowing developers to implement custom transport protocols if needed.

Netcode for GameObjects provides a [`UnityTransport`](xref:Unity.Netcode.Transports.UTP.UnityTransport) class you can use to configure and manage the Unity Transport package. This class provides a simple interface for setting up and managing network connections, as well as sending and receiving data.

## Single player transport

Netcode for GameObjects also provides a [single player transport](./singleplayer.md) that allows for easy switching between multiplayer and single player configurations.

## Custom transports

You can use any community-contributed custom transport implementations or write your own.

Community transports are interchangeable transport layers for Netcode for GameObjects and can be installed with the Unity Package Manager. After installation, the transport package will appear in the **Select Transport** dropdown of the NetworkManager. Refer to the [Netcode community contributed transports](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports) for more information.

To start writing your own transport layer and contributing to the community, refer to the [Netcode community contribution repository](https://github.com/Unity-Technologies/multiplayer-community-contributions) for starting points and how to add your content.
