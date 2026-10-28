using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    internal static partial class NgoCliQueries
    {
        internal enum SessionMode
        {
            Host,
            Server,
            Client,
        }

        // --- Start / stop ---

        /// <summary>
        /// Starts the NetworkManager as host, server or client, optionally pointing its UnityTransport at another
        /// address and port first. Returns once the start call returns; use <see cref="WaitReadyAsync"/> to wait
        /// for the connection.
        /// </summary>
        // The role each NetworkManager last started as through the CLI: once it has stopped, it no longer says.
        private static readonly Dictionary<NetworkManager, SessionMode> s_StartedAs = new Dictionary<NetworkManager, SessionMode>();

        internal static SessionResult Start(SessionMode mode, string managerFilter, string address, int port)
        {
            RequireDefined(mode, "mode");
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Requires play mode. Enter it with editor.playmode start first.");
            }
            if (port > ushort.MaxValue)
            {
                throw new ArgumentException($"port must be between 1 and {ushort.MaxValue} (or -1 to keep the configured port).");
            }

            var manager = ResolveManager(managerFilter);
            if (manager.IsListening || manager.ShutdownInProgress)
            {
                throw new InvalidOperationException($"{manager.name} is already running as {GetRole(manager)}. Stop it first with netcode.gameobjects.session stop.");
            }

            var result = new SessionResult { Action = "start" };
            if ((!string.IsNullOrWhiteSpace(address) || port > 0) && IsHybridSession(manager))
            {
                result.Notes.Add(HybridSessionNote(manager, "the address and port weren't applied"));
            }
            else if (!string.IsNullOrWhiteSpace(address) || port > 0)
            {
                if (manager.NetworkConfig.NetworkTransport is not UnityTransport utp)
                {
                    throw new ArgumentException("address and port can only be set on a UnityTransport. Configure your transport directly.");
                }
                RequireDirectConnection(utp, "address and port");
                var data = utp.ConnectionData;
                var connectAddress = string.IsNullOrWhiteSpace(address) ? data.Address : address.Trim();
                var listen = string.IsNullOrWhiteSpace(address) ? data.ServerListenAddress ?? string.Empty : ListenAddressFor(data.Address, data.ServerListenAddress, connectAddress);
                SetConnectionAddress(utp, connectAddress, port > 0 ? (ushort)port : data.Port, listen);
                result.Notes.Add($"Transport set to {utp.ConnectionData.Address}:{utp.ConnectionData.Port} for this session.");
                if (mode != SessionMode.Client && listen != (data.ServerListenAddress ?? string.Empty))
                {
                    result.Notes.Add(DescribeListenAddress(listen));
                }
            }

            bool started;
            using (var errors = new LogCapture())
            {
                started = mode switch
                {
                    SessionMode.Host => manager.StartHost(),
                    SessionMode.Server => manager.StartServer(),
                    _ => manager.StartClient(),
                };
                if (!started)
                {
                    throw new InvalidOperationException($"{manager.name} failed to start as {mode.ToString().ToLowerInvariant()}{errors.Describe()}");
                }
            }
            s_StartedAs[manager] = mode;

            result.Status = DescribeManager(manager);
            if (mode == SessionMode.Client)
            {
                result.Notes.Add("The client is connecting. Wait for it with netcode.gameobjects.session wait_ready.");
            }
            return result;
        }

        internal static SessionResult Stop(string managerFilter, bool discardMessageQueue)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(NotPlayingMessage);
            }
            var manager = ResolveManager(managerFilter);
            var result = new SessionResult { Action = "stop" };
            if (!manager.IsListening && !manager.ShutdownInProgress)
            {
                result.Notes.Add($"{manager.name} wasn't running.");
            }
            else
            {
                manager.Shutdown(discardMessageQueue);
                result.Notes.Add("Shutdown finishes on the next network update; netcode.gameobjects status shows when it's done.");
            }
            result.Status = DescribeManager(manager);
            return result;
        }

        /// <summary>Disconnects a remote client from the server or host.</summary>
        internal static SessionResult Kick(long clientId, string reason, string managerFilter)
        {
            var manager = ResolveRunningManager(managerFilter);
            if (!manager.IsServer)
            {
                throw new InvalidOperationException($"{manager.name} is a {GetRole(manager)}; only the server or host can disconnect other clients. Run the command against the server's editor, or stop this client with netcode.gameobjects.session stop.");
            }
            if (clientId < 0 || !IsConnected(manager, (ulong)clientId))
            {
                throw new ArgumentException($"No connected client {clientId}. Connected: {string.Join(", ", manager.ConnectedClientsIds)}. List them with netcode.gameobjects list_clients.");
            }
            if ((ulong)clientId == manager.LocalClientId)
            {
                throw new ArgumentException("That's the host's own client. Stop the host with netcode.gameobjects.session stop instead.");
            }

            manager.DisconnectClient((ulong)clientId, string.IsNullOrWhiteSpace(reason) ? null : reason);
            var result = new SessionResult { Action = "kick", Status = DescribeManager(manager) };
            result.Notes.Add($"Client {clientId} disconnected. Its objects are despawned or redistributed according to DontDestroyWithOwner.");
            return result;
        }

        /// <summary>
        /// Shuts a client down and starts it again with the same transport settings, then waits for it to be
        /// connected and approved.
        /// </summary>
        internal static async Task<WaitReadyResult> ReconnectAsync(string managerFilter, float timeoutSeconds)
        {
            ValidateTimeout(timeoutSeconds);
            var manager = ResolveManager(managerFilter);
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(NotPlayingMessage);
            }
            if (manager.IsServer)
            {
                throw new InvalidOperationException($"{manager.name} is the {GetRole(manager)}. reconnect is for clients; restart a server with netcode.gameobjects.session stop, then start.");
            }
            // A stopped host or server no longer reports IsServer, and would otherwise start again as a client.
            if (!manager.IsListening && s_StartedAs.TryGetValue(manager, out var startedAs) && startedAs != SessionMode.Client)
            {
                throw new InvalidOperationException($"{manager.name} was stopped as the {startedAs.ToString().ToLowerInvariant()}. reconnect is for clients; start it again with netcode.gameobjects.session start --mode {startedAs}.");
            }

            var started = EditorApplication.timeSinceStartup;
            // A client that was kicked or timed out has already shut down; connect it again as it is.
            if (manager.IsListening || manager.ShutdownInProgress)
            {
                manager.Shutdown();
                await WaitUntilAsync(() => !manager.IsListening && !manager.ShutdownInProgress, timeoutSeconds, "the client to shut down");
            }

            using (var errors = new LogCapture())
            {
                if (!manager.StartClient())
                {
                    throw new InvalidOperationException($"{manager.name} shut down but failed to start again{errors.Describe()}");
                }
            }
            s_StartedAs[manager] = SessionMode.Client;

            var remaining = timeoutSeconds - (float)(EditorApplication.timeSinceStartup - started);
            var ready = await WaitReadyAsync(manager, Math.Max(remaining, 0.1f), 0);
            ready.WaitedSeconds = EditorApplication.timeSinceStartup - started;
            return ready;
        }

        /// <summary>
        /// Waits until the manager is ready: a client connected and approved, or a server or host listening with
        /// at least <paramref name="minClients"/> remote clients.
        /// </summary>
        internal static Task<WaitReadyResult> WaitReadyAsync(string managerFilter, int minClients, float timeoutSeconds)
        {
            ValidateTimeout(timeoutSeconds);
            if (minClients < 0)
            {
                throw new ArgumentException("min_clients must be 0 or more.");
            }
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(NotPlayingMessage);
            }
            return WaitReadyAsync(ResolveManager(managerFilter), timeoutSeconds, minClients);
        }

        private static Task<WaitReadyResult> WaitReadyAsync(NetworkManager manager, float timeoutSeconds, int minClients)
        {
            var completion = new TaskCompletionSource<WaitReadyResult>();
            var start = EditorApplication.timeSinceStartup;
            var deadline = start + timeoutSeconds;

            WaitReadyResult Build(bool ready, bool timedOut, string reason) => new WaitReadyResult
            {
                Ready = ready,
                TimedOut = timedOut,
                Reason = reason,
                WaitedSeconds = EditorApplication.timeSinceStartup - start,
                Status = manager != null ? DescribeManager(manager) : null,
            };

            void Tick()
            {
                try
                {
                    if (!EditorApplication.isPlaying || manager == null)
                    {
                        EditorApplication.update -= Tick;
                        completion.TrySetResult(Build(false, false, "Play mode stopped."));
                        return;
                    }

                    var reason = ReadyBlocker(manager, minClients);
                    if (reason == null)
                    {
                        EditorApplication.update -= Tick;
                        completion.TrySetResult(Build(true, false, null));
                    }
                    else if (EditorApplication.timeSinceStartup >= deadline)
                    {
                        EditorApplication.update -= Tick;
                        completion.TrySetResult(Build(false, true, reason));
                    }
                }
                catch (Exception e)
                {
                    EditorApplication.update -= Tick;
                    completion.TrySetException(e);
                }
            }

            EditorApplication.update += Tick;
            return completion.Task;
        }

        /// <summary>Why the manager isn't ready yet, or null when it is.</summary>
        private static string ReadyBlocker(NetworkManager manager, int minClients)
        {
            if (manager.ShutdownInProgress)
            {
                return "Shutting down.";
            }
            if (!manager.IsListening)
            {
                return string.IsNullOrEmpty(manager.DisconnectReason)
                    ? "Not running. Start it with netcode.gameobjects.session start."
                    : $"Not running. Last disconnect reason: {manager.DisconnectReason}";
            }
            if (manager.IsServer)
            {
                var remote = CountOtherClients(manager);
                return remote >= minClients ? null : $"{remote} of {minClients} remote client(s) connected.";
            }
            if (!manager.IsConnectedClient)
            {
                return manager.IsApproved ? "Approved, synchronizing." : "Connecting.";
            }
            if (manager.DistributedAuthorityMode && minClients > 0)
            {
                var others = CountOtherClients(manager);
                return others >= minClients ? null : $"{others} of {minClients} other client(s) in the session.";
            }
            return null;
        }

        // --- Spawn / despawn / ownership ---

        internal static SpawnResult Spawn(string prefab, float[] position, long owner, bool playerObject, string managerFilter)
        {
            var manager = ResolveRunningManager(managerFilter);
            var prefabAsset = ResolvePrefab(prefab);
            if (prefabAsset.GetComponent<NetworkObject>() == null)
            {
                throw new ArgumentException($"{AssetDatabase.GetAssetPath(prefabAsset)} has no NetworkObject on its root. Add one with netcode.gameobjects make_prefab.");
            }
            if (position != null && position.Length != 3)
            {
                throw new ArgumentException("position must be three numbers, e.g. [0, 1, 0].");
            }
            if (!manager.DistributedAuthorityMode && !manager.IsServer)
            {
                throw new InvalidOperationException($"{manager.name} is a client. With a client-server topology only the server or host spawns; run the command against the server's editor.");
            }
            if (manager.DistributedAuthorityMode && !manager.IsConnectedClient)
            {
                throw new InvalidOperationException($"{manager.name} hasn't finished connecting to the session, so it can't spawn yet. Wait for it with netcode.gameobjects.session wait_ready.");
            }

            var result = new SpawnResult { Manager = manager.name, Prefab = AssetDatabase.GetAssetPath(prefabAsset) };
            var ownerId = manager.DistributedAuthorityMode ? manager.LocalClientId : owner < 0 ? NetworkManager.ServerClientId : (ulong)owner;
            if (manager.DistributedAuthorityMode && owner >= 0 && (ulong)owner != manager.LocalClientId)
            {
                result.Notes.Add($"Distributed authority spawns are owned by the spawning client; owner {owner} is ignored. Transfer it afterwards with netcode.gameobjects change_owner.");
            }
            else if (owner >= 0 && !IsValidOwner(manager, ownerId))
            {
                throw new ArgumentException($"No connected client {owner}. List them with netcode.gameobjects list_clients.");
            }

            var spawnPosition = position != null ? new Vector3(position[0], position[1], position[2]) : prefabAsset.transform.position;
            NetworkObject spawned;
            using (var errors = new LogCapture())
            {
                // Spawn errors are logged rather than thrown; report them as the command's error.
                spawned = NetworkObject.InstantiateAndSpawn(prefabAsset, manager, ownerId, isPlayerObject: playerObject, position: spawnPosition, rotation: prefabAsset.transform.rotation);
                if (spawned == null || !spawned.IsSpawned)
                {
                    // NGO instantiates before it spawns, so a failed spawn can leave an instance behind.
                    if (spawned != null)
                    {
                        DestroyUnspawned(manager, spawned);
                    }
                    throw new InvalidOperationException($"Spawning {prefabAsset.name} failed{errors.Describe()} Is it registered? Check with netcode.gameobjects list_prefabs.");
                }
            }
            result.Object = Describe(manager, spawned);
            return result;
        }

        // Destroys an instance that never spawned the way NGO destroys a despawned one: through its prefab handler if
        // it has one, else directly.
        private static void DestroyUnspawned(NetworkManager manager, NetworkObject instance)
        {
            if (manager.PrefabHandler.ContainsHandler(instance))
            {
                manager.PrefabHandler.HandleNetworkPrefabDestroy(instance);
            }
            else
            {
                UnityEngine.Object.Destroy(instance.gameObject);
            }
        }

        internal static DespawnResult Despawn(long networkObjectId, bool destroy, string managerFilter)
        {
            var manager = ResolveRunningManager(managerFilter);
            var networkObject = FindSpawnedObject(manager, networkObjectId);
            if (!networkObject.HasAuthority)
            {
                throw new InvalidOperationException(manager.DistributedAuthorityMode
                    ? $"{networkObject.name} is owned by client {networkObject.OwnerClientId}; in distributed authority only its owner can despawn it. Run the command against that client's editor."
                    : $"{manager.name} is a client; only the server or host can despawn. Run the command against the server's editor.");
            }

            var result = new DespawnResult { Manager = manager.name, NetworkObjectId = networkObjectId, Name = networkObject.name };
            using (var errors = new LogCapture())
            {
                try
                {
                    networkObject.Despawn(destroy);
                }
                catch (NotSupportedException e)
                {
                    throw new InvalidOperationException($"{networkObject.name} can't be despawned without destroying it: {e.Message} Retry with destroy true.");
                }
                if (networkObject != null && networkObject.IsSpawned)
                {
                    throw new InvalidOperationException($"Despawning {networkObject.name} failed{errors.Describe()}");
                }
            }
            result.Despawned = true;
            result.Destroyed = destroy;
            return result;
        }

        internal static OwnershipChangeResult ChangeOwner(long networkObjectId, long newOwner, string managerFilter)
        {
            var manager = ResolveRunningManager(managerFilter);
            var networkObject = FindSpawnedObject(manager, networkObjectId);
            if (newOwner < 0)
            {
                throw new ArgumentException("owner must be a client id (0 or more). List them with netcode.gameobjects list_clients.");
            }
            if (!IsValidOwner(manager, (ulong)newOwner))
            {
                throw new ArgumentException($"No connected client {newOwner}. List them with netcode.gameobjects list_clients.");
            }
            if (!manager.DistributedAuthorityMode && !manager.IsServer)
            {
                throw new InvalidOperationException($"{manager.name} is a client; with a client-server topology only the server or host changes ownership. Run the command against the server's editor.");
            }

            var result = new OwnershipChangeResult
            {
                Manager = manager.name,
                NetworkObjectId = networkObjectId,
                Name = networkObject.name,
                PreviousOwner = (long)networkObject.OwnerClientId,
                Topology = manager.NetworkConfig.NetworkTopology.ToString(),
            };
            if (networkObject.OwnerClientId == (ulong)newOwner)
            {
                result.Owner = newOwner;
                result.Notes.Add($"Client {newOwner} already owns it.");
                return result;
            }

            using (var errors = new LogCapture())
            {
                if (!manager.DistributedAuthorityMode && (ulong)newOwner == NetworkManager.ServerClientId)
                {
                    networkObject.RemoveOwnership();
                }
                else
                {
                    networkObject.ChangeOwnership((ulong)newOwner);
                }

                // The change applies immediately when allowed; refusals are only logged at the Developer log level.
                if (networkObject.OwnerClientId != (ulong)newOwner)
                {
                    throw new InvalidOperationException($"Ownership of {networkObject.name} didn't change{errors.Describe()}" + (manager.DistributedAuthorityMode
                        ? " It may be locked, need a request, not be transferable or be session-owner only; check its ownership flags with netcode.gameobjects get_object."
                        : ""));
                }
            }

            result.Owner = (long)networkObject.OwnerClientId;
            return result;
        }

        // --- Helpers ---

        /// <summary>A connected client, or with a client-server topology the server itself (a dedicated server isn't a client).</summary>
        private static bool IsValidOwner(NetworkManager manager, ulong clientId)
        {
            return IsConnected(manager, clientId)
                || (!manager.DistributedAuthorityMode && clientId == NetworkManager.ServerClientId);
        }

        private static bool IsConnected(NetworkManager manager, ulong clientId)
        {
            foreach (var id in manager.ConnectedClientsIds)
            {
                if (id == clientId)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The connected clients other than the manager's own.</summary>
        private static int CountOtherClients(NetworkManager manager)
        {
            var count = 0;
            foreach (var id in manager.ConnectedClientsIds)
            {
                if (id != manager.LocalClientId)
                {
                    count++;
                }
            }
            return count;
        }

        private static void ValidateTimeout(float timeoutSeconds)
        {
            if (timeoutSeconds <= 0 || timeoutSeconds > 600)
            {
                throw new ArgumentException("timeout must be more than 0 and at most 600 seconds.");
            }
        }

        private static Task WaitUntilAsync(Func<bool> condition, float timeoutSeconds, string what)
        {
            var completion = new TaskCompletionSource<bool>();
            var deadline = EditorApplication.timeSinceStartup + timeoutSeconds;

            void Tick()
            {
                try
                {
                    if (condition())
                    {
                        EditorApplication.update -= Tick;
                        completion.TrySetResult(true);
                    }
                    else if (!EditorApplication.isPlaying)
                    {
                        EditorApplication.update -= Tick;
                        completion.TrySetException(new InvalidOperationException($"Play mode stopped while waiting for {what}."));
                    }
                    else if (EditorApplication.timeSinceStartup >= deadline)
                    {
                        EditorApplication.update -= Tick;
                        completion.TrySetException(new TimeoutException($"Timed out waiting for {what}."));
                    }
                }
                catch (Exception e)
                {
                    EditorApplication.update -= Tick;
                    completion.TrySetException(e);
                }
            }

            EditorApplication.update += Tick;
            return completion.Task;
        }

        /// <summary>
        /// Collects warnings and errors logged while it's open. Netcode for GameObjects reports most refused
        /// operations through its log rather than exceptions, so commands use this to return the reason.
        /// </summary>
        private sealed class LogCapture : IDisposable
        {
            private readonly List<string> m_Messages = new List<string>();

            internal LogCapture()
            {
                Application.logMessageReceived += OnLog;
            }

            internal bool HasErrors { get; private set; }

            private void OnLog(string message, string stackTrace, LogType type)
            {
                if (type == LogType.Log)
                {
                    return;
                }
                if (type != LogType.Warning)
                {
                    HasErrors = true;
                }
                m_Messages.Add(message);
            }

            /// <summary>": message; message." for an error message, or "." when nothing was logged.</summary>
            internal string Describe() => m_Messages.Count == 0 ? "." : ": " + string.Join("; ", WithoutDuplicates(m_Messages)) + ".";

            public void Dispose() => Application.logMessageReceived -= OnLog;
        }
    }
}
