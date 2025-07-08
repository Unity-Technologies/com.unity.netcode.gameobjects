# Transports

Unity Netcode for GameObjects (Netcode) uses Unity Transport by default and supports UNet Transport (deprecated) up to Unity 2022.2 version.

## So what is a transport layer?

The transport layer establishes communication between your application and different hosts in a network.

A transport layer can provide:
* *Connection-oriented communication* to ensure a robust connection before exchanging data with a handshake protocol.
* *Maintain order delivery* for your packets to fix any discrepancies from the network layer in case of packet drops or device interruption.
* *Ensure data integrity* by requesting retransmission of missing or corrupted data through using checksums.
* *Control the data flow* in a network connection to avoid buffer overflow or underflow--causing unnecessary network performance issues.
* *Manage network congestion* by mediating flow rates and node overloads.
* *Adjust data streams* to transmit as byte streams or packets.

## Unity Transport package

Netcode's default transport Unity Transport is an entire transport layer that you can use to add multiplayer and network features to your project with or without Netcode. See the Transport [documentation](../../../transport/current/about) for more information and how to [install](../../../transport/current/install).

## Unity's UNet Transport Layer API

UNet is a deprecated solution that is no longer supported after Unity 2022.2. Unity Transport Package is the default transport for Netcode for GameObjects. We recommend transitioning to Unity Transport as soon as possible.

### Community Transports or Writing Your Own

You can use any of the community contributed custom transport implementations or write your own.

The community transports are interchangeable transport layers for Netcode and can be installed with the Unity Package Manager. After installation, the transport package will appear in the **Select Transport** dropdown of the `NetworkManager`. Check out the [Netcode community contributed transports](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports) for more information.

To start writing your own and contributing to the community, check out the [Netcode community contribution repository](https://github.com/Unity-Technologies/multiplayer-community-contributions) for starting points and how to add your content.
