# Testing Client Connection Management

Managing client connections in a networked game can lead to many unexpected edge-cases which if not properly tested and handled may cause bugs. Here is a non-exhaustive list of test cases that should be handled, depending on what features a game provides, and things to look out for.

### Clients connecting
- test cases:
  - Client connecting to a new game session
  - Client connecting to a new game session after leaving a previous game session
    - in the case of a client-hosted game, after ending a previous game as a host
  - Client connecting to an ongoing game session (late-joining)
  - Client reconnecting to an ongoing game session (see [Session Management](../../advanced-topics/session-management.md#Reconnection))
  - Client failing to connect due to approval denied (see [Connection Approval](../../basics/connection-approval.md))
- things to look out for:
  - Client-Side:
    - Does the state of the client before connecting have an impact (that is, if connecting after disconnecting from another game or hosting one)
    - Does the game state get properly replicated from the server when connecting?
  - Server-Side:
    - Does the server properly handle reconnections or late-joining, if the game supports it, or does it deny approval if not?


### Clients disconnecting
- test cases:
  - Client disconnecting gracefully by shutting down NetworkManager
  - Client disconnecting by closing the application
  - Client timing out when losing connection to the host/server
    - By disabling internet on client
    - By disabling it on the host/server
- things to look out for:
  - Client-side:
    - Is the state of every object tied to the game session properly reset if not destroyed? (for example, if a NetworkBehaviour isn't destroyed when despawning, is its state properly reset via OnNetworkDespawn and OnNetworkSpawn?)
    - Is the client brought back to a state from which it can try to connect to a new game?
  - Server-Side:
    - Is the server notified of this disconnection and does it handle it properly?
    - If using outside services, are they notified of this? (for example if using a lobby service, is the client removed from the lobby?)

### Host / Server starting the session
- test cases:
  - Host/Server starting a new game session
  - Host/Server starting a new game session after shutting down a previous game session
    - in the case of a client-hosted game, after disconnecting from a preivious game as a client
- things to look out for:
  - Server-side:
    - Does the state of the application before starting a new session have an impact (that is, if starting after shutting down another game or disconnecting from one as a client)

### Host / Server shutting down
- test cases:
  - Host/Server disconnecting gracefully by shutting down NetworkManager
  - Host/Server disconnecting by closing the application
  - If requiring services (i.e Unity Game Services or other services) to function:
    - Host/Server timing out when losing connection to services
- things to look out for:
  - Client-side:
    - Are clients notified of this shut down, and do they handle it?
  - Server-side:
    - Are the services used notified of this? (for example if using a lobby service, does the game properly close the lobby when shutting down the game session?)
    - Is the state of every object tied to the game session properly reset if not destroyed? (for example, if a NetworkBehaviour isn't destroyed when despawning, is its state properly reset via OnNetworkDespawn and OnNetworkSpawn?)
