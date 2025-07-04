# Create a game with a listen server and host architecture

## What is a listen server

A listen server acts as both a server and client on a single player's machine for multiplayer game play. This means one player both plays the game and owns the game world while other players connect to this server.

This set up has performance, security, and cost considerations for implementation.

### Disadvantages

- Network performance relies on the residential internet connection of the host player and their machine's output efficiency. Both may cause the remote players connecting to the host to suffer performance issues.
- The host player may have a large latency advantage over the remote players.
- With access to all of the game world's information, the host player has an easier opportunity to cheat.
- The server, i.e. the game ceases to exist when the host player leaves the game.

### Advantages

- Essentially free compared to dedicated server options.
- Doesn't require special infrastructure or forward planning to set up. This makes them common use for LAN parties because latency and bandwidth issues aren't a concern.

## When to use a listen server architecture

Listen server architecture is a popular choice for single player games that want to provide the option to add a friend into an existing game world. Listen servers are best suited for a smaller player group (< 12) and games that don't require a persistent world.

> [!NOTE]
> *Persistent world* in Netcode for GameObjects means "a persistent online world."
> For example, the game state isn't bound to a player or a session, but is typically tied to the host.

In contrast to dedicated servers, listen servers are cheaper without the need to run dedicated authoritative servers for a game. Often, the listen server approach is chosen to avoid setting up a system to orchestrate the game server fleet.

> [!NOTE]
> You still need to set up matchmaking for your player to join together to play. A listen server game requires redirecting players to the client-hosted server.

## Connecting to a listen server

Personal computers are hidden behind NATs (Network Address Translation devices) and routers to protect them from direct access. To connect to a listen server, you may choose an option such as [port forwarding](#port-forwarding), a [relay server](#relay-server), [NAT punch-through](#nat-punchthrough), or a [NAT punch with relay fallback](#nat-punch-and-relay-fallback).

### Port Forwarding

With port forwarding, the host can allow another player to connect to a listen server by forwarding a public port on their router to a machine in their local network.

Considerations:

* It has risks. By opening ports, you create direct pathways for hackers and malware attacks to access your system.
* The host must manually open the ports on their router, and this requires some technical knowledge the average user may not have.
* The host may not always have access to their router and would not be able to open a port. For example, the host may be using a mobile device, a corporate network, or public WiFi.

Port forwarding may not be a viable option for a released game but can be useful for development. For more information about port forwarding, see https://portforward.com/.

### Relay Server

A dedicated server has ports already forwarded for players to connect anytime. These servers can run in the cloud or a data center. The relay server option uses these servers to send data between players.

In a listen server relay scenario, the host and all clients connect to the same listen server. Clients send packets to the relay server, which then redirects these packets to the intended recipient.

Advantages:

* Compared to direct connecting through port forwarding, a relay server should always work for any client.
* A relay server knows when the host client disconnects and can inform the other clients or start a host migration process.

Disadvantages:

A relay server costs money, and the round trip times for packet exchange may be higher because they have to go through the relay server instead of being sent directly to the other client.

### NAT Punch-through

Network Address Translation (NAT) punch-through, also known as hole punching, opens a direct connection without port forwarding. When successful, clients are directly connected to each other to exchange packets. However, depending on the NAT types among the clients, NAT punching often fails.

Ways to NAT punch:
* Session Traversal Utilities for NAT [STUN](../reference/glossary/network-terms.md#session-traversal-utilities-for-nat-stun)
* Interactive Connectivity Establishment [ICE](../reference/glossary/network-terms.md#interactive-connectivity-establishment-ice)
* User Datagram Protocol [(UDP) hole punching](../reference/glossary/network-terms.md#udp-hole-punching)

Because of its high rate of failure, NAT punch-through is typically only used with a relay fallback.

### NAT Punch and Relay Fallback

This option combines NAT punching with a relay fallback. Clients first try to connect to the host by NAT punching and default back to the relay server on failure. This reduces the workload on the relay server while allowing clients to still connect to the host.

It is widely used because it reduces the hosting costs of relay servers.
