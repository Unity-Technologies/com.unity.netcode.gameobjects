# Network sessions and the NetworkManager

Intro

## Network sessions

A network session is a generic term for the period of time where two or more netcode applications have synchronized some subset of the [application state](application-state-network-session-state.md) between netcode application instance(s). There are three primary stages that define a network session:

- [Connection](#connection-stage) (network session time starts)
  - Establish and open a network connection, which can include some form of authentication process.
- [Synchronization](#synchronization-stage) (majority of network session time)
  - A subset of the application state is synchronized between netcode application instances, with baseline application state(s) synchronized during the first couple of seconds (or less) of synchronization.
- Disconnection (network session time stops)
  - Network connection between netcode applications is closed.

### Connection stage

The connection stage can involve several steps, depending on implementation, such as:

- Authenticating with a cloud service provider
- Searching for existing network sessions or creating a new one.
- If connecting over the internet, using a [relay](../relay/relay.md) or NAT punchthrough to establish a connection.

### Synchronization stage

The synchronization stage is where the majority of network session time is spent. During this stage, a subset of the application state is synchronized between netcode application instances. This can include:

