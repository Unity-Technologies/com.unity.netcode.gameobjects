# Troubleshooting

See the following information for common troubleshooting for Netcode for GameObjects.

## NullReferenceException when trying to start a server/host/client

**Issue:** When trying to start a server, host, or client by executing one of these lines of code:

```csharp
NetworkManager.Singleton.StartServer()
NetworkManager.Singleton.StartHost()
NetworkManager.Singleton.StartClient()
```

The following exception is thrown:

```csharp
NullReferenceException: Object reference not set to an instance of an object
```

**Solution:** You most likely forgot to add the `NetworkManager` component to a game object in your scene.

## NullReferenceException when trying to send an RPC to the server

**Issue:** When the client tries to run `InvokeServerRpc`, the following exception is thrown:

```csharp
NullReferenceException: Object reference not set to an instance of an object
```

**Solution:** You most likely forgot to `Spawn()` your object. Run `Spawn()` on your `NetworkObject` component as the server to fix this issue.

## Server build is using 100% CPU

**Issue**: When running an MLAPI server created from a server build it has a cpu usage of 100% blocking all my other applications.

**Solution**: Unity server builds try to push as many Updates per second as possible. On a server this is most often not necessary. You can limit the target frame rate to reduce the amounts of Updates with this:
```csharp
Application.targetFrameRate = 30;
```
