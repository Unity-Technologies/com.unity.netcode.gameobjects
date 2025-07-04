# Logging

Netcode for GameObjects (Netcode) has built in support for logging which can be great for working with development builds, playtesting and more. If used in production, it should be noted that logs can be forged and do take up some bandwidth depending on the log sizes.

The logging functionality can be accessed with the NetworkLog API. If a `NetworkLog` is called on the server, the log won't be sent across the network but will instead just be logged locally with the `ServerClientId` as the sender. An example log would be `[MLAPI_SERVER Sender=0] Hello World!`. If the NetworkLog API is instead accessed from a client, it will first log locally on the sending client but will also be logged on the server.

## Examples

```csharp
if (IsServer)
{
    // This won't send any network packets but will log it locally on the server
    NetworkLog.LogInfoServer("Hello World!");
}

if (IsClient)
{
    // This will log locally and send the log to the server to be logged there aswell
    NetworkLog.LogInfoServer("Hello World!");
}
```
