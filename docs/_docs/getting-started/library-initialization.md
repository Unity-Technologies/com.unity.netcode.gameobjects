---
title: Library Initialization
permalink: /wiki/library-initialization/
---

Initializing the MLAPI is fairly simple. You need a GameObject with the NetworkingManager component added to it. The NetworkingManager class has a static singleton reference to itself making it easy to access from anywhere. The first configuration you have to do is to set the Transport. You can read more about Transports on the [Custom Transports](/wiki/custom-transports/) page. 

First add the MLAPI library to your using declarations.
```
using MLAPI;
```

To initialize the library. You have three options.

### Host mode

This mode runs a Server and a virtual Client connected to its own server. The virtual client has no real network connection to the server, but instead just talk via message queues. This makes the host both a Server and a Client in the same process.

Usage:
```csharp
NetworkingManager.Singleton.StartHost();
```


### Client mode

This mode runs a Client that connects to a Server or Host.

Usage:
```csharp
NetworkingManager.Singleton.StartClient();
```

### Server mode

This mode runs a Server which other Clients can connect to. It has no own client attached, and thus lack it's own player object and such. This is the "dedicated server" mode.

Usage:
```csharp
NetworkingManager.Singleton.StartServer();
```
