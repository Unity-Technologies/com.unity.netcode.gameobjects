# Distributed Authority Social Hub sample

The [Distributed Authority Social Hub Sample](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/tree/main/Basic/DistributedAuthoritySocialHub) is a project that demonstrates Distributed Authority's features and helps you integrate Distributed Authority into your own game projects.

Within Social Hub, you can explore Distributed Authority in a sandbox-like environment. You can:

- Automatically redistribute ownership of NetworkObjects across connected clients
- Experience responsive gameplay through client-side NetworkObject spawning and deferred NetworkObject despawning
- Transfer ownership of NetworkObjects with variable ownership flags
- Host migration of a networked game session

## Prerequisites

Social Hub uses services from Unity Gaming Services to facilitate connectivity between players. To use these services inside your project, you must:

1. [Create an organization](https://support.unity.com/hc/en-us/articles/208592876-How-do-I-create-a-new-Unity-organization) inside the Unity Dashboard.
2. Register your Unity project with that organization's cloud ID.

## The Bootstrap scene

This is the entry point to the sample. After the Bootstrap scene loads, the MainMenu scene appears.

**Note**: Be sure you have registered the project with a cloud ID.

## The MainMenu scene

To create or join an existing game:

1. Enter a player name and a session name.
2. Select **Start**.

A successful load indicates that you have correctly registered your project with a cloud ID.

## The HubScene_TownMarket scene

This a sandbox-like environment that supports 64 players. The control model is WASD controls. Move around the level with client-authority, and use **E** to interact with crates and pots. These are NetworkObjects that you can pick up, drop, and throw. Attach one NetworkObject to another with an animated rig through the use of FixedJoints. As a client picks up a NetworkObject, authority of that NetworkObject transfers to that client.

A client has authority over its player NetworkObject. Therefore, the movement, animations, and interactions that a client performs are client-authoritative.

This sample inherently supports host migration out of the box. Load multiple clients, and witness the automatic distribution of ownership of NetworkObjects when the original session owner leaves the session.

## Additional resources

- Get help and ask questions on [Multiplayer Discussions](https://discussions.unity.com/lists/multiplayer).
- Join the community of Multiplayer creators on the [Multiplayer Networking Discord](https://discord.gg/unity-multiplayer-network).
- [Request a feature or report a bug](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/issues/new/choose).
