#  Reconnecting mid-game

In a multiplayer game, clients might get disconnected from the server for a variety of reasons (such as network issues or application/device crashes). For those reasons, you might want to allow your players to reconnect to the game.

# Considerations

Review the following considerations before allowing clients to reconnect in the middle of an active game:

- Clients might have specific state and in-game data associated with them. If you want them to reconnect with that same state and data, implement a mechanism to keep the association of state and data with the client for when they reconnect. Refer to [Session Management](session-management.md) for more information.
- Depending on your game's requirements, make sure the client's state is reset and ready to connect before attempting reconnection. This might require resetting some external services.
- When using [scene management](../basics/scenemanagement/scene-management-overview.md) and multiple additive scenes, there is a specific case to keep in mind. During the synchronization process, which launches when connecting to a game, if the client's active main scene is the same as the server's, it won't start a scene load in single mode for that scene. Instead, it loads all additive scenes loaded on the server. This means that if the client has additive scenes loaded, it won't unload them like it would if the client's main scene was different than the server's.

  - For example, if during a game, the server loads **main scene A**, then additively loads scenes **B** and **C**, the client has all three loaded. If the client disconnects and reconnects without changing scenes, the scene synchronization process recognizes that **main scene A** is already loaded on the client, and then proceeds to load the server's additive scenes. In that case, the client loads the scenes **B** and **C** a second time and then has two copies of those scenes loaded.

  However, if, while the client is disconnected, the server loads or unloads a scene additively, there is also a mismatch between the scenes loaded on the client and on the server. For example, if the server unloads scene **C** and loads scene **D**, the client loads scene **D** when synchronizing, but doesn't unload scene C, so the client has loaded scenes **A**, **B** (twice), **C**, and **D**, while the server only has loaded scenes **A**, **B**, and **D**.

  - If you want to avoid this behavior, you can
    - Load a different scene in single mode on the client when disconnecting
    - Unload additive scenes on the client when disconnecting
    - Unload additive scenes on the client when reconnecting
    - Use the `NetworkSceneManager.VerifySceneBeforeLoading` callback to prevent loading scenes already loaded on the client. However, this won't handle unloading scenes that were unloaded by the server between the time the client disconnected and reconnected.

If you want to avoid this behavior, you can:

- Load a different scene in single mode on the client when disconnecting

- Unload additive scenes on the client when disconnecting

- Unload additive scenes on the client when reconnecting

- Use the `NetworkSceneManager.VerifySceneBeforeLoading` callback to prevent loading scenes already loaded on the client. However, this won't handle unloading scenes that the server unloaded between the time the client disconnected and reconnected.

# Automatic reconnection

For a smoother experience for your players, clients can automatically try to reconnect to a game when they lose a connection unexpectedly.

To implement automatic reconnection:

- Define what triggers the reconnection process and when it should start.

  - For example, you can use the `NetworkManager.OnClientDisconnectCallback` callback, or some other unique event depending on your game.

- Ensure the `NetworkManager` shuts down before attempting any reconnection.

  - You can use the `NetworkManager.ShutdownInProgress` property to manage this.

- Add additional checks to ensure automatic reconnection doesn't trigger under the wrong conditions.

  - Examples

    - A client purposefully quits a game

    - The server shuts down as expected when the game session ends

    - A client gets kicked from a game

    - A client is denied during connection approval

Depending on your game, you might want to add the following features as well:

- Include multiple reconnection attempts in case of failure. You need to define the number of attempts, ensure that `NetworkManager` properly shuts down between each try, and reset the client's state (if needed).

- Offer an option for players to cancel the reconnection process. This might be useful when there are a lot of reconnection attempts or when each try lasts a long duration.

## Automatic reconnection example


The entry point for this feature is in [this class](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/ConnectionManagement/ConnectionState/ClientReconnectingState.cs). Boss Room's implementation uses a state inside a state machine that starts a coroutine on entering it ( `ReconnectCoroutine`) that attempts to reconnect a few times sequentially, until it either succeeds, surpasses the defined maximum number of attempts, or is cancelled. (Check out `OnClientConnected`, `OnClientDisconnect`, `OnDisconnectReasonReceived`, and `OnUserRequestedShutdown`.)

The reconnecting state is entered when a client disconnects unexpectedly. (Check out `OnClientDisconnect` in [this class](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/v2.2.0/Assets/Scripts/ConnectionManagement/ConnectionState/ClientConnectedState.cs))

> [!NOTE]
> This sample connects with [Lobby](https://docs.unity.com/lobby/unity-lobby-service-overview.html) and [Relay](https://docs.unity.com/relay/get-started.html) services, so the client must make sure it has left the lobby before each reconnection try.
