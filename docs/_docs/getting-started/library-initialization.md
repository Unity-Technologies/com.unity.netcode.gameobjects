---
title: Library Initialization
permalink: /wiki/library-initialization/
---

Initializing the MLAPI is fairly simple. You need a GameObject with the NetworkingManager component added to it. The NetworkingManager class has a static singleton reference to itself making it easy to access from anywhere.
To initialize the library. You have three options.

### Host mode

This mode runs a Server and a virtual Client connected to its own server.

Usage:
```csharp
NetworkingManager.singleton.StartHost();
```


### Client mode

This mode runs a Client that connects to a Server or Host.

Usage:
```csharp
NetworkingManager.singleton.StartClient();
```

### Server mode

This mode runs a Server which other Clients can connect to.

Usage:
```csharp
NetworkingManager.singleton.StartServer();
```