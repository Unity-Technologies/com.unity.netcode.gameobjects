using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools;
#endif
using Unity.Profiling;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace Unity.Netcode
{
    /// <summary>
    /// The main component of the library
    /// </summary>
    [AddComponentMenu("Netcode/Network Manager", -100)]
    public class NetworkManager : MonoBehaviour, INetworkUpdateSystem
    {
#pragma warning disable IDE1006 // disable naming rule violation check

        // RuntimeAccessModifiersILPP will make this `public`
        internal delegate void RpcReceiveHandler(NetworkBehaviour behaviour, FastBufferReader reader, __RpcParams parameters);

        // RuntimeAccessModifiersILPP will make this `public`
        internal static readonly Dictionary<uint, RpcReceiveHandler> __rpc_func_table = new Dictionary<uint, RpcReceiveHandler>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // RuntimeAccessModifiersILPP will make this `public`
        internal static readonly Dictionary<uint, string> __rpc_name_table = new Dictionary<uint, string>();
#endif

#pragma warning restore IDE1006 // restore naming rule violation check

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static ProfilerMarker s_SyncTime = new ProfilerMarker($"{nameof(NetworkManager)}.SyncTime");
        private static ProfilerMarker s_TransportPoll = new ProfilerMarker($"{nameof(NetworkManager)}.TransportPoll");
        private static ProfilerMarker s_TransportConnect = new ProfilerMarker($"{nameof(NetworkManager)}.TransportConnect");
        private static ProfilerMarker s_HandleIncomingData = new ProfilerMarker($"{nameof(NetworkManager)}.{nameof(HandleIncomingData)}");
        private static ProfilerMarker s_TransportDisconnect = new ProfilerMarker($"{nameof(NetworkManager)}.TransportDisconnect");
#endif

        private const double k_TimeSyncFrequency = 1.0d; // sync every second
        private const float k_DefaultBufferSizeSec = 0.05f; // todo talk with UX/Product, find good default value for this

        internal static string PrefabDebugHelper(NetworkPrefab networkPrefab)
        {
            return $"{nameof(NetworkPrefab)} \"{networkPrefab.Prefab.name}\"";
        }

        internal NetworkBehaviourUpdater BehaviourUpdater { get; set; }

        internal void MarkNetworkObjectDirty(NetworkObject networkObject)
        {
            BehaviourUpdater.AddForUpdate(networkObject);
        }

        internal MessagingSystem MessagingSystem { get; private set; }

        private NetworkPrefabHandler m_PrefabHandler;

        internal Dictionary<ulong, ConnectionApprovalResponse> ClientsToApprove = new Dictionary<ulong, ConnectionApprovalResponse>();

        /// <summary>
        /// The <see cref="NetworkPrefabHandler"/> instance created after starting the <see cref="NetworkManager"/>
        /// </summary>
        public NetworkPrefabHandler PrefabHandler
        {
            get
            {
                if (m_PrefabHandler == null)
                {
                    m_PrefabHandler = new NetworkPrefabHandler();
                }

                return m_PrefabHandler;
            }
        }

        private bool m_ShuttingDown;
        private bool m_StopProcessingMessages;

        private class NetworkManagerHooks : INetworkHooks
        {
            private NetworkManager m_NetworkManager;

            internal NetworkManagerHooks(NetworkManager manager)
            {
                m_NetworkManager = manager;
            }

            public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
            {
            }

            public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
            {
            }

            public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
            {
            }

            public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
            {
            }

            public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
            {
            }

            public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
            {
            }

            public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
            {
            }

            public void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
            {
            }

            public bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery)
            {
                return !m_NetworkManager.m_StopProcessingMessages;
            }

            public bool OnVerifyCanReceive(ulong senderId, Type messageType, FastBufferReader messageContent, ref NetworkContext context)
            {
                if (m_NetworkManager.PendingClients.TryGetValue(senderId, out PendingClient client) &&
                    (client.ConnectionState == PendingClient.State.PendingApproval || (client.ConnectionState == PendingClient.State.PendingConnection && messageType != typeof(ConnectionRequestMessage))))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Message received from {nameof(senderId)}={senderId} before it has been accepted");
                    }

                    return false;
                }

                return !m_NetworkManager.m_StopProcessingMessages;
            }

            public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
            {
            }

            public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
            {
            }
        }

        private class NetworkManagerMessageSender : IMessageSender
        {
            private NetworkManager m_NetworkManager;

            public NetworkManagerMessageSender(NetworkManager manager)
            {
                m_NetworkManager = manager;
            }

            public void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData)
            {
                var sendBuffer = batchData.ToTempByteArray();

                m_NetworkManager.NetworkConfig.NetworkTransport.Send(m_NetworkManager.ClientIdToTransportId(clientId), sendBuffer, delivery);
            }
        }

        /// <summary>
        /// Returns the <see cref="GameObject"/> to use as the override as could be defined within the NetworkPrefab list
        /// Note: This should be used to create <see cref="GameObject"/> pools (with <see cref="NetworkObject"/> components)
        /// under the scenario where you are using the Host model as it spawns everything locally. As such, the override
        /// will not be applied when spawning locally on a Host.
        /// Related Classes and Interfaces:
        /// <see cref="NetworkPrefabHandler"/>
        /// <see cref="INetworkPrefabInstanceHandler"/>
        /// </summary>
        /// <param name="gameObject">the <see cref="GameObject"/> to be checked for a <see cref="NetworkManager"/> defined NetworkPrefab override</param>
        /// <returns>a <see cref="GameObject"/> that is either the override or if no overrides exist it returns the same as the one passed in as a parameter</returns>
        public GameObject GetNetworkPrefabOverride(GameObject gameObject)
        {
            if (gameObject.TryGetComponent<NetworkObject>(out var networkObject))
            {
                if (NetworkConfig.NetworkPrefabOverrideLinks.ContainsKey(networkObject.GlobalObjectIdHash))
                {
                    switch (NetworkConfig.NetworkPrefabOverrideLinks[networkObject.GlobalObjectIdHash].Override)
                    {
                        case NetworkPrefabOverride.Hash:
                        case NetworkPrefabOverride.Prefab:
                            {
                                return NetworkConfig.NetworkPrefabOverrideLinks[networkObject.GlobalObjectIdHash].OverridingTargetPrefab;
                            }
                    }
                }
            }
            return gameObject;
        }

        /// <summary>
        /// Accessor for the <see cref="NetworkTimeSystem"/> of the NetworkManager.
        /// Prefer the use of the LocalTime and ServerTime properties
        /// </summary>
        public NetworkTimeSystem NetworkTimeSystem { get; private set; }

        /// <summary>
        /// Accessor for the <see cref="NetworkTickSystem"/> of the NetworkManager.
        /// </summary>
        public NetworkTickSystem NetworkTickSystem { get; private set; }

        /// <summary>
        /// The local <see cref="NetworkTime"/>
        /// </summary>
        public NetworkTime LocalTime => NetworkTickSystem?.LocalTime ?? default;

        /// <summary>
        /// The <see cref="NetworkTime"/> on the server
        /// </summary>
        public NetworkTime ServerTime => NetworkTickSystem?.ServerTime ?? default;

        /// <summary>
        /// Gets or sets if the application should be set to run in background
        /// </summary>
        [HideInInspector]
        public bool RunInBackground = true;

        /// <summary>
        /// The log level to use
        /// </summary>
        [HideInInspector]
        public LogLevel LogLevel = LogLevel.Normal;

        /// <summary>
        /// The singleton instance of the NetworkManager
        /// </summary>
        public static NetworkManager Singleton { get; private set; }

        /// <summary>
        /// Gets the SpawnManager for this NetworkManager
        /// </summary>
        public NetworkSpawnManager SpawnManager { get; private set; }

        internal IDeferredMessageManager DeferredMessageManager { get; private set; }

        /// <summary>
        /// Gets the CustomMessagingManager for this NetworkManager
        /// </summary>
        public CustomMessagingManager CustomMessagingManager { get; private set; }

        /// <summary>
        /// The <see cref="NetworkSceneManager"/> instance created after starting the <see cref="NetworkManager"/>
        /// </summary>
        public NetworkSceneManager SceneManager { get; private set; }

        /// <summary>
        /// The client id used to represent the server
        /// </summary>
        public const ulong ServerClientId = 0;

        /// <summary>
        /// Gets the networkId of the server
        /// </summary>
        private ulong m_ServerTransportId => NetworkConfig.NetworkTransport?.ServerClientId ?? throw new NullReferenceException($"The transport in the active {nameof(NetworkConfig)} is null");

        /// <summary>
        /// Returns ServerClientId if IsServer or LocalClientId if not
        /// </summary>
        public ulong LocalClientId
        {
            get => IsServer ? NetworkConfig.NetworkTransport.ServerClientId : m_LocalClientId;
            internal set => m_LocalClientId = value;
        }

        private ulong m_LocalClientId;

        private Dictionary<ulong, NetworkClient> m_ConnectedClients = new Dictionary<ulong, NetworkClient>();

        private ulong m_NextClientId = 1;
        private Dictionary<ulong, ulong> m_ClientIdToTransportIdMap = new Dictionary<ulong, ulong>();
        private Dictionary<ulong, ulong> m_TransportIdToClientIdMap = new Dictionary<ulong, ulong>();

        private List<NetworkClient> m_ConnectedClientsList = new List<NetworkClient>();

        private List<ulong> m_ConnectedClientIds = new List<ulong>();

        /// <summary>
        /// Gets a dictionary of connected clients and their clientId keys. This is only accessible on the server.
        /// </summary>
        public IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients
        {
            get
            {
                if (IsServer == false)
                {
                    throw new NotServerException($"{nameof(ConnectedClients)} should only be accessed on server.");
                }
                return m_ConnectedClients;
            }
        }

        /// <summary>
        /// Gets a list of connected clients. This is only accessible on the server.
        /// </summary>
        public IReadOnlyList<NetworkClient> ConnectedClientsList
        {
            get
            {
                if (IsServer == false)
                {
                    throw new NotServerException($"{nameof(ConnectedClientsList)} should only be accessed on server.");
                }
                return m_ConnectedClientsList;
            }
        }

        /// <summary>
        /// Gets a list of just the IDs of all connected clients. This is only accessible on the server.
        /// </summary>
        public IReadOnlyList<ulong> ConnectedClientsIds
        {
            get
            {
                if (IsServer == false)
                {
                    throw new NotServerException($"{nameof(m_ConnectedClientIds)} should only be accessed on server.");
                }
                return m_ConnectedClientIds;
            }
        }

        /// <summary>
        /// Gets the local <see cref="NetworkClient"/> for this client.
        /// </summary>
        public NetworkClient LocalClient { get; internal set; }

        /// <summary>
        /// Gets a dictionary of the clients that have been accepted by the transport but are still pending by the Netcode. This is only populated on the server.
        /// </summary>
        public readonly Dictionary<ulong, PendingClient> PendingClients = new Dictionary<ulong, PendingClient>();

        /// <summary>
        /// Gets Whether or not a server is running
        /// </summary>
        public bool IsServer { get; internal set; }

        /// <summary>
        /// Gets Whether or not a client is running
        /// </summary>
        public bool IsClient { get; internal set; }

        /// <summary>
        /// Gets if we are running as host
        /// </summary>
        public bool IsHost => IsServer && IsClient;

        /// <summary>
        /// Gets Whether or not we are listening for connections
        /// </summary>
        public bool IsListening { get; internal set; }

        /// <summary>
        /// When true, the client is connected, approved, and synchronized with
        /// the server.
        /// </summary>
        public bool IsConnectedClient { get; internal set; }

        /// <summary>
        /// Is true when the client has been approved.
        /// </summary>
        /// <remarks>
        /// This only reflects the client's approved status and does not mean the client
        /// has finished the connection and synchronization process. The server-host will
        /// always be approved upon being starting the <see cref="NetworkManager"/>
        /// <see cref="IsConnectedClient"/>
        /// </remarks>
        public bool IsApproved { get; internal set; }

        /// <summary>
        /// Can be used to determine if the <see cref="NetworkManager"/> is currently shutting itself down
        /// </summary>
        public bool ShutdownInProgress { get { return m_ShuttingDown; } }

        /// <summary>
        /// The callback to invoke once a client connects. This callback is only ran on the server and on the local client that connects.
        /// </summary>
        public event Action<ulong> OnClientConnectedCallback = null;

        internal void InvokeOnClientConnectedCallback(ulong clientId) => OnClientConnectedCallback?.Invoke(clientId);

        /// <summary>
        /// The callback to invoke when a client disconnects. This callback is only ran on the server and on the local client that disconnects.
        /// </summary>
        public event Action<ulong> OnClientDisconnectCallback = null;

        /// <summary>
        /// The callback to invoke once the server is ready
        /// </summary>
        public event Action OnServerStarted = null;

        /// <summary>
        /// The callback to invoke if the <see cref="NetworkTransport"/> fails.
        /// </summary>
        /// <remarks>
        /// A failure of the transport is always followed by the <see cref="NetworkManager"/> shutting down. Recovering
        /// from a transport failure would normally entail reconfiguring the transport (e.g. re-authenticating, or
        /// recreating a new service allocation depending on the transport) and restarting the client/server/host.
        /// </remarks>
        public event Action OnTransportFailure = null;

        /// <summary>
        /// Connection Approval Response
        /// </summary>
        public class ConnectionApprovalResponse
        {
            /// <summary>
            /// Whether or not the client was approved
            /// </summary>
            public bool Approved;
            /// <summary>
            /// If true, a player object will be created. Otherwise the client will have no object.
            /// </summary>
            public bool CreatePlayerObject;
            /// <summary>
            /// The prefabHash to use for the client. If createPlayerObject is false, this is ignored. If playerPrefabHash is null, the default player prefab is used.
            /// </summary>
            public uint? PlayerPrefabHash;
            /// <summary>
            /// The position to spawn the client at. If null, the prefab position is used.
            /// </summary>
            public Vector3? Position;
            /// <summary>
            /// The rotation to spawn the client with. If null, the prefab position is used.
            /// </summary>
            public Quaternion? Rotation;
            /// <summary>
            /// If the Approval decision cannot be made immediately, the client code can set Pending to true, keep a reference to the ConnectionApprovalResponse object and write to it later. Client code must exercise care to setting all the members to the value it wants before marking Pending to false, to indicate completion. If the field is set as Pending = true, we'll monitor the object until it gets set to not pending anymore and use the parameters then.
            /// </summary>
            public bool Pending;
        }

        /// <summary>
        /// Connection Approval Request
        /// </summary>
        public struct ConnectionApprovalRequest
        {
            /// <summary>
            /// The connection data payload
            /// </summary>
            public byte[] Payload;
            /// <summary>
            /// The Network Id of the client we are about to handle
            /// </summary>
            public ulong ClientNetworkId;
        }

        /// <summary>
        /// The callback to invoke during connection approval. Allows client code to decide whether or not to allow incoming client connection
        /// </summary>
        public Action<ConnectionApprovalRequest, ConnectionApprovalResponse> ConnectionApprovalCallback
        {
            get => m_ConnectionApprovalCallback;
            set
            {
                if (value != null && value.GetInvocationList().Length > 1)
                {
                    throw new InvalidOperationException($"Only one {nameof(ConnectionApprovalCallback)} can be registered at a time.");
                }
                else
                {
                    m_ConnectionApprovalCallback = value;
                }
            }
        }

        private Action<ConnectionApprovalRequest, ConnectionApprovalResponse> m_ConnectionApprovalCallback;

        /// <summary>
        /// The current NetworkConfig
        /// </summary>
        [HideInInspector]
        public NetworkConfig NetworkConfig;

        /// <summary>
        /// The current host name we are connected to, used to validate certificate
        /// </summary>
        public string ConnectedHostname { get; private set; }

        internal INetworkMetrics NetworkMetrics { get; private set; }

        internal static event Action OnSingletonReady;

#if UNITY_EDITOR
        internal void OnValidate()
        {
            if (NetworkConfig == null)
            {
                return; // May occur when the component is added
            }

            if (GetComponentInChildren<NetworkObject>() != null)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"{nameof(NetworkManager)} cannot be a {nameof(NetworkObject)}.");
                }
            }

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            // If the scene is not dirty or the asset database is currently updating then we can skip updating the NetworkPrefab information
            if (!activeScene.isDirty || EditorApplication.isUpdating)
            {
                return;
            }

            // During OnValidate we will always clear out NetworkPrefabOverrideLinks and rebuild it
            NetworkConfig.NetworkPrefabOverrideLinks.Clear();

            // Check network prefabs and assign to dictionary for quick look up
            for (int i = 0; i < NetworkConfig.NetworkPrefabs.Count; i++)
            {
                var networkPrefab = NetworkConfig.NetworkPrefabs[i];
                var networkPrefabGo = networkPrefab?.Prefab;
                if (networkPrefabGo != null)
                {
                    if (!networkPrefabGo.TryGetComponent<NetworkObject>(out var networkObject))
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogError($"Cannot register {PrefabDebugHelper(networkPrefab)}, it does not have a {nameof(NetworkObject)} component at its root");
                        }
                    }
                    else
                    {
                        {
                            var childNetworkObjects = new List<NetworkObject>();
                            networkPrefabGo.GetComponentsInChildren(true, childNetworkObjects);
                            if (childNetworkObjects.Count > 1) // total count = 1 root NetworkObject + n child NetworkObjects
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                                {
                                    NetworkLog.LogWarning($"{PrefabDebugHelper(networkPrefab)} has child {nameof(NetworkObject)}(s) but they will not be spawned across the network (unsupported {nameof(NetworkPrefab)} setup)");
                                }
                            }
                        }

                        // Default to the standard NetworkPrefab.Prefab's NetworkObject first
                        var globalObjectIdHash = networkObject.GlobalObjectIdHash;

                        // Now check to see if it has an override
                        switch (networkPrefab.Override)
                        {
                            case NetworkPrefabOverride.Prefab:
                                {
                                    if (NetworkConfig.NetworkPrefabs[i].SourcePrefabToOverride == null &&
                                        NetworkConfig.NetworkPrefabs[i].Prefab != null)
                                    {
                                        if (networkPrefab.SourcePrefabToOverride == null)
                                        {
                                            networkPrefab.SourcePrefabToOverride = networkPrefabGo;
                                        }

                                        globalObjectIdHash = networkPrefab.SourcePrefabToOverride.GetComponent<NetworkObject>().GlobalObjectIdHash;
                                    }

                                    break;
                                }
                            case NetworkPrefabOverride.Hash:
                                globalObjectIdHash = networkPrefab.SourceHashToOverride;
                                break;
                        }

                        // Add to the NetworkPrefabOverrideLinks or handle a new (blank) entries
                        if (!NetworkConfig.NetworkPrefabOverrideLinks.ContainsKey(globalObjectIdHash))
                        {
                            NetworkConfig.NetworkPrefabOverrideLinks.Add(globalObjectIdHash, networkPrefab);
                        }
                        else
                        {
                            // Duplicate entries can happen when adding a new entry into a list of existing entries
                            // Either this is user error or a new entry, either case we replace it with a new, blank, NetworkPrefab under this condition
                            NetworkConfig.NetworkPrefabs[i] = new NetworkPrefab();
                        }
                    }
                }
            }
        }
#endif
        /// <summary>
        /// Adds a new prefab to the network prefab list.
        /// This can be any GameObject with a NetworkObject component, from any source (addressables, asset
        /// bundles, Resource.Load, dynamically created, etc)
        ///
        /// There are three limitations to this method:
        /// - If you have NetworkConfig.ForceSamePrefabs enabled, you can only do this before starting
        /// networking, and the server and all connected clients must all have the same exact set of prefabs
        /// added via this method before connecting
        /// - Adding a prefab on the server does not automatically add it on the client - it's up to you
        /// to make sure the client and server are synchronized via whatever method makes sense for your game
        /// (RPCs, configs, deterministic loading, etc)
        /// - If the server sends a Spawn message to a client that has not yet added a prefab for, the spawn message
        /// and any other relevant messages will be held for a configurable time (default 1 second, configured via
        /// NetworkConfig.SpawnTimeout) before an error is logged. This is intented to enable the SDK to gracefully
        /// handle unexpected conditions (slow disks, slow network, etc) that slow down asset loading. This timeout
        /// should not be relied on and code shouldn't be written around it - your code should be written so that
        /// the asset is expected to be loaded before it's needed.
        /// </summary>
        /// <param name="prefab"></param>
        /// <exception cref="Exception"></exception>
        public void AddNetworkPrefab(GameObject prefab)
        {
            if (IsListening && NetworkConfig.ForceSamePrefabs)
            {
                throw new Exception($"All prefabs must be registered before starting {nameof(NetworkManager)} when {nameof(NetworkConfig.ForceSamePrefabs)} is enabled.");
            }

            var networkObject = prefab.GetComponent<NetworkObject>();
            if (!networkObject)
            {
                throw new Exception($"All {nameof(NetworkPrefab)}s must contain a {nameof(NetworkObject)} component.");
            }

            var networkPrefab = new NetworkPrefab { Prefab = prefab };
            NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            if (IsListening)
            {
                var sourcePrefabGlobalObjectIdHash = (uint)0;
                var targetPrefabGlobalObjectIdHash = (uint)0;
                if (!ShouldAddPrefab(networkPrefab, out sourcePrefabGlobalObjectIdHash, out targetPrefabGlobalObjectIdHash))
                {
                    NetworkConfig.NetworkPrefabs.Remove(networkPrefab);
                    return;
                }

                if (!AddPrefabRegistration(networkPrefab, sourcePrefabGlobalObjectIdHash, targetPrefabGlobalObjectIdHash))
                {
                    NetworkConfig.NetworkPrefabs.Remove(networkPrefab);
                    return;
                }
                DeferredMessageManager.ProcessTriggers(IDeferredMessageManager.TriggerType.OnAddPrefab, networkObject.GlobalObjectIdHash);
            }
        }

        /// <summary>
        /// Remove a prefab from the prefab list.
        /// As with AddNetworkPrefab, this is specific to the client it's called on -
        /// calling it on the server does not automatically remove anything on any of the
        /// client processes.
        ///
        /// Like AddNetworkPrefab, when NetworkConfig.ForceSamePrefabs is enabled,
        /// this cannot be called after connecting.
        /// </summary>
        /// <param name="prefab"></param>
        public void RemoveNetworkPrefab(GameObject prefab)
        {
            if (IsListening && NetworkConfig.ForceSamePrefabs)
            {
                throw new Exception($"Prefabs cannot be removed after starting {nameof(NetworkManager)} when {nameof(NetworkConfig.ForceSamePrefabs)} is enabled.");
            }

            var globalObjectIdHash = prefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
            for (var i = 0; i < NetworkConfig.NetworkPrefabs.Count; ++i)
            {
                if (NetworkConfig.NetworkPrefabs[i].Prefab.GetComponent<NetworkObject>().GlobalObjectIdHash == globalObjectIdHash)
                {
                    NetworkConfig.NetworkPrefabs.RemoveAt(i);
                    break;
                }
            }
            if (PrefabHandler.ContainsHandler(globalObjectIdHash))
            {
                PrefabHandler.RemoveHandler(globalObjectIdHash);
            }
            if (NetworkConfig.NetworkPrefabOverrideLinks.TryGetValue(globalObjectIdHash, out var targetPrefab))
            {
                NetworkConfig.NetworkPrefabOverrideLinks.Remove(globalObjectIdHash);
                var targetHash = targetPrefab.Prefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
                if (NetworkConfig.OverrideToNetworkPrefab.ContainsKey(targetHash))
                {
                    NetworkConfig.OverrideToNetworkPrefab.Remove(targetHash);
                }
            }
        }

        private bool ShouldAddPrefab(NetworkPrefab networkPrefab, out uint sourcePrefabGlobalObjectIdHash, out uint targetPrefabGlobalObjectIdHash, int index = -1)
        {
            sourcePrefabGlobalObjectIdHash = 0;
            targetPrefabGlobalObjectIdHash = 0;
            var networkObject = (NetworkObject)null;
            if (networkPrefab == null || (networkPrefab.Prefab == null && networkPrefab.Override == NetworkPrefabOverride.None))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogWarning(
                        $"{nameof(NetworkPrefab)} cannot be null ({nameof(NetworkPrefab)} at index: {index})");
                }
                return false;
            }
            else if (networkPrefab.Override == NetworkPrefabOverride.None)
            {
                if (!networkPrefab.Prefab.TryGetComponent(out networkObject))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                    {
                        NetworkLog.LogWarning($"{PrefabDebugHelper(networkPrefab)} is missing " +
                                              $"a {nameof(NetworkObject)} component (entry will be ignored).");
                    }
                    return false;
                }

                // Otherwise get the GlobalObjectIdHash value
                sourcePrefabGlobalObjectIdHash = networkObject.GlobalObjectIdHash;
            }
            else // Validate Overrides
            {
                // Validate source prefab override values first
                switch (networkPrefab.Override)
                {
                    case NetworkPrefabOverride.Hash:
                        {
                            if (networkPrefab.SourceHashToOverride == 0)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                                {
                                    NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(NetworkPrefab.SourceHashToOverride)} is zero " +
                                                          "(entry will be ignored).");
                                }
                                return false;
                            }
                            sourcePrefabGlobalObjectIdHash = networkPrefab.SourceHashToOverride;
                            break;
                        }
                    case NetworkPrefabOverride.Prefab:
                        {
                            if (networkPrefab.SourcePrefabToOverride == null)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                                {
                                    NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(NetworkPrefab.SourcePrefabToOverride)} is null (entry will be ignored).");
                                }

                                Debug.LogWarning($"{nameof(NetworkPrefab)} override entry {networkPrefab.SourceHashToOverride} will be removed and ignored.");
                                return false;
                            }
                            else
                            {
                                if (!networkPrefab.SourcePrefabToOverride.TryGetComponent(out networkObject))
                                {
                                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                                    {
                                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} ({networkPrefab.SourcePrefabToOverride.name}) " +
                                                              $"is missing a {nameof(NetworkObject)} component (entry will be ignored).");
                                    }

                                    Debug.LogWarning($"{nameof(NetworkPrefab)} override entry (\"{networkPrefab.SourcePrefabToOverride.name}\") will be removed and ignored.");
                                    return false;
                                }

                                sourcePrefabGlobalObjectIdHash = networkObject.GlobalObjectIdHash;
                            }
                            break;
                        }
                }

                // Validate target prefab override values next
                if (networkPrefab.OverridingTargetPrefab == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                    {
                        NetworkLog.LogWarning($"{nameof(NetworkPrefab)} {nameof(NetworkPrefab.OverridingTargetPrefab)} is null!");
                    }
                    switch (networkPrefab.Override)
                    {
                        case NetworkPrefabOverride.Hash:
                            {
                                Debug.LogWarning($"{nameof(NetworkPrefab)} override entry {networkPrefab.SourceHashToOverride} will be removed and ignored.");
                                break;
                            }
                        case NetworkPrefabOverride.Prefab:
                            {
                                Debug.LogWarning($"{nameof(NetworkPrefab)} override entry ({networkPrefab.SourcePrefabToOverride.name}) will be removed and ignored.");
                                break;
                            }
                    }
                    return false;
                }
                else
                {
                    targetPrefabGlobalObjectIdHash = networkPrefab.OverridingTargetPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
                }
            }
            return true;
        }

        internal bool AddPrefabRegistration(NetworkPrefab networkPrefab, uint sourcePrefabGlobalObjectIdHash, uint targetPrefabGlobalObjectIdHash)
        {
            // Assign the appropriate GlobalObjectIdHash to the appropriate NetworkPrefab
            if (!NetworkConfig.NetworkPrefabOverrideLinks.ContainsKey(sourcePrefabGlobalObjectIdHash))
            {
                if (networkPrefab.Override == NetworkPrefabOverride.None)
                {
                    NetworkConfig.NetworkPrefabOverrideLinks.Add(sourcePrefabGlobalObjectIdHash, networkPrefab);
                }
                else
                {
                    if (!NetworkConfig.OverrideToNetworkPrefab.ContainsKey(targetPrefabGlobalObjectIdHash))
                    {
                        switch (networkPrefab.Override)
                        {
                            case NetworkPrefabOverride.Prefab:
                                {
                                    NetworkConfig.NetworkPrefabOverrideLinks.Add(sourcePrefabGlobalObjectIdHash, networkPrefab);
                                    NetworkConfig.OverrideToNetworkPrefab.Add(targetPrefabGlobalObjectIdHash, sourcePrefabGlobalObjectIdHash);
                                }
                                break;
                            case NetworkPrefabOverride.Hash:
                                {
                                    NetworkConfig.NetworkPrefabOverrideLinks.Add(sourcePrefabGlobalObjectIdHash, networkPrefab);
                                    NetworkConfig.OverrideToNetworkPrefab.Add(targetPrefabGlobalObjectIdHash, sourcePrefabGlobalObjectIdHash);
                                }
                                break;
                        }
                    }
                    else
                    {
                        var networkObject = networkPrefab.Prefab.GetComponent<NetworkObject>();
                        // This can happen if a user tries to make several GlobalObjectIdHash values point to the same target
                        Debug.LogError($"{nameof(NetworkPrefab)} (\"{networkObject.name}\") has a duplicate {nameof(NetworkObject.GlobalObjectIdHash)} target entry value of: {targetPrefabGlobalObjectIdHash}! Removing entry from list!");
                        return false;
                    }
                }
            }
            else
            {
                var networkObject = networkPrefab.Prefab.GetComponent<NetworkObject>();
                // This should never happen, but in the case it somehow does log an error and remove the duplicate entry
                Debug.LogError($"{nameof(NetworkPrefab)} ({networkObject.name}) has a duplicate {nameof(NetworkObject.GlobalObjectIdHash)} source entry value of: {sourcePrefabGlobalObjectIdHash}! Removing entry from list!");
                return false;
            }
            return true;
        }

        private void InitializePrefabs(int startIdx = 0)
        {
            // This is used to remove entries not needed or invalid
            var removeEmptyPrefabs = new List<int>();

            // Build the NetworkPrefabOverrideLinks dictionary
            for (int i = startIdx; i < NetworkConfig.NetworkPrefabs.Count; i++)
            {
                var sourcePrefabGlobalObjectIdHash = (uint)0;
                var targetPrefabGlobalObjectIdHash = (uint)0;
                if (!ShouldAddPrefab(NetworkConfig.NetworkPrefabs[i], out sourcePrefabGlobalObjectIdHash, out targetPrefabGlobalObjectIdHash, i))
                {
                    removeEmptyPrefabs.Add(i);
                    continue;
                }

                if (!AddPrefabRegistration(NetworkConfig.NetworkPrefabs[i], sourcePrefabGlobalObjectIdHash, targetPrefabGlobalObjectIdHash))
                {
                    removeEmptyPrefabs.Add(i);
                    continue;
                }
            }

            // Clear out anything that is invalid or not used (for invalid entries we already logged warnings to the user earlier)
            // Iterate backwards so indices don't shift as we remove
            for (int i = removeEmptyPrefabs.Count - 1; i >= 0; i--)
            {
                NetworkConfig.NetworkPrefabs.RemoveAt(removeEmptyPrefabs[i]);
            }

            removeEmptyPrefabs.Clear();
        }

        private void Initialize(bool server)
        {
            // Don't allow the user to start a network session if the NetworkManager is
            // still parented under another GameObject
            if (NetworkManagerCheckForParent(true))
            {
                return;
            }

            IsApproved = false;

            ComponentFactory.SetDefaults();

            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo(nameof(Initialize));
            }

            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
            this.RegisterNetworkUpdate(NetworkUpdateStage.PostLateUpdate);

            MessagingSystem = new MessagingSystem(new NetworkManagerMessageSender(this), this);

            MessagingSystem.Hook(new NetworkManagerHooks(this));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            MessagingSystem.Hook(new ProfilingHooks());
#endif

#if MULTIPLAYER_TOOLS
            MessagingSystem.Hook(new MetricHooks(this));
#endif
            LocalClientId = ulong.MaxValue;

            ClearClients();

            // Create spawn manager instance
            SpawnManager = new NetworkSpawnManager(this);

            DeferredMessageManager = ComponentFactory.Create<IDeferredMessageManager>(this);

            CustomMessagingManager = new CustomMessagingManager(this);

            SceneManager = new NetworkSceneManager(this);

            BehaviourUpdater = new NetworkBehaviourUpdater();


            if (NetworkMetrics == null)
            {
#if MULTIPLAYER_TOOLS
                NetworkMetrics = new NetworkMetrics();
#else
                NetworkMetrics = new NullNetworkMetrics();
#endif
            }

#if MULTIPLAYER_TOOLS
            NetworkSolutionInterface.SetInterface(new NetworkSolutionInterfaceParameters
            {
                NetworkObjectProvider = new NetworkObjectProvider(this),
            });
#endif

            if (NetworkConfig.NetworkTransport == null)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogError("No transport has been selected!");
                }

                return;
            }

            NetworkConfig.NetworkTransport.NetworkMetrics = NetworkMetrics;

            if (server)
            {
                NetworkTimeSystem = NetworkTimeSystem.ServerTimeSystem();
            }
            else
            {
                NetworkTimeSystem = new NetworkTimeSystem(1.0 / NetworkConfig.TickRate, k_DefaultBufferSizeSec, 0.2);
            }

            NetworkTickSystem = new NetworkTickSystem(NetworkConfig.TickRate, 0, 0);
            NetworkTickSystem.Tick += OnNetworkManagerTick;

            this.RegisterNetworkUpdate(NetworkUpdateStage.PreUpdate);

            // Always clear our prefab override links before building
            NetworkConfig.NetworkPrefabOverrideLinks.Clear();
            NetworkConfig.OverrideToNetworkPrefab.Clear();

            InitializePrefabs();

            // If we have a player prefab, then we need to verify it is in the list of NetworkPrefabOverrideLinks for client side spawning.
            if (NetworkConfig.PlayerPrefab != null)
            {
                if (NetworkConfig.PlayerPrefab.TryGetComponent<NetworkObject>(out var playerPrefabNetworkObject))
                {
                    //In the event there is no NetworkPrefab entry (i.e. no override for default player prefab)
                    if (!NetworkConfig.NetworkPrefabOverrideLinks.ContainsKey(playerPrefabNetworkObject
                        .GlobalObjectIdHash))
                    {
                        //Then add a new entry for the player prefab
                        var playerNetworkPrefab = new NetworkPrefab();
                        playerNetworkPrefab.Prefab = NetworkConfig.PlayerPrefab;
                        NetworkConfig.NetworkPrefabs.Insert(0, playerNetworkPrefab);
                        NetworkConfig.NetworkPrefabOverrideLinks.Add(playerPrefabNetworkObject.GlobalObjectIdHash,
                            playerNetworkPrefab);
                    }
                }
                else
                {
                    // Provide the name of the prefab with issues so the user can more easily find the prefab and fix it
                    Debug.LogError($"{nameof(NetworkConfig.PlayerPrefab)} (\"{NetworkConfig.PlayerPrefab.name}\") has no NetworkObject assigned to it!.");
                }
            }

            NetworkConfig.NetworkTransport.OnTransportEvent += HandleRawTransportPoll;

            NetworkConfig.NetworkTransport.Initialize(this);
        }

        private void ClearClients()
        {
            PendingClients.Clear();
            m_ConnectedClients.Clear();
            m_ConnectedClientsList.Clear();
            m_ConnectedClientIds.Clear();
            LocalClient = null;
            NetworkObject.OrphanChildren.Clear();
            ClientsToApprove.Clear();
        }

        /// <summary>
        /// Starts a server
        /// </summary>
        /// <returns>(<see cref="true"/>/<see cref="false"/>) returns true if <see cref="NetworkManager"/> started in server mode successfully.</returns>
        public bool StartServer()
        {
            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo(nameof(StartServer));
            }

            if (!CanStart(StartType.Server))
            {
                return false;
            }

            Initialize(true);
            IsServer = true;
            IsClient = false;
            IsListening = true;

            try
            {
                // If we failed to start then shutdown and notify user that the transport failed to start
                if (NetworkConfig.NetworkTransport.StartServer())
                {
                    SpawnManager.ServerSpawnSceneObjectsOnStartSweep();

                    OnServerStarted?.Invoke();
                    IsApproved = true;
                    return true;
                }
                else
                {
                    IsServer = false;
                    IsClient = false;
                    IsListening = false;

                    Debug.LogError($"Server is shutting down due to network transport start failure of {NetworkConfig.NetworkTransport.GetType().Name}!");
                    OnTransportFailure?.Invoke();
                    Shutdown();
                }
            }
            catch (Exception)
            {
                IsServer = false;
                IsClient = false;
                IsListening = false;
                throw;
            }

            return false;
        }

        /// <summary>
        /// Starts a client
        /// </summary>
        /// <returns>(<see cref="true"/>/<see cref="false"/>) returns true if <see cref="NetworkManager"/> started in client mode successfully.</returns>
        public bool StartClient()
        {
            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo(nameof(StartClient));
            }

            if (!CanStart(StartType.Client))
            {
                return false;
            }

            Initialize(false);
            MessagingSystem.ClientConnected(ServerClientId);

            if (!NetworkConfig.NetworkTransport.StartClient())
            {
                Debug.LogError($"Client is shutting down due to network transport start failure of {NetworkConfig.NetworkTransport.GetType().Name}!");
                OnTransportFailure?.Invoke();
                Shutdown();
                return false;
            }

            IsServer = false;
            IsClient = true;
            IsListening = true;

            return true;
        }

        /// <summary>
        /// Starts a Host
        /// </summary>
        /// <returns>(<see cref="true"/>/<see cref="false"/>) returns true if <see cref="NetworkManager"/> started in host mode successfully.</returns>
        public bool StartHost()
        {
            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo(nameof(StartHost));
            }

            if (!CanStart(StartType.Host))
            {
                return false;
            }

            Initialize(true);

            IsServer = true;
            IsClient = true;
            IsListening = true;

            try
            {
                // If we failed to start then shutdown and notify user that the transport failed to start
                if (!NetworkConfig.NetworkTransport.StartServer())
                {
                    Debug.LogError($"Server is shutting down due to network transport start failure of {NetworkConfig.NetworkTransport.GetType().Name}!");
                    OnTransportFailure?.Invoke();
                    Shutdown();

                    IsServer = false;
                    IsClient = false;
                    IsListening = false;

                    return false;
                }
            }
            catch (Exception)
            {
                IsServer = false;
                IsClient = false;
                IsListening = false;
                throw;
            }

            MessagingSystem.ClientConnected(ServerClientId);
            LocalClientId = ServerClientId;
            NetworkMetrics.SetConnectionId(LocalClientId);

            if (NetworkConfig.ConnectionApproval && ConnectionApprovalCallback != null)
            {
                var response = new ConnectionApprovalResponse();
                ConnectionApprovalCallback(new ConnectionApprovalRequest { Payload = NetworkConfig.ConnectionData, ClientNetworkId = ServerClientId }, response);
                if (!response.Approved)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning("You cannot decline the host connection. The connection was automatically approved.");
                    }
                }

                response.Approved = true;
                IsApproved = true;
                HandleConnectionApproval(ServerClientId, response);
            }
            else
            {
                var response = new ConnectionApprovalResponse
                {
                    Approved = true,
                    CreatePlayerObject = NetworkConfig.PlayerPrefab != null
                };
                HandleConnectionApproval(ServerClientId, response);
            }

            SpawnManager.ServerSpawnSceneObjectsOnStartSweep();

            OnServerStarted?.Invoke();

            return true;
        }

        private enum StartType
        {
            Server,
            Host,
            Client
        }

        private bool CanStart(StartType type)
        {
            if (IsListening)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning("Cannot start " + type + " while an instance is already running");
                }

                return false;
            }

            // Only if it is starting as a server or host do we need to check this
            // Clients don't invoke the ConnectionApprovalCallback
            if (NetworkConfig.ConnectionApproval && type != StartType.Client)
            {
                if (ConnectionApprovalCallback == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning(
                            "No ConnectionApproval callback defined. Connection approval will timeout");
                    }
                }
            }

            if (ConnectionApprovalCallback != null)
            {
                if (!NetworkConfig.ConnectionApproval)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning(
                            "A ConnectionApproval callback is defined but ConnectionApproval is disabled. In order to use ConnectionApproval it has to be explicitly enabled ");
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Set this NetworkManager instance as the static NetworkManager singleton
        /// </summary>
        public void SetSingleton()
        {
            Singleton = this;

            OnSingletonReady?.Invoke();
        }

        private void OnEnable()
        {
            if (RunInBackground)
            {
                Application.runInBackground = true;
            }

            if (Singleton == null)
            {
                SetSingleton();
            }

            if (!NetworkManagerCheckForParent())
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Awake()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// Handle runtime detection for parenting the NetworkManager's GameObject under another GameObject
        /// </summary>
        private void OnTransformParentChanged()
        {
            NetworkManagerCheckForParent();
        }

        /// <summary>
        /// Determines if the NetworkManager's GameObject is parented under another GameObject and
        /// notifies the user that this is not allowed for the NetworkManager.
        /// </summary>
        internal bool NetworkManagerCheckForParent(bool ignoreNetworkManagerCache = false)
        {
#if UNITY_EDITOR
            var isParented = NetworkManagerHelper.NotifyUserOfNestedNetworkManager(this, ignoreNetworkManagerCache);
#else
            var isParented = transform.root != transform;
            if (isParented)
            {
                throw new Exception(GenerateNestedNetworkManagerMessage(transform));
            }
#endif
            return isParented;
        }

        static internal string GenerateNestedNetworkManagerMessage(Transform transform)
        {
            return $"{transform.name} is nested under {transform.root.name}. NetworkManager cannot be nested.\n";
        }

#if UNITY_EDITOR
        static internal INetworkManagerHelper NetworkManagerHelper;
        /// <summary>
        /// Interface for NetworkManagerHelper
        /// </summary>
        internal interface INetworkManagerHelper
        {
            bool NotifyUserOfNestedNetworkManager(NetworkManager networkManager, bool ignoreNetworkManagerCache = false, bool editorTest = false);
            void CheckAndNotifyUserNetworkObjectRemoved(NetworkManager networkManager, bool editorTest = false);
        }
#endif

        // Ensures that the NetworkManager is cleaned up before OnDestroy is run on NetworkObjects and NetworkBehaviours when unloading a scene with a NetworkManager
        private void OnSceneUnloaded(Scene scene)
        {
            if (scene == gameObject.scene)
            {
                OnDestroy();
            }
        }

        // Ensures that the NetworkManager is cleaned up before OnDestroy is run on NetworkObjects and NetworkBehaviours when quitting the application.
        private void OnApplicationQuit()
        {
            OnDestroy();
        }

        // Note that this gets also called manually by OnSceneUnloaded and OnApplicationQuit
        private void OnDestroy()
        {
            ShutdownInternal();

            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;

            if (Singleton == this)
            {
                Singleton = null;
            }
        }

        private void DisconnectRemoteClient(ulong clientId)
        {
            var transportId = ClientIdToTransportId(clientId);
            NetworkConfig.NetworkTransport.DisconnectRemoteClient(transportId);
        }

        /// <summary>
        /// Globally shuts down the library.
        /// Disconnects clients if connected and stops server if running.
        /// </summary>
        /// <param name="discardMessageQueue">
        /// If false, any messages that are currently in the incoming queue will be handled,
        /// and any messages in the outgoing queue will be sent, before the shutdown is processed.
        /// If true, NetworkManager will shut down immediately, and any unprocessed or unsent messages
        /// will be discarded.
        /// </param>
        public void Shutdown(bool discardMessageQueue = false)
        {
            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo(nameof(Shutdown));
            }

            // If we're not running, don't start shutting down, it would only cause an immediate
            // shutdown the next time the manager is started.
            if (IsServer || IsClient)
            {
                m_ShuttingDown = true;
                m_StopProcessingMessages = discardMessageQueue;
            }

            NetworkConfig.NetworkTransport.OnTransportEvent -= HandleRawTransportPoll;
        }

        internal void ShutdownInternal()
        {
            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo(nameof(ShutdownInternal));
            }

            if (IsServer)
            {
                // make sure all messages are flushed before transport disconnect clients
                if (MessagingSystem != null)
                {
                    MessagingSystem.ProcessSendQueues();
                }

                var disconnectedIds = new HashSet<ulong>();

                //Don't know if I have to disconnect the clients. I'm assuming the NetworkTransport does all the cleaning on shutdown. But this way the clients get a disconnect message from server (so long it does't get lost)

                foreach (KeyValuePair<ulong, NetworkClient> pair in ConnectedClients)
                {
                    if (!disconnectedIds.Contains(pair.Key))
                    {
                        disconnectedIds.Add(pair.Key);

                        if (pair.Key == NetworkConfig.NetworkTransport.ServerClientId)
                        {
                            continue;
                        }

                        DisconnectRemoteClient(pair.Key);
                    }
                }

                foreach (KeyValuePair<ulong, PendingClient> pair in PendingClients)
                {
                    if (!disconnectedIds.Contains(pair.Key))
                    {
                        disconnectedIds.Add(pair.Key);
                        if (pair.Key == NetworkConfig.NetworkTransport.ServerClientId)
                        {
                            continue;
                        }

                        DisconnectRemoteClient(pair.Key);
                    }
                }
            }

            if (IsClient && IsConnectedClient)
            {
                // Client only, send disconnect to server
                NetworkConfig.NetworkTransport.DisconnectLocalClient();
            }

            IsConnectedClient = false;
            IsApproved = false;

            // We need to clean up NetworkObjects before we reset the IsServer
            // and IsClient properties. This provides consistency of these two
            // property values for NetworkObjects that are still spawned when
            // the shutdown cycle begins.
            if (SpawnManager != null)
            {
                SpawnManager.DespawnAndDestroyNetworkObjects();
                SpawnManager.ServerResetShudownStateForSceneObjects();

                SpawnManager = null;
            }

            IsServer = false;
            IsClient = false;

            this.UnregisterAllNetworkUpdates();

            if (NetworkTickSystem != null)
            {
                NetworkTickSystem.Tick -= OnNetworkManagerTick;
                NetworkTickSystem = null;
            }

            if (MessagingSystem != null)
            {
                MessagingSystem.Dispose();
                MessagingSystem = null;
            }

            if (NetworkConfig?.NetworkTransport != null)
            {
                NetworkConfig.NetworkTransport.OnTransportEvent -= HandleRawTransportPoll;
            }

            if (DeferredMessageManager != null)
            {
                DeferredMessageManager.CleanupAllTriggers();
            }

            if (SceneManager != null)
            {
                // Let the NetworkSceneManager clean up its two SceneEvenData instances
                SceneManager.Dispose();
                SceneManager = null;
            }

            if (CustomMessagingManager != null)
            {
                CustomMessagingManager = null;
            }

            if (BehaviourUpdater != null)
            {
                BehaviourUpdater = null;
            }

            // This is required for handling the potential scenario where multiple NetworkManager instances are created.
            // See MTT-860 for more information
            if (IsListening)
            {
                //The Transport is set during initialization, thus it is possible for the Transport to be null
                NetworkConfig?.NetworkTransport?.Shutdown();
            }

            m_ClientIdToTransportIdMap.Clear();
            m_TransportIdToClientIdMap.Clear();

            IsListening = false;
            m_ShuttingDown = false;
            m_StopProcessingMessages = false;

            ClearClients();
        }

        /// <inheritdoc />
        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.EarlyUpdate:
                    OnNetworkEarlyUpdate();
                    break;
                case NetworkUpdateStage.PreUpdate:
                    OnNetworkPreUpdate();
                    break;
                case NetworkUpdateStage.PostLateUpdate:
                    OnNetworkPostLateUpdate();
                    break;
            }
        }

        private void ProcessPendingApprovals()
        {
            List<ulong> senders = null;

            foreach (var responsePair in ClientsToApprove)
            {
                var response = responsePair.Value;
                var senderId = responsePair.Key;

                if (!response.Pending)
                {
                    try
                    {
                        HandleConnectionApproval(senderId, response);

                        if (senders == null)
                        {
                            senders = new List<ulong>();
                        }
                        senders.Add(senderId);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            if (senders != null)
            {
                foreach (var sender in senders)
                {
                    ClientsToApprove.Remove(sender);
                }
            }
        }

        private void OnNetworkEarlyUpdate()
        {
            if (!IsListening)
            {
                return;
            }

            ProcessPendingApprovals();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_TransportPoll.Begin();
#endif
            NetworkEvent networkEvent;
            do
            {
                networkEvent = NetworkConfig.NetworkTransport.PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime);
                HandleRawTransportPoll(networkEvent, clientId, payload, receiveTime);
                // Only do another iteration if: there are no more messages AND (there is no limit to max events or we have processed less than the maximum)
            } while (IsListening && networkEvent != NetworkEvent.Nothing);

            MessagingSystem.ProcessIncomingMessageQueue();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_TransportPoll.End();
#endif
        }

        // TODO Once we have a way to subscribe to NetworkUpdateLoop with order we can move this out of NetworkManager but for now this needs to be here because we need strict ordering.
        private void OnNetworkPreUpdate()
        {
            if (IsServer == false && IsConnectedClient == false)
            {
                // As a client wait to run the time system until we are connected.
                return;
            }

            if (m_ShuttingDown && m_StopProcessingMessages)
            {
                return;
            }

            // Only update RTT here, server time is updated by time sync messages
            var reset = NetworkTimeSystem.Advance(Time.unscaledDeltaTime);
            if (reset)
            {
                NetworkTickSystem.Reset(NetworkTimeSystem.LocalTime, NetworkTimeSystem.ServerTime);
            }
            NetworkTickSystem.UpdateTick(NetworkTimeSystem.LocalTime, NetworkTimeSystem.ServerTime);

            if (IsServer == false)
            {
                NetworkTimeSystem.Sync(NetworkTimeSystem.LastSyncedServerTimeSec + Time.unscaledDeltaTime, NetworkConfig.NetworkTransport.GetCurrentRtt(ServerClientId) / 1000d);
            }
        }

        private void OnNetworkPostLateUpdate()
        {

            if (!m_ShuttingDown || !m_StopProcessingMessages)
            {
                MessagingSystem.ProcessSendQueues();
                NetworkMetrics.UpdateNetworkObjectsCount(SpawnManager.SpawnedObjects.Count);
                NetworkMetrics.UpdateConnectionsCount((IsServer) ? ConnectedClients.Count : 1);
                NetworkMetrics.DispatchFrame();

                NetworkObject.VerifyParentingStatus();
            }
            DeferredMessageManager.CleanupStaleTriggers();

            if (m_ShuttingDown)
            {
                ShutdownInternal();
            }
        }

        /// <summary>
        /// This function runs once whenever the local tick is incremented and is responsible for the following (in order):
        /// - collect commands/inputs and send them to the server (TBD)
        /// - call NetworkFixedUpdate on all NetworkBehaviours in prediction/client authority mode
        /// </summary>
        private void OnNetworkManagerTick()
        {
            // Do NetworkVariable updates
            BehaviourUpdater.NetworkBehaviourUpdate(this);

            int timeSyncFrequencyTicks = (int)(k_TimeSyncFrequency * NetworkConfig.TickRate);
            if (IsServer && NetworkTickSystem.ServerTime.Tick % timeSyncFrequencyTicks == 0)
            {
                SyncTime();
            }
        }

        private void SendConnectionRequest()
        {
            var message = new ConnectionRequestMessage
            {
                // Since only a remote client will send a connection request,
                // we should always force the rebuilding of the NetworkConfig hash value
                ConfigHash = NetworkConfig.GetConfig(false),
                ShouldSendConnectionData = NetworkConfig.ConnectionApproval,
                ConnectionData = NetworkConfig.ConnectionData
            };
            SendMessage(ref message, NetworkDelivery.ReliableSequenced, ServerClientId);
        }

        private IEnumerator ApprovalTimeout(ulong clientId)
        {
            var timeStarted = IsServer ? LocalTime.TimeAsFloat : Time.realtimeSinceStartup;
            var timedOut = false;
            var connectionApproved = false;
            var connectionNotApproved = false;
            var timeoutMarker = timeStarted + NetworkConfig.ClientConnectionBufferTimeout;

            while (IsListening && !ShutdownInProgress && !timedOut && !connectionApproved)
            {
                yield return null;
                // Check if we timed out
                timedOut = timeoutMarker < (IsServer ? LocalTime.TimeAsFloat : Time.realtimeSinceStartup);

                if (IsServer)
                {
                    // When the client is no longer in the pending clients list and is in the connected clients list
                    // it has been approved
                    connectionApproved = !PendingClients.ContainsKey(clientId) && ConnectedClients.ContainsKey(clientId);

                    // For the server side, if the client is in neither list then it was declined or the client disconnected
                    connectionNotApproved = !PendingClients.ContainsKey(clientId) && !ConnectedClients.ContainsKey(clientId);
                }
                else
                {
                    connectionApproved = IsApproved;
                }
            }

            // Exit coroutine if we are no longer listening or a shutdown is in progress (client or server)
            if (!IsListening || ShutdownInProgress)
            {
                yield break;
            }

            // If the client timed out or was not approved
            if (timedOut || connectionNotApproved)
            {
                // Timeout
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                {
                    if (timedOut)
                    {
                        if (IsServer)
                        {
                            // Log a warning that the transport detected a connection but then did not receive a follow up connection request message.
                            // (hacking or something happened to the server's network connection)
                            NetworkLog.LogWarning($"Server detected a transport connection from Client-{clientId}, but timed out waiting for the connection request message.");
                        }
                        else
                        {
                            // We only provide informational logging for the client side
                            NetworkLog.LogInfo("Timed out waiting for the server to approve the connection request.");
                        }
                    }
                    else if (connectionNotApproved)
                    {
                        NetworkLog.LogInfo($"Client-{clientId} was either denied approval or disconnected while being approved.");
                    }
                }

                if (IsServer)
                {
                    DisconnectClient(clientId);
                }
                else
                {
                    Shutdown(true);
                }
            }
        }

        internal ulong TransportIdToClientId(ulong transportId)
        {
            return transportId == m_ServerTransportId ? ServerClientId : m_TransportIdToClientIdMap[transportId];
        }

        internal ulong ClientIdToTransportId(ulong clientId)
        {
            return clientId == ServerClientId ? m_ServerTransportId : m_ClientIdToTransportIdMap[clientId];
        }

        private void HandleRawTransportPoll(NetworkEvent networkEvent, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            var transportId = clientId;
            switch (networkEvent)
            {
                case NetworkEvent.Connect:
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    s_TransportConnect.Begin();
#endif

                    // Assumptions:
                    // - When server receives a connection, it *must be* a client
                    // - When client receives one, it *must be* the server
                    // Client's can't connect to or talk to other clients.
                    // Server is a sentinel so only one exists, if we are server, we can't be
                    // connecting to it.
                    if (IsServer)
                    {
                        clientId = m_NextClientId++;
                    }
                    else
                    {
                        clientId = ServerClientId;
                    }
                    m_ClientIdToTransportIdMap[clientId] = transportId;
                    m_TransportIdToClientIdMap[transportId] = clientId;

                    MessagingSystem.ClientConnected(clientId);
                    if (IsServer)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                        {
                            NetworkLog.LogInfo("Client Connected");
                        }

                        PendingClients.Add(clientId, new PendingClient()
                        {
                            ClientId = clientId,
                            ConnectionState = PendingClient.State.PendingConnection
                        });

                        StartCoroutine(ApprovalTimeout(clientId));
                    }
                    else
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                        {
                            NetworkLog.LogInfo("Connected");
                        }

                        SendConnectionRequest();
                        StartCoroutine(ApprovalTimeout(clientId));
                    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    s_TransportConnect.End();
#endif
                    break;
                case NetworkEvent.Data:
                    {
                        clientId = TransportIdToClientId(clientId);

                        HandleIncomingData(clientId, payload, receiveTime);
                        break;
                    }
                case NetworkEvent.Disconnect:
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    s_TransportDisconnect.Begin();
#endif
                    clientId = TransportIdCleanUp(clientId, transportId);

                    if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                    {
                        NetworkLog.LogInfo($"Disconnect Event From {clientId}");
                    }

                    OnClientDisconnectCallback?.Invoke(clientId);

                    if (IsServer)
                    {
                        OnClientDisconnectFromServer(clientId);
                    }
                    else
                    {
                        // We must pass true here and not process any sends messages
                        // as we are no longer connected and thus there is no one to
                        // send any messages to and this will cause an exception within
                        // UnityTransport as the client ID is no longer valid.
                        Shutdown(true);
                    }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    s_TransportDisconnect.End();
#endif
                    break;

                case NetworkEvent.TransportFailure:
                    Debug.LogError($"Shutting down due to network transport failure of {NetworkConfig.NetworkTransport.GetType().Name}!");
                    OnTransportFailure?.Invoke();
                    Shutdown(true);
                    break;
            }
        }

        /// <summary>
        /// Handles cleaning up the transport id/client id tables after
        /// receiving a disconnect event from transport
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong TransportIdCleanUp(ulong clientId, ulong transportId)
        {
            // This check is for clients that attempted to connect but failed.
            // When this happens, the client will not have an entry within the
            // m_TransportIdToClientIdMap or m_ClientIdToTransportIdMap lookup
            // tables so we exit early and just return 0 to be used for the
            // disconnect event.
            if (!IsServer && !m_TransportIdToClientIdMap.ContainsKey(clientId))
            {
                return 0;
            }

            clientId = TransportIdToClientId(clientId);

            m_TransportIdToClientIdMap.Remove(transportId);
            m_ClientIdToTransportIdMap.Remove(clientId);

            return clientId;
        }

        internal unsafe int SendMessage<TMessageType, TClientIdListType>(ref TMessageType message, NetworkDelivery delivery, in TClientIdListType clientIds)
            where TMessageType : INetworkMessage
            where TClientIdListType : IReadOnlyList<ulong>
        {
            // Prevent server sending to itself
            if (IsServer)
            {
                ulong* nonServerIds = stackalloc ulong[clientIds.Count];
                int newIdx = 0;
                for (int idx = 0; idx < clientIds.Count; ++idx)
                {
                    if (clientIds[idx] == ServerClientId)
                    {
                        continue;
                    }

                    nonServerIds[newIdx++] = clientIds[idx];
                }

                if (newIdx == 0)
                {
                    return 0;
                }
                return MessagingSystem.SendMessage(ref message, delivery, nonServerIds, newIdx);
            }
            // else
            if (clientIds.Count != 1 || clientIds[0] != ServerClientId)
            {
                throw new ArgumentException($"Clients may only send messages to {nameof(ServerClientId)}");
            }

            return MessagingSystem.SendMessage(ref message, delivery, clientIds);
        }

        internal unsafe int SendMessage<T>(ref T message, NetworkDelivery delivery,
            ulong* clientIds, int numClientIds)
            where T : INetworkMessage
        {
            // Prevent server sending to itself
            if (IsServer)
            {
                ulong* nonServerIds = stackalloc ulong[numClientIds];
                int newIdx = 0;
                for (int idx = 0; idx < numClientIds; ++idx)
                {
                    if (clientIds[idx] == ServerClientId)
                    {
                        continue;
                    }

                    nonServerIds[newIdx++] = clientIds[idx];
                }

                if (newIdx == 0)
                {
                    return 0;
                }
                return MessagingSystem.SendMessage(ref message, delivery, nonServerIds, newIdx);
            }
            // else
            if (numClientIds != 1 || clientIds[0] != ServerClientId)
            {
                throw new ArgumentException($"Clients may only send messages to {nameof(ServerClientId)}");
            }

            return MessagingSystem.SendMessage(ref message, delivery, clientIds, numClientIds);
        }

        internal unsafe int SendMessage<T>(ref T message, NetworkDelivery delivery, in NativeArray<ulong> clientIds)
            where T : INetworkMessage
        {
            return SendMessage(ref message, delivery, (ulong*)clientIds.GetUnsafePtr(), clientIds.Length);
        }

        internal int SendMessage<T>(ref T message, NetworkDelivery delivery, ulong clientId)
            where T : INetworkMessage
        {
            // Prevent server sending to itself
            if (IsServer && clientId == ServerClientId)
            {
                return 0;
            }

            if (!IsServer && clientId != ServerClientId)
            {
                throw new ArgumentException($"Clients may only send messages to {nameof(ServerClientId)}");
            }
            return MessagingSystem.SendMessage(ref message, delivery, clientId);
        }

        internal int SendPreSerializedMessage<T>(in FastBufferWriter writer, int maxSize, ref T message, NetworkDelivery delivery, ulong clientId)
            where T : INetworkMessage
        {
            return MessagingSystem.SendPreSerializedMessage(writer, maxSize, ref message, delivery, clientId);
        }

        internal void HandleIncomingData(ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleIncomingData.Begin();
#endif

            MessagingSystem.HandleIncomingData(clientId, payload, receiveTime);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_HandleIncomingData.End();
#endif
        }

        /// <summary>
        /// Disconnects the remote client.
        /// </summary>
        /// <param name="clientId">The ClientId to disconnect</param>
        public void DisconnectClient(ulong clientId)
        {
            if (!IsServer)
            {
                throw new NotServerException($"Only server can disconnect remote clients. Please use `{nameof(Shutdown)}()` instead.");
            }

            OnClientDisconnectFromServer(clientId);
            DisconnectRemoteClient(clientId);
        }

        private void OnClientDisconnectFromServer(ulong clientId)
        {
            PendingClients.Remove(clientId);

            if (ConnectedClients.TryGetValue(clientId, out NetworkClient networkClient))
            {
                if (IsServer)
                {
                    var playerObject = networkClient.PlayerObject;
                    if (playerObject != null)
                    {
                        if (!playerObject.DontDestroyWithOwner)
                        {
                            if (PrefabHandler.ContainsHandler(ConnectedClients[clientId].PlayerObject.GlobalObjectIdHash))
                            {
                                PrefabHandler.HandleNetworkPrefabDestroy(ConnectedClients[clientId].PlayerObject);
                            }
                            else
                            {
                                Destroy(playerObject.gameObject);
                            }
                        }
                        else
                        {
                            playerObject.RemoveOwnership();
                        }
                    }

                    // Get the NetworkObjects owned by the disconnected client
                    var clientOwnedObjects = SpawnManager.GetClientOwnedObjects(clientId);
                    if (clientOwnedObjects == null)
                    {
                        // This could happen if a client is never assigned a player object and is disconnected
                        // Only log this in verbose/developer mode
                        if (LogLevel == LogLevel.Developer)
                        {
                            NetworkLog.LogWarning($"ClientID {clientId} disconnected with (0) zero owned objects!  Was a player prefab not assigned?");
                        }
                    }
                    else
                    {
                        // Handle changing ownership and prefab handlers
                        for (int i = clientOwnedObjects.Count - 1; i >= 0; i--)
                        {
                            var ownedObject = clientOwnedObjects[i];
                            if (ownedObject != null)
                            {
                                if (!ownedObject.DontDestroyWithOwner)
                                {
                                    if (PrefabHandler.ContainsHandler(clientOwnedObjects[i].GlobalObjectIdHash))
                                    {
                                        PrefabHandler.HandleNetworkPrefabDestroy(clientOwnedObjects[i]);
                                    }
                                    else
                                    {
                                        Destroy(ownedObject.gameObject);
                                    }
                                }
                                else
                                {
                                    ownedObject.RemoveOwnership();
                                }
                            }
                        }
                    }

                    // TODO: Could(should?) be replaced with more memory per client, by storing the visibility
                    foreach (var sobj in SpawnManager.SpawnedObjectsList)
                    {
                        sobj.Observers.Remove(clientId);
                    }
                }

                for (int i = 0; i < ConnectedClientsList.Count; i++)
                {
                    if (ConnectedClientsList[i].ClientId == clientId)
                    {
                        m_ConnectedClientsList.RemoveAt(i);
                        break;
                    }
                }

                for (int i = 0; i < ConnectedClientsIds.Count; i++)
                {
                    if (ConnectedClientsIds[i] == clientId)
                    {
                        m_ConnectedClientIds.RemoveAt(i);
                        break;
                    }
                }

                m_ConnectedClients.Remove(clientId);
            }
            MessagingSystem.ClientDisconnected(clientId);
        }

        private void SyncTime()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_SyncTime.Begin();
#endif
            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo("Syncing Time To Clients");
            }

            var message = new TimeSyncMessage
            {
                Tick = NetworkTickSystem.ServerTime.Tick
            };
            SendMessage(ref message, NetworkDelivery.Unreliable, ConnectedClientsIds);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_SyncTime.End();
#endif
        }

        /// <summary>
        /// Server Side: Handles the approval of a client
        /// </summary>
        /// <param name="ownerClientId">The Network Id of the client being approved</param>
        /// <param name="response">The response to allow the player in or not, with its parameters</param>
        internal void HandleConnectionApproval(ulong ownerClientId, ConnectionApprovalResponse response)
        {
            if (response.Approved)
            {
                // Inform new client it got approved
                PendingClients.Remove(ownerClientId);

                var client = new NetworkClient { ClientId = ownerClientId, };
                m_ConnectedClients.Add(ownerClientId, client);
                m_ConnectedClientsList.Add(client);
                m_ConnectedClientIds.Add(client.ClientId);

                if (response.CreatePlayerObject)
                {
                    var playerPrefabHash = response.PlayerPrefabHash ?? NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;

                    // Generate a SceneObject for the player object to spawn
                    var sceneObject = new NetworkObject.SceneObject
                    {
                        Header = new NetworkObject.SceneObject.HeaderData
                        {
                            IsPlayerObject = true,
                            OwnerClientId = ownerClientId,
                            IsSceneObject = false,
                            HasTransform = true,
                            Hash = playerPrefabHash,
                        },
                        TargetClientId = ownerClientId,
                        Transform = new NetworkObject.SceneObject.TransformData
                        {
                            Position = response.Position.GetValueOrDefault(),
                            Rotation = response.Rotation.GetValueOrDefault()
                        }
                    };

                    // Create the player NetworkObject locally
                    var networkObject = SpawnManager.CreateLocalNetworkObject(sceneObject);

                    // Spawn the player NetworkObject locally
                    SpawnManager.SpawnNetworkObjectLocally(
                        networkObject,
                        SpawnManager.GetNetworkObjectId(),
                        sceneObject: false,
                        playerObject: true,
                        ownerClientId,
                        destroyWithScene: false);

                    ConnectedClients[ownerClientId].PlayerObject = networkObject;
                }

                // Server doesn't send itself the connection approved message
                if (ownerClientId != ServerClientId)
                {
                    var message = new ConnectionApprovedMessage
                    {
                        OwnerClientId = ownerClientId,
                        NetworkTick = LocalTime.Tick
                    };
                    if (!NetworkConfig.EnableSceneManagement)
                    {
                        if (SpawnManager.SpawnedObjectsList.Count != 0)
                        {
                            message.SpawnedObjectsList = SpawnManager.SpawnedObjectsList;
                        }
                    }

                    SendMessage(ref message, NetworkDelivery.ReliableFragmentedSequenced, ownerClientId);

                    for (int index = 0; index < MessagingSystem.MessageHandlers.Length; index++)
                    {
                        if (MessagingSystem.MessageTypes[index] != null)
                        {
                            var orderingMessage = new OrderingMessage
                            {
                                Order = index,
                                Hash = XXHash.Hash32(MessagingSystem.MessageTypes[index].FullName)
                            };

                            SendMessage(ref orderingMessage, NetworkDelivery.ReliableFragmentedSequenced, ownerClientId);
                        }
                    }

                    // If scene management is enabled, then let NetworkSceneManager handle the initial scene and NetworkObject synchronization
                    if (!NetworkConfig.EnableSceneManagement)
                    {
                        InvokeOnClientConnectedCallback(ownerClientId);
                    }
                    else
                    {
                        SceneManager.SynchronizeNetworkObjects(ownerClientId);
                    }
                }
                else // Server just adds itself as an observer to all spawned NetworkObjects
                {
                    LocalClient = client;
                    SpawnManager.UpdateObservedNetworkObjects(ownerClientId);
                    InvokeOnClientConnectedCallback(ownerClientId);
                }

                if (!response.CreatePlayerObject || (response.PlayerPrefabHash == null && NetworkConfig.PlayerPrefab == null))
                {
                    return;
                }

                // Separating this into a contained function call for potential further future separation of when this notification is sent.
                ApprovedPlayerSpawn(ownerClientId, response.PlayerPrefabHash ?? NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash);
            }
            else
            {
                PendingClients.Remove(ownerClientId);
                DisconnectRemoteClient(ownerClientId);
            }
        }

        /// <summary>
        /// Spawns the newly approved player
        /// </summary>
        /// <param name="clientId">new player client identifier</param>
        /// <param name="playerPrefabHash">the prefab GlobalObjectIdHash value for this player</param>
        internal void ApprovedPlayerSpawn(ulong clientId, uint playerPrefabHash)
        {
            foreach (var clientPair in ConnectedClients)
            {
                if (clientPair.Key == clientId ||
                    clientPair.Key == ServerClientId || // Server already spawned it
                    ConnectedClients[clientId].PlayerObject == null ||
                    !ConnectedClients[clientId].PlayerObject.Observers.Contains(clientPair.Key))
                {
                    continue; //The new client.
                }

                var message = new CreateObjectMessage
                {
                    ObjectInfo = ConnectedClients[clientId].PlayerObject.GetMessageSceneObject(clientPair.Key)
                };
                message.ObjectInfo.Header.Hash = playerPrefabHash;
                message.ObjectInfo.Header.IsSceneObject = false;
                message.ObjectInfo.Header.HasParent = false;
                message.ObjectInfo.Header.IsPlayerObject = true;
                message.ObjectInfo.Header.OwnerClientId = clientId;
                var size = SendMessage(ref message, NetworkDelivery.ReliableFragmentedSequenced, clientPair.Key);
                NetworkMetrics.TrackObjectSpawnSent(clientPair.Key, ConnectedClients[clientId].PlayerObject, size);
            }
        }
    }
}
