# Single player sessions

Netcode for GameObjects provides a [`SinglePlayerTransport`](xref:Unity.Netcode.Transports.SinglePlayer.SinglePlayerTransport) that derives from [`NetworkTransport`](xref:Unity.Netcode.NetworkTransport).

You can use the `SinglePlayerTransport` to run a single player session without having to modify your primary networking script.

## Add the single player transport

- Add the `SinglePlayerTransport` to your NetworkManager.
- You can create a custom `MonoBehaviour` component to handle your connection flow or you can derive from `NetworkManager` and add additional methods/logic to handle starting a single player session or multiplayer session.
  - When starting a single player session, prior to starting the NetworkManager as a host (_required_), you will want to assign the `SinglePlayerTransport` to the `NetworkManager.NetworkConfig.NetworkTransport`.
  - When starting a multiplayer session, prior to starting the NetworkManager, you will want to assign the `UnityTransport` (_or any other `NetworkTransport` derived class that you might use for multiplayer sessions_) to the `NetworkManager.NetworkConfig.NetworkTransport`.

## Example script

The following is an example component script that provides a single method to start a single or multiplayer session:

[!code-cs[](../../Tests/Runtime/DocumentationCodeSamples/Configuration/SinglePlayerSessions.cs#SinglePlayerTransportExample)]
