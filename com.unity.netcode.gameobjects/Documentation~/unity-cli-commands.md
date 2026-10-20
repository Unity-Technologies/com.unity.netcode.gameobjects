# Unity CLI commands

Drive Netcode for GameObjects from the command line, a CI job or an AI agent with the Unity CLI. The commands set up a NetworkManager and network prefabs, start and stop sessions, inspect connected clients and spawned objects, simulate poor network conditions, and sample network traffic.

The commands are available when the project has the `com.unity.pipeline` package (version 0.9.0-exp.1 or later). Without it, nothing extra is compiled. Run them against an open Editor as `unity command <group> <name> [arguments]`; `unity command --list` shows every command with its arguments.

## Typical workflow

```
unity command netcode.gameobjects create_network_manager
unity command netcode.gameobjects make_prefab --prefab Assets/Prefabs/Player.prefab
unity command netcode.gameobjects.config set --field player_prefab --value Assets/Prefabs/Player.prefab
unity command editor.playmode start
unity command netcode.gameobjects.session start --mode Host
unity command netcode.gameobjects.session wait_ready
unity command netcode.gameobjects list_objects
```

To test with several players, use [Multiplayer Play Mode](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@latest) scenarios (`editor.playmode start_scenario`) and run the commands against each player's Editor with `unity command --project-path <player project path>`.

## Commands

### netcode.gameobjects

| **Command** | **Description** |
| :--- | :--- |
| `status` | Each NetworkManager: role (host, server, client), connection state, local client id, topology, session owner, client and object counts, ticks, transport address, and whether the session runs hybrid prefabs. |
| `list_clients` | Connected clients with their player object, owned object count and round trip time. |
| `list_objects` | Spawned NetworkObjects as this peer sees them, filtered by name or owner. |
| `get_object` | One NetworkObject: owner, authority, ownership flags and who can change them, observers and NetworkBehaviours. |
| `spawn` | Instantiate and spawn a registered network prefab at a position, optionally for an owner or as a player object. |
| `despawn` | Despawn a NetworkObject, optionally keeping the GameObject. |
| `change_owner` | Give a NetworkObject to another client, following the topology's ownership rules. |
| `create_network_manager` | Add a NetworkManager with a UnityTransport to the active scene. |
| `list_prefabs` | Network prefabs lists with their prefabs, and the lists and player prefab each NetworkManager uses. |
| `register_prefab` | Add a network prefab to a network prefabs list. |
| `make_prefab` | Add a NetworkObject to a prefab's root and report its prefab id hash. |
| `audit` | Find setup problems that otherwise show when a session starts: in NetworkManagers (transport, topology, prefab lists, player prefab, hybrid prefab requirements) and in network prefabs (NetworkBehaviours without a NetworkObject, nested NetworkObjects, duplicate prefab ids, unregistered prefabs). `--scope config` or `--scope prefabs` runs one half. |

### netcode.gameobjects.profiler

| **Command** | **Description** |
| :--- | :--- |
| `get_stats` | Sample network traffic over a number of frames. Message and byte counters need the Multiplayer Tools package. |

### netcode.gameobjects.session

| **Command** | **Description** |
| :--- | :--- |
| `start` | Start the NetworkManager as host, server or client, optionally with another address and port. |
| `stop` | Shut the NetworkManager down. |
| `kick` | Disconnect a client from the server or host. |
| `reconnect` | Disconnect a client and connect it again, then wait for it to be connected. |
| `wait_ready` | Wait until a client is connected, or a server has a number of clients. |

### netcode.gameobjects.simulator

The simulator applies packet delay, jitter and loss to UnityTransport through its simulator pipeline stage, which is available in the Editor and development builds. Settings last for the Editor session and also apply to sessions started later.

| **Command** | **Description** |
| :--- | :--- |
| `get` | The current simulated conditions. |
| `set` | Set a preset and/or packet delay, jitter and loss. |
| `list_presets` | Network condition presets. They come from the Multiplayer Tools package. |
| `lag_spike` | Drop every packet for a short time. |
| `timeout` | Drop every packet until turned off, so connections time out. |

### netcode.gameobjects.config

| **Command** | **Description** |
| :--- | :--- |
| `get` | The NetworkManager's configuration and the names `set` accepts. |
| `set` | Set a NetworkConfig field (for example `tick_rate`), the transport `address`, `port` or `listen_address`, the `player_prefab`, or the `log_level`. The log level also changes a running session. |

Commands that change scenes or assets are undoable where Unity supports it and don't save the scene; save it with `unity command scene save`. Commands that change a running session apply to the Editor they run in.
