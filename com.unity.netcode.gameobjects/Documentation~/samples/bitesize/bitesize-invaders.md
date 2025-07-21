---
id: bitesize-invaders
title: Invaders sample
description: Learn more about game flow, modes, unconventional movement networked, and a shared timer using Netcode for GameObjects.
---

The [Invaders Sample Project](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/main/Deprecated/Invaders) to understand the game flow and modes with  Netcode for GameObjects (Netcode) using Scene Management, Unconventional Movement Networked, and a Shared Timer between clients updated client-side with server side seeding.

## Game Flows

Most Multiplayer games have multiple Networked Scenes, where players can join, communicate, and progress through scenes and maps together. This is a game flow: players join and establish communication together, the server determines the next scene or map, and transitions all clients to the new scene and loads the required map. This ensures players can play together.

The logic and transitions are a specified game flow. To transition in a smooth manner, you need to create game flows and back-end systems that support those flows.

Invaders implements this game-flow by creating different controller classes that handle each Game Mode such as MainMenu, a simple Networked Lobby and InGame modes, and other support classes that help with the transition from one Game Mode to another.

The backbones of the flow/system mentioned above is consisting of two main components:

* `SceneTransitionHandler`
* `SceneState`
* `Lobby Controller`

### SceneTransitionHandler

A `SceneTransitionHandler` with a lightweight state machine allows you to track clients' progress in regards to Scene Loading. It notifies the server when clients finish loading so that the other listeners are informed, by subscribing to the `NetworkedSceneManager` load events and creating a wrapper around it that others can subscribe to.

Those events are invoked by `NetworkSceneManager` during the loading process. Invaders subscribe to these events when strating the server in the [MenuControl](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/MenuControl.cs) via the [SceneTransitionHandler.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/SceneTransitionHandler.cs):

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/MenuControl.cs#L16-L30
```

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/SceneTransitionHandler.cs#L90-L97
```

### SceneState

At the same time, we have implemented a light State Machine to keep track of the current `SceneState`. For example, the `SceneState` can indicate if players are in the Init or Bootstrap scene, Start or Lobby, or InGame. You can run a different Behavior or in this case a different `UpdateLoop` function, for each state.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/SceneTransitionHandler.cs#L25-L34
```

One example of how to update the current `SceneState` is in *[InvadersGame.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/InvadersGame.cs)*, in the `OnNetworkSpawn` function.

:::note
This class has the same role as the Lobby Controller, it acts as a Manager, for a specific part of the game.
:::

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/InvadersGame.cs#L156-L194
```

### Lobby Controller

A Lobby Controller is a Manager for the lobby. This is where we applied a simple Mediator Design Pattern that restricts direct communications between the objects and forces them to collaborate only using a moderator object. In this case, the *[LobbyControl.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/LobbyControl.cs)* handles Lobby interactions and state. This works hand-in-hand with the `SceneTransitionHandler`, by subscribing to the `OnClientLoadedScene` of that class in `OnNetworkSpawn`.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/LobbyControl.cs#L23-L45
```

Whenever the `OnClientLoadedScene` callback is called, the custom `ClientLoadedScene` function is also called. And that is the location where you add the new Player to a container that just loaded the Lobby Scene, generates user stats for it (which is just a random name), and then later sends an update to the rest of the users notifying them that someone new has joined the Lobby.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/LobbyControl.cs#L90-L107
```

When the players join this Lobby, they all need to click **Ready** before the game can progress to the next scene (before the Host can start the game). After players click Ready, you send a `ServerRPC` called `OnClientIsReadyServerRpc` (inside the `PlayerIsReady` function). When it arrives server-side, it marks the client state as ready based on its `ClientId`. You keep track of if a client is ready in the `m_ClientsInLobby` `Dictionary`.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/LobbyControl.cs#L195-L209
```

At the same time, to sync up with the rest of the clients and update their UI, we send a ClientRpc. The update is handled by the ClientRpc called `SendClientReadyStatusUpdatesClientRpc` in `UpdateAndCheckPlayersInLobby`.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/LobbyControl.cs#L126-L144
```

When all the players have joined the lobby and are ready, `UpdateAndCheckPlayersInLobby` calls `CheckForAllPlayersReady` to transition to the next scene.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/LobbyControl.cs#L146-L174
```

## Unconventional Networked Movement

Invaders has an easy movement type - moving only on one (horizontal) axis - which allows you to only modify the transform client-side without waiting for server-side validation. You can find where we perform the move logic in *[PlayerControl.cs](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/PlayerControl.cs)* in the `InGameUpdate` function . With the help of a [`NetworkTransform`](../../components/networktransform.md) that is attached directly to the Player Game Object, it will automatically sync up the Transform with the other clients. At the same time, it will smooth out the movement by interpolating or extrapolating for all of them.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/PlayerControl.cs#L176-L193
```

## Shared Start/Round Timer updated Client-Side

Games commonly have timers to display in the UI such as Start Timer, Round Timer, and Cooldowns. The Invaders sample also has a shared timer to ensure all players start the game at the same time. Otherwise, players with higher-end devices and better network access may have an unfair advantage by loading scenes and maps faster.

When you implement this kind of timer, usually you would use a `NetworkVariable<float>` to replicate and display the exact time value across all clients. To improve performance, you don't need to replicate that float every Network Tick to the Clients, which would only waste network bandwidth and some minimal CPU resources.

An alternative solution is to sync only the start of the timer to the clients, and afterwards only sync the remaining value of the timer when a new client joins. For the remaining time, clients can update the timer locally. This method ensures the server does not need to send the value of that timer every Network Update tick since you know what the approximated value will be.

In Invaders we chose the second solution instead, and simply start the timer for clients via an RPC.

### Start the game timer

First, use `ShouldStartCountDown` to start the timer and send the time remaining value to the client-side. This initiates and sends the value only once, indicating the game has started and how much time remains. The game then counts down locally using these values. See [Update game timer Client-Side](#update-game-timer-client-side).

Example code to start the countdown:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/InvadersGame.cs#L205-L229
```

In the case of a late-joining client, if the timer is already started, we send them an RPC to tell them the amount of time remaining.

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/InvadersGame.cs#L196-L203
```

### Update game timer Client-Side

On the client-side, use the `UpdateGameTimer` to locally calculate and update the `gameTimer`. The server only needs to be contacted once to check if the game has started (`m_HasGameStared` is true) and the `m_TimeRemaining` amount, recieved by `ShouldStartCountDown`. When met, it locally calculates and updates the `gameTimer` reducing the remaining time on the client-side for all players. When `m_TimeRemaining` reaches 0.0, the timer is up.

Example code to update the game timer:

```csharp reference
https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/v1.2.1/Basic/Invaders/Assets/Scripts/InvadersGame.cs#L276-L299
```
