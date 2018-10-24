---
title: Lag Compensation
permalink: /wiki/lag-compensation/
---

The MLAPI has built in Lag Compensation which is useful for Server Authoritative actions. For example if a client tells the server to shoot, and it's aimed right at another player. By the time the messages reaches the server, the client that was hit would already have moved. Lag compensation allows the server to rewind time to predict what the other client saw.

Each object that you want to be included in the lag compensation needs a TrackedObject script on it.
A lag compensation can then be executed like this:
```csharp
// 1f is the amount of seconds to go back in time.
LagCompensationManager.Simulate(1f, () =>
{
    // Do your stuff here. Raycasts etc.
});
```
or
```csharp
// One in this case is the clientId 1's roundtriptime / 2
LagCompensationManager.Simulate(1, () =>
{
    // Do your stuff here. Raycasts etc.
});
```