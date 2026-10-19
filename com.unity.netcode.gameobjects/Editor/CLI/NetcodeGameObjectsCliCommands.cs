using System;
using System.Threading.Tasks;
using Unity.Pipeline.Commands;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    // Netcode for GameObjects commands for the Unity CLI (com.unity.pipeline), run as `unity command <group> <name>`.
    // Each is a thin wrapper over NgoCliQueries, which holds the logic and its tests. The command name is the method
    // name in snake_case, so renaming a method renames its command. Parameter order and Required are wire API too:
    // add new parameters at the end, as optional. Commands that wait declare a double `timeout` parameter, which the
    // pipeline fills with the time left of the request.

    /// <summary>NetworkManager status, clients, spawned objects, prefabs and traffic: group <c>netcode.gameobjects</c>.</summary>
    internal static class NetcodeGameObjectsCliCommands
    {
        private const string k_Group = NgoCliQueries.Group;
        internal const string Tag = "networking/netcode";

        [CliGroupCommand(k_Group, "Each NetworkManager in the open scenes: role (host, server, client), connection state, local client id, topology, session owner, client and object counts, ticks, transport address and whether the session runs hybrid (Unified Netcode) prefabs. Outside play mode, the configuration only.", Tags = new[] { Tag })]
        private static object Status()
            => NgoCliOutput.ToWire(NgoCliQueries.GetStatus());

        [CliGroupCommand(k_Group, "Connected clients of the running NetworkManager: client id, local, server, session owner, player object, owned object count and round trip time. Requires a running session.", Tags = new[] { Tag })]
        private static object ListClients(
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.ListClients(manager));

        [CliGroupCommand(k_Group, "Spawned NetworkObjects of the running NetworkManager, as this peer sees them: network id, name, prefab id hash, owner, authority, player object, in-scene placed, hybrid, position. Requires a running session.", Tags = new[] { Tag })]
        private static object ListObjects(
            [CliArg("name_contains", "Only objects whose name contains this text.")] string nameContains = null,
            [CliArg("owner", "Only objects owned by this client id. -1 for all.")] long owner = -1,
            [CliArg("limit", "Maximum objects returned.")] int limit = 100,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.ListObjects(manager, nameContains, owner, limit));

        [CliGroupCommand(k_Group, "One spawned NetworkObject: owner, authority, ownership flags and who may change them, observers, NetworkBehaviours, scene and position. Requires a running session.", Tags = new[] { Tag })]
        private static object GetObject(
            [CliArg("id", "NetworkObjectId, from netcode.gameobjects list_objects.", Required = true)] long id,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.GetObject(id, manager));

        [CliGroupCommand(k_Group, "Instantiate and spawn a registered network prefab. Server or host in client-server; any client in distributed authority (which then owns it). Returns the spawned object.", Tags = new[] { Tag })]
        private static object Spawn(
            [CliArg("prefab", "Prefab path, GUID or name.", Required = true)] string prefab,
            [CliArg("position", "World position as [x, y, z]. Defaults to the prefab's.")] float[] position = null,
            [CliArg("owner", "Owning client id (client-server only). -1 for the server.")] long owner = -1,
            [CliArg("player_object", "Spawn it as the owner's player object.")] bool playerObject = false,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.Spawn(prefab, position, owner, playerObject, manager));

        [CliGroupCommand(k_Group, "Despawn a NetworkObject on every peer. Server or host in client-server; the owner in distributed authority.", Tags = new[] { Tag })]
        private static object Despawn(
            [CliArg("id", "NetworkObjectId, from netcode.gameobjects list_objects.", Required = true)] long id,
            [CliArg("destroy", "Destroy the GameObject too. False keeps it in the scene, unspawned.")] bool destroy = true,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.Despawn(id, destroy, manager));

        [CliGroupCommand(k_Group, "Give a NetworkObject to another client. Server or host in client-server; in distributed authority the owner, subject to the object's ownership flags (see netcode.gameobjects get_object).", Tags = new[] { Tag })]
        private static object ChangeOwner(
            [CliArg("id", "NetworkObjectId, from netcode.gameobjects list_objects.", Required = true)] long id,
            [CliArg("owner", "New owner's client id, from netcode.gameobjects list_clients.", Required = true)] long owner,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.ChangeOwner(id, owner, manager));

        [CliGroupCommand(k_Group, "Add a NetworkManager with a UnityTransport to the active scene, with the default network prefabs list attached. Undoable; saves nothing. Not allowed in play mode.", Tags = new[] { Tag })]
        private static object CreateNetworkManager(
            [CliArg("name", "GameObject name.")] string name = "NetworkManager",
            [CliArg("topology", "ClientServer or DistributedAuthority.")] NetworkTopologyTypes topology = NetworkTopologyTypes.ClientServer,
            [CliArg("address", "Address clients connect to.")] string address = "127.0.0.1",
            [CliArg("port", "Port the server listens on and clients connect to.")] int port = 7777,
            [CliArg("dry_run", "Validate and report without changing the scene.")] bool dryRun = false)
            => NgoCliOutput.ToWire(NgoCliQueries.CreateNetworkManager(name, topology, address, port, dryRun));

        [CliGroupCommand(k_Group, "Network prefabs lists in the project with their prefabs (path, prefab id hash, overrides, hybrid), the lists and player prefab each NetworkManager uses, and whether the default list is generated automatically.", Tags = new[] { Tag })]
        private static object ListPrefabs(
            [CliArg("list", "Only lists whose path contains this text.")] string list = null)
            => NgoCliOutput.ToWire(NgoCliQueries.ListPrefabs(list));

        [CliGroupCommand(k_Group, "Add a network prefab to a network prefabs list (the given one, else the scene's NetworkManager's, else the default). Reports when it's already registered. Not allowed in play mode.", Tags = new[] { Tag })]
        private static object RegisterPrefab(
            [CliArg("prefab", "Prefab path, GUID or name.", Required = true)] string prefab,
            [CliArg("list", "NetworkPrefabsList asset path.")] string list = null,
            [CliArg("dry_run", "Validate and report without changing the list.")] bool dryRun = false)
            => NgoCliOutput.ToWire(NgoCliQueries.RegisterPrefab(prefab, list, dryRun));

        [CliGroupCommand(k_Group, "Make a prefab a network prefab: add a NetworkObject to its root and report its prefab id hash and list registration. Saves the prefab. Not allowed in play mode.", Tags = new[] { Tag })]
        private static object MakePrefab(
            [CliArg("prefab", "Prefab path, GUID or name.", Required = true)] string prefab,
            [CliArg("dry_run", "Validate and report without changing the prefab.")] bool dryRun = false)
            => NgoCliOutput.ToWire(NgoCliQueries.MakePrefab(prefab, dryRun));

        [CliGroupCommand(k_Group, "Check the project for problems that otherwise show when a session starts: NetworkManager setups in the open scenes (transport, topology, prefab lists, player prefab, hierarchy, hybrid prefab requirements) and network prefabs (NetworkBehaviours without a NetworkObject, nested or misplaced NetworkObjects, NetworkManagers in network prefabs, missing or duplicate prefab ids, unregistered prefabs, broken list entries).", Tags = new[] { Tag })]
        private static object Audit(
            [CliArg("scope", "all, config (NetworkManagers) or prefabs.")] string scope = "all",
            [CliArg("folder", "Prefabs only: prefabs under this folder, e.g. Assets/Prefabs.")] string folder = null,
            [CliArg("limit", "Maximum issues returned (counts include all).")] int limit = 200,
            [CliArg("manager", "Config only: this NetworkManager (name or hierarchy path). Omit for all.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.Audit(scope, folder, limit, manager));

        /// <summary>How long a command may wait: stop a little before the request's timeout so the reply lands first.</summary>
        internal static float WaitSeconds(double timeoutInSeconds)
        {
            if (double.IsPositiveInfinity(timeoutInSeconds) || timeoutInSeconds > 600)
            {
                return 600;
            }
            var margin = Math.Min(2.0, timeoutInSeconds / 10);
            return (float)Math.Max(timeoutInSeconds - margin, 0.1);
        }
    }

    /// <summary>Network traffic, as the Network Profiler shows it: group <c>netcode.gameobjects.profiler</c>.</summary>
    internal static class NetcodeGameObjectsProfilerCliCommands
    {
        private const string k_Group = NgoCliQueries.Group + ".profiler";
        private const string k_Tag = NetcodeGameObjectsCliCommands.Tag;

        [CliGroupCommand(k_Group, "Sample network traffic over N frames: the profiler's network counters (messages, RPCs, NetworkVariables, spawns and bytes, with Multiplayer Tools installed), clients with round trip times, and the spawned object count. Requires a running session.", Tags = new[] { k_Tag })]
        private static async Task<object> GetStats(
            [CliArg("frames", "Frames to sample.")] int frames = 120,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null,
            [CliArg("timeout", "Stop sampling after this many seconds even if fewer frames ran.")] double timeoutInSeconds = 30)
        {
            var result = await NgoCliQueries.SampleStatsAsync(manager, frames, NetcodeGameObjectsCliCommands.WaitSeconds(timeoutInSeconds));
            return NgoCliOutput.ToWire(result);
        }
    }

    /// <summary>Starting, stopping and waiting on network sessions: group <c>netcode.gameobjects.session</c>.</summary>
    internal static class NetcodeGameObjectsSessionCliCommands
    {
        private const string k_Group = NgoCliQueries.Group + ".session";
        private const string k_Tag = NetcodeGameObjectsCliCommands.Tag;

        [CliGroupCommand(k_Group, "Start the NetworkManager as host, server or client, optionally overriding the UnityTransport address and port. Requires play mode (editor.playmode start). For a client, follow with netcode.gameobjects.session wait_ready.", Tags = new[] { k_Tag })]
        private static object Start(
            [CliArg("mode", "Host, Server or Client.", Required = true)] NgoCliQueries.SessionMode mode,
            [CliArg("address", "Address to connect to (client) or advertise. Defaults to the transport's.")] string address = null,
            [CliArg("port", "Port. -1 keeps the transport's.")] int port = -1,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.Start(mode, manager, address, port));

        [CliGroupCommand(k_Group, "Shut the NetworkManager down (any role). Clients disconnect; a host or server ends the session for everyone.", Tags = new[] { k_Tag })]
        private static object Stop(
            [CliArg("discard_message_queue", "Drop queued messages instead of sending them first.")] bool discardMessageQueue = false,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.Stop(manager, discardMessageQueue));

        [CliGroupCommand(k_Group, "Disconnect a client from the server or host, with an optional reason the client receives.", Tags = new[] { k_Tag })]
        private static object Kick(
            [CliArg("client_id", "Client id, from netcode.gameobjects list_clients.", Required = true)] long clientId,
            [CliArg("reason", "Disconnect reason sent to the client.")] string reason = null,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.Kick(clientId, reason, manager));

        [CliGroupCommand(k_Group, "Shut a client down and connect it again with the same settings, then wait until it's connected. Tests reconnection handling.", Tags = new[] { k_Tag })]
        private static async Task<object> Reconnect(
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null,
            [CliArg("timeout", "Seconds to wait for the reconnection.")] double timeoutInSeconds = 30)
        {
            var result = await NgoCliQueries.ReconnectAsync(manager, NetcodeGameObjectsCliCommands.WaitSeconds(timeoutInSeconds));
            return NgoCliOutput.ToWire(result);
        }

        [CliGroupCommand(k_Group, "Wait until the session is ready: a client connected and synchronized, or a server or host with at least min_clients remote clients. Returns ready false with the reason on timeout.", Tags = new[] { k_Tag })]
        private static async Task<object> WaitReady(
            [CliArg("min_clients", "Server or host: remote clients to wait for. Distributed authority: other clients in the session.")] int minClients = 0,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null,
            [CliArg("timeout", "Seconds to wait.")] double timeoutInSeconds = 30)
        {
            var result = await NgoCliQueries.WaitReadyAsync(manager, minClients, NetcodeGameObjectsCliCommands.WaitSeconds(timeoutInSeconds));
            return NgoCliOutput.ToWire(result);
        }
    }

    /// <summary>Simulated network conditions on UnityTransport: group <c>netcode.gameobjects.simulator</c>.</summary>
    internal static class NetcodeGameObjectsSimulatorCliCommands
    {
        private const string k_Group = NgoCliQueries.Group + ".simulator";
        private const string k_Tag = NetcodeGameObjectsCliCommands.Tag;

        [CliGroupCommand(k_Group, "The simulated network conditions (preset, delay, jitter, loss, timeout, lag spike), the transports they apply to, and any Multiplayer Tools Network Simulator components in the scene.", Tags = new[] { k_Tag })]
        private static object Get()
            => NgoCliOutput.ToWire(NgoCliQueries.GetSimulator());

        [CliGroupCommand(k_Group, "Simulate network conditions on UnityTransport: a preset and/or packet delay, jitter and loss. Applies live in play mode and to transports started later in this editor session.", Tags = new[] { k_Tag })]
        private static object Set(
            [CliArg("enable", "Turn the simulator on (true) or off (false).")] bool enable = true,
            [CliArg("preset", "Preset name from netcode.gameobjects.simulator list_presets (needs Multiplayer Tools).")] string preset = null,
            [CliArg("delay_ms", "Packet delay in milliseconds, 0-2000. -1 leaves it unchanged.")] int delayMs = -1,
            [CliArg("jitter_ms", "Packet jitter in milliseconds, 0-200. -1 leaves it unchanged.")] int jitterMs = -1,
            [CliArg("drop_percent", "Packet loss in percent, 0-100. -1 leaves it unchanged.")] int dropPercent = -1)
            => NgoCliOutput.ToWire(NgoCliQueries.SetSimulator(enable, preset, delayMs, jitterMs, dropPercent));

        [CliGroupCommand(k_Group, "Network condition presets (home broadband, mobile 2G to 5G…) with their delay, jitter and loss. They come from Multiplayer Tools.", Tags = new[] { k_Tag })]
        private static object ListPresets()
            => NgoCliOutput.ToWire(NgoCliQueries.ListSimulatorPresets());

        [CliGroupCommand(k_Group, "Drop every packet for a short time, then restore the simulator settings. Tests how gameplay copes with a lag spike. Requires a running session.", Tags = new[] { k_Tag })]
        private static object LagSpike(
            [CliArg("duration_ms", "Spike length in milliseconds, 10-120000.")] int durationMs = 500)
            => NgoCliOutput.ToWire(NgoCliQueries.StartLagSpike(durationMs));

        [CliGroupCommand(k_Group, "Drop every packet until turned off, so connections time out. Tests disconnect handling. Requires a running session to turn on.", Tags = new[] { k_Tag })]
        private static object Timeout(
            [CliArg("enable", "true to start dropping everything, false to stop.")] bool enable = true)
            => NgoCliOutput.ToWire(NgoCliQueries.SetTimeout(enable));
    }

    /// <summary>NetworkManager configuration: group <c>netcode.gameobjects.config</c>.</summary>
    internal static class NetcodeGameObjectsConfigCliCommands
    {
        private const string k_Group = NgoCliQueries.Group + ".config";
        private const string k_Tag = NetcodeGameObjectsCliCommands.Tag;

        [CliGroupCommand(k_Group, "The NetworkManager's configuration: NetworkConfig fields (tick rate, connection approval, scene management, topology…), transport address and port, log level, player prefab, prefab lists, and the settable field names.", Tags = new[] { k_Tag })]
        private static object Get(
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null)
            => NgoCliOutput.ToWire(NgoCliQueries.GetConfig(manager));

        [CliGroupCommand(k_Group, "Set one NetworkManager setting: a NetworkConfig field (snake_case, e.g. tick_rate), address, port, listen_address, player_prefab or log_level (Developer, Normal, Error, Nothing; applies live in play mode). Undoable in edit mode; saves nothing.", Tags = new[] { k_Tag })]
        private static object Set(
            [CliArg("field", "Field name, from settableFields of netcode.gameobjects.config get.", Required = true)] string field,
            [CliArg("value", "New value, e.g. 60, true, DistributedAuthority or a prefab path.", Required = true)] string value,
            [CliArg("manager", "NetworkManager name or hierarchy path, when there are several.")] string manager = null,
            [CliArg("dry_run", "Validate and report without changing anything.")] bool dryRun = false)
            => NgoCliOutput.ToWire(NgoCliQueries.SetConfig(field, value, manager, dryRun));
    }
}
