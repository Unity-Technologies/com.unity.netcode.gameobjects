using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    /// <summary>
    /// Read and write operations behind the Netcode for GameObjects CLI commands (group <c>netcode.gameobjects</c>).
    /// The commands live in the Unity.Netcode.GameObjects.CLI.Editor assembly, which only compiles when
    /// com.unity.pipeline is installed; keeping the logic here means the package's tests exercise it without the
    /// pipeline. Errors are thrown as <see cref="InvalidOperationException"/> or <see cref="ArgumentException"/>
    /// with a message that names the next step to take.
    /// </summary>
    internal static partial class NgoCliQueries
    {
        internal const string Group = "netcode.gameobjects";

        internal const string NoManagerMessage =
            "No NetworkManager in the open scenes. Create one with netcode.gameobjects create_network_manager, or open the scene that has it (scene open).";

        internal const string NotPlayingMessage =
            "Requires play mode. Enter it with editor.playmode start, then start the session with netcode.gameobjects.session start.";

        internal const string NotListeningMessage =
            "The NetworkManager isn't running. Start it with netcode.gameobjects.session start (mode host, server or client).";

        // --- Status ---

        internal static StatusResult GetStatus()
        {
            var result = new StatusResult { IsPlaying = EditorApplication.isPlaying };
            foreach (var manager in FindManagers())
            {
                result.Managers.Add(DescribeManager(manager));
            }

            if (result.Managers.Count == 0)
            {
                result.Hint = NoManagerMessage;
            }
            else if (!result.IsPlaying)
            {
                result.Hint = "Not in play mode: only the configuration is reported. Enter play mode (editor.playmode start), then netcode.gameobjects.session start.";
            }
            else if (result.Managers.All(m => !m.IsListening))
            {
                result.Hint = NotListeningMessage;
            }
            return result;
        }

        internal static ManagerStatus DescribeManager(NetworkManager manager)
        {
            var config = manager.NetworkConfig;
            var status = new ManagerStatus
            {
                Name = manager.name,
                Path = GetHierarchyPath(manager.transform),
                Scene = manager.gameObject.scene.name,
                IsSingleton = NetworkManager.Singleton == manager,
                TickRate = config?.TickRate ?? 0,
                Topology = config?.NetworkTopology.ToString(),
                LogLevel = manager.LogLevel.ToString(),
                Transport = DescribeTransport(config?.NetworkTransport),
                UnifiedNetcode = DescribeUnifiedNetcode(manager),
                Role = "none",
            };

            if (!EditorApplication.isPlaying)
            {
                return status;
            }

            status.IsListening = manager.IsListening;
            status.ShuttingDown = manager.ShutdownInProgress;
            status.DisconnectReason = string.IsNullOrEmpty(manager.DisconnectReason) ? null : manager.DisconnectReason;
            if (!manager.IsListening)
            {
                return status;
            }

            status.Role = GetRole(manager);
            status.IsConnectedClient = manager.IsConnectedClient;
            status.IsApproved = manager.IsApproved;
            status.LocalClientId = (long)manager.LocalClientId;
            status.DistributedAuthority = manager.DistributedAuthorityMode;
            if (manager.DistributedAuthorityMode)
            {
                status.CurrentSessionOwner = (long)manager.CurrentSessionOwner;
                status.IsSessionOwner = manager.LocalClient != null && manager.LocalClient.IsSessionOwner;
            }
            status.ConnectedClientCount = manager.ConnectedClientsIds?.Count;
            status.SpawnedObjectCount = manager.SpawnManager?.SpawnedObjects.Count;
            if (manager.NetworkTickSystem != null)
            {
                status.ServerTick = manager.ServerTime.Tick;
                status.LocalTick = manager.LocalTime.Tick;
            }
            return status;
        }

        private static TransportSummary DescribeTransport(NetworkTransport transport)
        {
            if (transport == null)
            {
                return null;
            }

            var summary = new TransportSummary { Type = transport.GetType().Name };
            if (transport is UnityTransport utp)
            {
                summary.Protocol = utp.Protocol.ToString();
                summary.Address = utp.ConnectionData.Address;
                summary.Port = utp.ConnectionData.Port;
                summary.ListenAddress = string.IsNullOrEmpty(utp.ConnectionData.ServerListenAddress) ? null : utp.ConnectionData.ServerListenAddress;
            }
            return summary;
        }

        private static UnifiedNetcodeSummary DescribeUnifiedNetcode(NetworkManager manager)
        {
#if UNIFIED_NETCODE
            var summary = new UnifiedNetcodeSummary { Available = true };
            // A session runs on a Netcode for Entities world only when its prefabs include hybrid (GhostObject) ones.
            if (EditorApplication.isPlaying && manager.IsListening)
            {
                summary.HybridSession = manager.NetworkConfig?.Prefabs != null && manager.NetworkConfig.Prefabs.HasGhostPrefabs;
            }
            return summary;
#else
            return new UnifiedNetcodeSummary { Available = false };
#endif
        }

        internal static string GetRole(NetworkManager manager)
        {
            if (!manager.IsListening)
            {
                return "none";
            }
            if (manager.IsHost)
            {
                return "host";
            }
            if (manager.IsServer)
            {
                return "server";
            }
            return manager.IsClient ? "client" : "none";
        }

        // --- Clients ---

        internal static ClientListResult ListClients(string managerFilter) => ListClients(ResolveRunningManager(managerFilter));

        private static ClientListResult ListClients(NetworkManager manager)
        {
            var result = new ClientListResult { Manager = manager.name, Role = GetRole(manager) };
            foreach (var clientId in manager.ConnectedClientsIds)
            {
                manager.ConnectedClients.TryGetValue(clientId, out var client);
                var info = new ClientInfo
                {
                    ClientId = (long)clientId,
                    IsLocal = clientId == manager.LocalClientId,
                    IsServer = clientId == NetworkManager.ServerClientId && !manager.DistributedAuthorityMode,
                    IsSessionOwner = manager.DistributedAuthorityMode && clientId == manager.CurrentSessionOwner,
                    RttMs = TryGetRtt(manager, clientId),
                };
                if (client != null)
                {
                    info.PlayerObjectId = client.PlayerObject != null ? (long)client.PlayerObject.NetworkObjectId : null;
                    info.OwnedObjectCount = manager.SpawnManager?.GetClientOwnedObjects(clientId).Length;
                }
                result.Clients.Add(info);
            }

            if (!manager.IsServer)
            {
                result.Note = "Read on a client: player objects and owned objects only include what this client can see. Run the command on the server or host for the full picture.";
            }
            return result;
        }

        private static long? TryGetRtt(NetworkManager manager, ulong clientId)
        {
            var transport = manager.NetworkConfig?.NetworkTransport;
            if (transport == null || clientId == manager.LocalClientId)
            {
                return null;
            }
            // A client only knows its round trip to the server; the transport takes the server id there.
            if (!manager.IsServer && clientId != NetworkManager.ServerClientId)
            {
                return null;
            }
            try
            {
                return (long)transport.GetCurrentRtt(manager.IsServer ? clientId : NetworkManager.ServerClientId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // --- Objects ---

        internal static ObjectListResult ListObjects(string managerFilter, string nameContains, long owner, int limit)
        {
            if (limit < 1)
            {
                throw new ArgumentException("limit must be at least 1.");
            }
            var manager = ResolveRunningManager(managerFilter);
            var objects = manager.SpawnManager.SpawnedObjects.Values
                .Where(o => o != null)
                .Where(o => string.IsNullOrEmpty(nameContains) || o.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(o => owner < 0 || o.OwnerClientId == (ulong)owner)
                .OrderBy(o => o.NetworkObjectId)
                .ToList();

            var result = new ObjectListResult
            {
                Manager = manager.name,
                Role = GetRole(manager),
                Total = objects.Count,
            };
            foreach (var networkObject in objects.Take(limit))
            {
                result.Objects.Add(Summarize(networkObject, new ObjectSummary()));
            }
            result.Returned = result.Objects.Count;
            return result;
        }

        internal static ObjectDetail GetObject(long networkObjectId, string managerFilter)
        {
            var manager = ResolveRunningManager(managerFilter);
            var networkObject = FindSpawnedObject(manager, networkObjectId);
            return Describe(manager, networkObject);
        }

        internal static ObjectDetail Describe(NetworkManager manager, NetworkObject networkObject)
        {
            var detail = Summarize(networkObject, new ObjectDetail());
            detail.Manager = manager.name;
            detail.Scene = networkObject.gameObject.scene.name;
            detail.ActiveInHierarchy = networkObject.gameObject.activeInHierarchy;
            detail.DestroyWithScene = networkObject.DestroyWithScene;
            detail.DontDestroyWithOwner = networkObject.DontDestroyWithOwner;
            detail.Ownership = DescribeOwnership(manager, networkObject);
            foreach (var behaviour in networkObject.GetComponentsInChildren<NetworkBehaviour>(true))
            {
                // As NGO assigns them: a nested NetworkObject owns the behaviours below it.
                if (behaviour.GetComponentInParent<NetworkObject>(true) == networkObject)
                {
                    detail.Behaviours.Add(behaviour.GetType().Name);
                }
            }

            // Observers are only tracked by the authority that decides visibility.
            if (manager.IsServer || manager.DistributedAuthorityMode)
            {
                detail.Observers = new List<long>();
                var observers = networkObject.GetObservers();
                while (observers.MoveNext())
                {
                    detail.Observers.Add((long)observers.Current);
                }
                detail.Observers.Sort();
            }
            return detail;
        }

        private static T Summarize<T>(NetworkObject networkObject, T summary) where T : ObjectSummary
        {
            var position = networkObject.transform.position;
            summary.NetworkObjectId = (long)networkObject.NetworkObjectId;
            summary.Name = networkObject.name;
            summary.PrefabIdHash = networkObject.PrefabIdHash;
            summary.OwnerClientId = (long)networkObject.OwnerClientId;
            summary.IsOwner = networkObject.IsOwner;
            summary.HasAuthority = networkObject.HasAuthority;
            summary.IsPlayerObject = networkObject.IsPlayerObject;
            summary.InScenePlaced = networkObject.InScenePlaced;
            summary.Hybrid = IsHybrid(networkObject);
            summary.Position = new[] { position.x, position.y, position.z };
            return summary;
        }

        private static OwnershipInfo DescribeOwnership(NetworkManager manager, NetworkObject networkObject)
        {
            var info = new OwnershipInfo
            {
                Transferable = networkObject.IsOwnershipTransferable,
                Distributable = networkObject.IsOwnershipDistributable,
                SessionOwnerOnly = networkObject.IsOwnershipSessionOwner,
                RequestRequired = networkObject.IsOwnershipRequestRequired,
                Locked = networkObject.IsOwnershipLocked,
                RequestInProgress = networkObject.IsRequestInProgress,
                WhoCanChange = manager.DistributedAuthorityMode
                    ? "The current owner (or the session owner for session-owner objects), subject to the ownership flags."
                    : "The server or host.",
            };
            foreach (NetworkObject.OwnershipStatus flag in Enum.GetValues(typeof(NetworkObject.OwnershipStatus)))
            {
                if (flag != NetworkObject.OwnershipStatus.None && flag != NetworkObject.OwnershipStatus.All && networkObject.Ownership.HasFlag(flag))
                {
                    info.Status.Add(flag.ToString());
                }
            }
            return info;
        }

        private static bool IsHybrid(NetworkObject networkObject)
        {
#if UNIFIED_NETCODE
            return networkObject.HasGhost;
#else
            return false;
#endif
        }

        internal static NetworkObject FindSpawnedObject(NetworkManager manager, long networkObjectId)
        {
            if (networkObjectId < 0)
            {
                throw new ArgumentException("id must be a NetworkObjectId (0 or more). List them with netcode.gameobjects list_objects.");
            }
            if (!manager.SpawnManager.SpawnedObjects.TryGetValue((ulong)networkObjectId, out var networkObject) || networkObject == null)
            {
                throw new ArgumentException($"No spawned NetworkObject with id {networkObjectId} on {manager.name}. List them with netcode.gameobjects list_objects.");
            }
            return networkObject;
        }

        // --- NetworkManager resolution ---

        /// <summary>The NetworkManagers in the open scenes, including DontDestroyOnLoad ones in play mode.</summary>
        internal static List<NetworkManager> FindManagers()
        {
            return FindObjects.ByType<NetworkManager>(includeInactive: true)
                .Where(m => m != null && !EditorUtility.IsPersistent(m))
                .OrderBy(m => m.name, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Picks the NetworkManager a command targets: the one named <paramref name="filter"/> (or with that
        /// hierarchy path), else the one whose hierarchy path contains it, else the only one, else the running singleton.
        /// </summary>
        internal static NetworkManager ResolveManager(string filter, List<NetworkManager> managers = null)
        {
            managers ??= FindManagers();
            if (managers.Count == 0)
            {
                throw new InvalidOperationException(NoManagerMessage);
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var trimmed = filter.Trim();
                var paths = managers.Select(m => (manager: m, path: GetHierarchyPath(m.transform))).ToList();
                // An exact name or hierarchy path wins over a partial match: "Server" picks Server, not ServerBrowser.
                var matches = paths.Where(p =>
                    string.Equals(p.manager.name, trimmed, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.path, trimmed, StringComparison.OrdinalIgnoreCase)).Select(p => p.manager).ToList();
                if (matches.Count == 0)
                {
                    matches = paths.Where(p => p.path.IndexOf(trimmed, StringComparison.OrdinalIgnoreCase) >= 0).Select(p => p.manager).ToList();
                }
                if (matches.Count == 1)
                {
                    return matches[0];
                }
                if (matches.Count == 0)
                {
                    throw new ArgumentException($"No NetworkManager matches '{filter}'. NetworkManagers: {string.Join(", ", managers.Select(m => GetHierarchyPath(m.transform)))}.");
                }
                managers = matches;
            }

            if (managers.Count == 1)
            {
                return managers[0];
            }
            var singleton = NetworkManager.Singleton;
            if (singleton != null && managers.Contains(singleton))
            {
                return singleton;
            }
            throw new ArgumentException($"Several NetworkManagers. Pass manager with one of: {string.Join(", ", managers.Select(m => GetHierarchyPath(m.transform)))}.");
        }

        // A scene's name for messages; an unsaved scene has none.
        internal static string SceneLabel(UnityEngine.SceneManagement.Scene scene) => string.IsNullOrEmpty(scene.name) ? "an unsaved scene" : scene.name;

        internal static NetworkManager ResolveRunningManager(string filter)
        {
            // Report a missing NetworkManager first: entering play mode wouldn't help.
            var manager = ResolveManager(filter);
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(NotPlayingMessage);
            }
            if (!manager.IsListening)
            {
                throw new InvalidOperationException(NotListeningMessage);
            }
            return manager;
        }

        /// <summary>
        /// Sets a UnityTransport's address, port and listen address. Unlike UnityTransport.SetConnectionData, this
        /// keeps the protocol (Relay or direct) and the WebSocket path, and leaves an empty listen address empty.
        /// </summary>
        internal static void SetConnectionAddress(UnityTransport utp, string address, ushort port, string listenAddress)
        {
            var data = utp.ConnectionData;
            data.Address = address;
            data.Port = port;
            data.ServerListenAddress = listenAddress ?? string.Empty;
            utp.ConnectionData = data;
        }

        /// <summary>
        /// The listen address for a server that clients reach at <paramref name="address"/>: empty (loopback) for a
        /// loopback address, otherwise every interface, so clients on other machines can connect.
        /// </summary>
        internal static string DefaultListenAddress(string address)
        {
            if (string.Equals(address, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
            if (IPAddress.TryParse(address, out var ip))
            {
                return IPAddress.IsLoopback(ip) ? string.Empty : ip.AddressFamily == AddressFamily.InterNetworkV6 ? "::" : "0.0.0.0";
            }
            return "0.0.0.0";
        }

        /// <summary>Throws when the transport connects through Relay, which ignores the address and port.</summary>
        internal static void RequireDirectConnection(UnityTransport utp, string setting)
        {
            if (utp.Protocol == UnityTransport.ProtocolType.RelayUnityTransport)
            {
                throw new InvalidOperationException($"{utp.name}'s UnityTransport connects through Relay, which ignores {setting}: the server comes from the Relay allocation (SetRelayServerData). Set the transport's protocol to UnityTransport to connect directly.");
            }
        }

        internal static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }
            return path;
        }

        internal static void RequireDefined<T>(T value, string argument) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                throw new ArgumentException($"{argument} must be one of: {string.Join(", ", Enum.GetNames(typeof(T)))}.");
            }
        }
    }
}
