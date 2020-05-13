---
title: Common Mistakes
permalink: /wiki/common-mistakes/
---

This is a collection of common mistakes:

- [`NullReferenceException` when trying to start a server/host/client](#err-001)
- [`NullReferenceException` when trying to send an RPC to the server](#err-002)

---

### <a name="err-001"></a>`NullReferenceException` when trying to start a server/host/client

#### Problem
When trying to start a server, host, or client by executing one of these lines of code

```csharp
NetworkingManager.Singleton.StartServer()
NetworkingManager.Singleton.StartHost()
NetworkingManager.Singleton.StartClient()
```

the following exception is thrown:

```csharp
NullReferenceException: Object reference not set to an instance of an object
```

#### Solution
You most likely forgot to add the `NetworkingManager` component to a game object in your scene.

---

### <a name="err-002"></a>`NullReferenceException` when trying to send an RPC to the server

#### Problem
When the client tries to run `InvokeServerRpc`, the following exception is thrown:

```csharp
NullReferenceException: Object reference not set to an instance of an object
```

#### Solution
You most likely forgot to `Spawn()` your object.

Run `Spawn()` on your `NetworkedObject` component as the server to fix this issue.
