using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools;
#endif

using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Unity.Netcode
{
    /// <summary>
    /// The main component of the library
    /// </summary>
    [AddComponentMenu("Netcode/Network Manager", -100)]
    public class NetworkManager : MonoBehaviour
    {
        // TODO 2023-Q2: Removing this is a "breaking change"
        // (the issue with interfaces...implementations have to be public!)
        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {

        }

        #region RPC RELATED CODE
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
        #endregion

        #region CONNECTION MANAGEMENT RELATED CODE
        /// <summary>
        /// The client id used to represent the server
        /// </summary>
        public const ulong ServerClientId = 0;

        /// <summary>
        /// Gets the networkId of the server
        /// </summary>
        private ulong m_ServerTransportId => ConnectionManager.ServerTransportId;

        /// <summary>
        /// Returns ServerClientId if IsServer or LocalClientId if not
        /// </summary>
        public ulong LocalClientId
        {
            get
            {
                return ConnectionManager.LocalClient.ClientId;
            }
            internal set
            {
                ConnectionManager.LocalClient.ClientId = value;
            }
        }

        /// <summary>
        /// Gets a dictionary of connected clients and their clientId keys. This is only accessible on the server.
        /// </summary>
        public IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients
        {
            get
            {
                if (IsServer == false)
                {
                    throw new NotServerException($"{nameof(ConnectionManager.ConnectedClients)} should only be accessed on server.");
                }
                return ConnectionManager.ConnectedClients;
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
                    throw new NotServerException($"{nameof(ConnectionManager.ConnectedClientsList)} should only be accessed on server.");
                }
                return ConnectionManager.ConnectedClientsList;
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
                    throw new NotServerException($"{nameof(ConnectionManager.ConnectedClientIds)} should only be accessed on server.");
                }
                return ConnectionManager.ConnectedClientIds;
            }
        }

        /// <summary>
        /// Gets the local <see cref="NetworkClient"/> for this client.
        /// </summary>
        public NetworkClient LocalClient => ConnectionManager.LocalClient;

        /// <summary>
        /// Gets a dictionary of the clients that have been accepted by the transport but are still pending by the Netcode. This is only populated on the server.
        /// </summary>
        /// !!!!!!!!!!!!!!!!!!!!!!!!!!!! (2023-Q2) THIS IS NOT CURRENTLY POPULATED WITH ANYTHING !!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public readonly Dictionary<ulong, PendingClient> PendingClients = new Dictionary<ulong, PendingClient>();

        // TODO-2023-Q2: FIXME!!!! (Below is what we want to make this and should not impact the API)
        // While the API itself remains exactly the same, our package-validation-suite thinks this breaks the API
        //public IReadOnlyDictionary<ulong, PendingClient> PendingClients => ConnectionManager.PendingClients;

        /// <summary>
        /// Gets Whether or not a server is running
        /// </summary>
        public bool IsServer => ConnectionManager.LocalClient.IsServer;

        /// <summary>
        /// Gets Whether or not a client is running
        /// </summary>
        public bool IsClient => ConnectionManager.LocalClient.IsClient;

        /// <summary>
        /// Gets if we are running as host
        /// </summary>
        public bool IsHost => ConnectionManager.LocalClient.IsHost;

        /// <summary>
        /// When disconnected from the server, the server may send a reason. If a reason was sent, this property will
        /// tell client code what the reason was. It should be queried after the OnClientDisconnectCallback is called
        /// </summary>
        public string DisconnectReason => ConnectionManager.DisconnectReason;

        /// <summary>
        /// Is true when a server or host is listening for connections.
        /// Is true when a client is connecting or connected to a network session.
        /// Is false when not listening, connecting, or connected.
        /// </summary>
        public bool IsListening
        {
            get
            {
                return ConnectionManager.IsListening;
            }
            internal set
            {
                ConnectionManager.IsListening = value;
            }
        }

        /// <summary>
        /// When true, the client is connected, approved, and synchronized with
        /// the server.
        /// <see cref="NetworkClient.IsConnected"/> <br />
        /// <see cref="NetworkClient.IsApproved"/> <br />
        /// </summary>
        public bool IsConnectedClient
        {
            get
            {
                return ConnectionManager.LocalClient.IsConnected;
            }
            internal set
            {
                ConnectionManager.LocalClient.IsConnected = value;
            }
        }

        /// <summary>
        /// Is true when the client has been approved.
        /// </summary>
        /// <remarks>
        /// This only reflects the client's approved status and does not mean the client
        /// has finished the connection and synchronization process. The server-host will
        /// always be approved upon being starting the <see cref="NetworkManager"/>
        /// <see cref="NetworkClient.IsConnectedClient"/>
        /// </remarks>
        public bool IsApproved
        {
            get
            {
                return ConnectionManager.LocalClient.IsApproved;
            }
            internal set
            {
                ConnectionManager.LocalClient.IsApproved = value;
            }
        }

        /// <summary>
        /// The callback to invoke if the <see cref="NetworkTransport"/> fails.
        /// </summary>
        /// <remarks>
        /// A failure of the transport is always followed by the <see cref="NetworkManager"/> shutting down. Recovering
        /// from a transport failure would normally entail reconfiguring the transport (e.g. re-authenticating, or
        /// recreating a new service allocation depending on the transport) and restarting the client/server/host.
        /// </remarks>
        public event Action OnTransportFailure
        {
            add
            {
                ConnectionManager.OnTransportFailure += value;
            }

            remove
            {
                ConnectionManager.OnTransportFailure -= value;
            }
        }

        /// <summary>
        /// The callback to invoke during connection approval. Allows client code to decide whether or not to allow incoming client connection
        /// </summary>
        public Action<ConnectionApprovalRequest, ConnectionApprovalResponse> ConnectionApprovalCallback
        {
            get => ConnectionManager.ConnectionApprovalCallback;
            set
            {
                if (value != null && value.GetInvocationList().Length > 1)
                {
                    throw new InvalidOperationException($"Only one {nameof(ConnectionApprovalCallback)} can be registered at a time.");
                }
                else
                {
                    ConnectionManager.ConnectionApprovalCallback = value;
                }
            }
        }

        /// <summary>
        /// The callback to invoke once a client connects. This callback is only ran on the server and on the local client that connects.
        /// </summary>
        public event Action<ulong> OnClientConnectedCallback
        {
            add
            {
                ConnectionManager.OnClientConnectedCallback += value;
            }

            remove
            {
                ConnectionManager.OnClientConnectedCallback -= value;
            }
        }

        /// <summary>
        /// The callback to invoke when a client disconnects. This callback is only ran on the server and on the local client that disconnects.
        /// </summary>
        public event Action<ulong> OnClientDisconnectCallback
        {
            add
            {
                ConnectionManager.OnClientDisconnectCallback += value;
            }

            remove
            {
                ConnectionManager.OnClientDisconnectCallback -= value;
            }
        }

        /// <summary>
        /// Disconnects the remote client.
        /// </summary>
        /// <param name="clientId">The ClientId to disconnect</param>
        public void DisconnectClient(ulong clientId) => ConnectionManager.DisconnectClient(clientId);

        /// <summary>
        /// Disconnects the remote client.
        /// </summary>
        /// <param name="clientId">The ClientId to disconnect</param>
        /// <param name="reason">Disconnection reason. If set, client will receive a DisconnectReasonMessage and have the
        /// reason available in the NetworkManager.DisconnectReason property</param>
        public void DisconnectClient(ulong clientId, string reason = null) => ConnectionManager.DisconnectClient(clientId, reason);

        /// <summary>
        /// The current host name we are connected to, used to validate certificate
        /// </summary>
        public string ConnectedHostname => ConnectionManager.ConnectedHostname;

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

            /// <summary>
            /// Optional reason. If Approved is false, this reason will be sent to the client so they know why they
            /// were not approved.
            /// </summary>
            public string Reason;
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
        #endregion

        #region PREFAB RELATED CODE

        internal static string PrefabDebugHelper(NetworkPrefab networkPrefab)
        {
            return $"{nameof(NetworkPrefab)} \"{networkPrefab.Prefab.name}\"";
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
                if (NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.ContainsKey(networkObject.GlobalObjectIdHash))
                {
                    switch (NetworkConfig.Prefabs.NetworkPrefabOverrideLinks[networkObject.GlobalObjectIdHash].Override)
                    {
                        case NetworkPrefabOverride.Hash:
                        case NetworkPrefabOverride.Prefab:
                            {
                                return NetworkConfig.Prefabs.NetworkPrefabOverrideLinks[networkObject.GlobalObjectIdHash].OverridingTargetPrefab;
                            }
                    }
                }
            }
            return gameObject;
        }

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
            bool added = NetworkConfig.Prefabs.Add(networkPrefab);
            if (IsListening && added)
            {
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
            NetworkConfig.Prefabs.Remove(prefab);
            if (PrefabHandler.ContainsHandler(globalObjectIdHash))
            {
                PrefabHandler.RemoveHandler(globalObjectIdHash);
            }
        }
        #endregion

        /// <summary>
        /// Can be used to determine if the <see cref="NetworkManager"/> is currently shutting itself down
        /// </summary>
        public bool ShutdownInProgress { get { return m_ShuttingDown; } }
        private bool m_ShuttingDown;

        /// <summary>
        /// The current netcode project configuration
        /// </summary>
        [HideInInspector]
        public NetworkConfig NetworkConfig;

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

        #region NGO SYSTEMS
        private NetworkPrefabHandler m_PrefabHandler;

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


        internal NetworkBehaviourUpdater BehaviourUpdater { get; set; }

        /// <summary>
        /// Accessor property for the <see cref="NetworkTimeSystem"/> of the NetworkManager.
        /// Prefer the use of the LocalTime and ServerTime properties
        /// </summary>
        public NetworkTimeSystem NetworkTimeSystem { get; private set; }

        /// <summary>
        /// Accessor property for the <see cref="NetworkTickSystem"/> of the NetworkManager.
        /// </summary>
        public NetworkTickSystem NetworkTickSystem { get; private set; }

        internal INetworkMetrics NetworkMetrics => NetworkMetricsManager.NetworkMetrics;
        internal NetworkMetricsManager NetworkMetricsManager = new NetworkMetricsManager();
        internal NetworkConnectionManager ConnectionManager = new NetworkConnectionManager();
        #endregion

        /// <summary>
        /// This callback is invoked when the local server is started and listening for incoming connections.
        /// </summary>
        public event Action OnServerStarted = null;

        /// <summary>
        /// The callback to invoke once the local client is ready
        /// </summary>
        public event Action OnClientStarted = null;

        /// <summary>
        /// This callback is invoked once the local server is stopped.
        /// </summary>
        /// <param name="arg1">The first parameter of this event will be set to <see cref="true"/> when stopping a host instance and <see cref="false"/> when stopping a server instance.</param>
        public event Action<bool> OnServerStopped = null;

        /// <summary>
        /// The callback to invoke once the local client stops
        /// </summary>
        /// <remarks>The parameter states whether the client was running in host mode</remarks>
        /// <param name="arg1">The first parameter of this event will be set to <see cref="true"/> when stopping the host client and <see cref="false"/> when stopping a standard client instance.</param>
        public event Action<bool> OnClientStopped = null;

        internal static event Action OnSingletonReady;

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

        internal static string GenerateNestedNetworkManagerMessage(Transform transform)
        {
            return $"{transform.name} is nested under {transform.root.name}. NetworkManager cannot be nested.\n";
        }

        #region EDITOR SPECIFIC CODE
#if UNITY_EDITOR
        internal static INetworkManagerHelper NetworkManagerHelper;
        /// <summary>
        /// Interface for NetworkManagerHelper
        /// </summary>
        internal interface INetworkManagerHelper
        {
            bool NotifyUserOfNestedNetworkManager(NetworkManager networkManager, bool ignoreNetworkManagerCache = false, bool editorTest = false);
            void CheckAndNotifyUserNetworkObjectRemoved(NetworkManager networkManager, bool editorTest = false);
        }

        internal delegate void ResetNetworkManagerDelegate(NetworkManager manager);

        internal static ResetNetworkManagerDelegate OnNetworkManagerReset;

        private void Reset()
        {
            OnNetworkManagerReset?.Invoke(this);
        }

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
            NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.Clear();

            var prefabs = NetworkConfig.Prefabs.Prefabs;
            // Check network prefabs and assign to dictionary for quick look up
            for (int i = 0; i < prefabs.Count; i++)
            {
                var networkPrefab = prefabs[i];
                var networkPrefabGo = networkPrefab?.Prefab;
                if (networkPrefabGo == null)
                {
                    continue;
                }

                var networkObject = networkPrefabGo.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogError($"Cannot register {PrefabDebugHelper(networkPrefab)}, it does not have a {nameof(NetworkObject)} component at its root");
                    }

                    continue;
                }

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
            }
        }
#endif
        #endregion

        #region START-UP AND SHUTDOWN CODE
        internal void Initialize(bool server)
        {
            ConnectionManager.Initialize(this);
            // Don't allow the user to start a network session if the NetworkManager is
            // still parented under another GameObject
            if (NetworkManagerCheckForParent(true))
            {
                return;
            }

            ComponentFactory.SetDefaults();

            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo(nameof(Initialize));
            }

            LocalClientId = ulong.MaxValue;

            NetworkTickSystem = new NetworkTickSystem(NetworkConfig.TickRate, 0, 0);
            if (server)
            {
                NetworkTimeSystem = NetworkTimeSystem.ServerTimeSystem();
            }
            else
            {
                NetworkTimeSystem = new NetworkTimeSystem(1.0 / NetworkConfig.TickRate);
            }

            NetworkTimeSystem.InitializeForUpdates(this);

            // Create spawn manager instance
            SpawnManager = new NetworkSpawnManager(this);

            DeferredMessageManager = ComponentFactory.Create<IDeferredMessageManager>(this);

            CustomMessagingManager = new CustomMessagingManager(this);

            SceneManager = new NetworkSceneManager(this);

            BehaviourUpdater = new NetworkBehaviourUpdater(this);

            NetworkMetricsManager.Initialize(this);

            if (NetworkConfig.NetworkTransport == null)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogError("No transport has been selected!");
                }

                return;
            }

            NetworkConfig.NetworkTransport.NetworkMetrics = NetworkMetrics;

            NetworkConfig.InitializePrefabs();

            // If we have a player prefab, then we need to verify it is in the list of NetworkPrefabOverrideLinks for client side spawning.
            if (NetworkConfig.PlayerPrefab != null)
            {
                if (NetworkConfig.PlayerPrefab.TryGetComponent<NetworkObject>(out var playerPrefabNetworkObject))
                {
                    //In the event there is no NetworkPrefab entry (i.e. no override for default player prefab)
                    if (!NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.ContainsKey(playerPrefabNetworkObject
                        .GlobalObjectIdHash))
                    {
                        //Then add a new entry for the player prefab
                        AddNetworkPrefab(NetworkConfig.PlayerPrefab);
                    }
                }
                else
                {
                    // Provide the name of the prefab with issues so the user can more easily find the prefab and fix it
                    Debug.LogError($"{nameof(NetworkConfig.PlayerPrefab)} (\"{NetworkConfig.PlayerPrefab.name}\") has no NetworkObject assigned to it!.");
                }
            }

            if (!NetworkConfig.NetworkTransport.UseTransportPolling())
            {
                NetworkConfig.NetworkTransport.OnTransportEvent += ConnectionManager.HandleNetworkEvent;
            }

            NetworkConfig.NetworkTransport.Initialize(this);
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
            ConnectionManager.LocalClient.SetRole(true, false, this);
            Initialize(true);
            IsListening = true;
            LocalClientId = ServerClientId;

            try
            {
                // If we failed to start then shutdown and notify user that the transport failed to start
                if (NetworkConfig.NetworkTransport.StartServer())
                {
                    SpawnManager.ServerSpawnSceneObjectsOnStartSweep();

                    OnServerStarted?.Invoke();
                    ConnectionManager.LocalClient.IsApproved = true;
                    return true;
                }
                else
                {
                    ConnectionManager.TransportFailureEventHandler(true);
                    ConnectionManager.LocalClient.SetRole(false, false);
                    IsListening = false;
                    Shutdown();
                }
            }
            catch (Exception)
            {
                ConnectionManager.LocalClient.SetRole(false, false);
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
            ConnectionManager.LocalClient.SetRole(false, true, this);

            Initialize(false);


            if (!NetworkConfig.NetworkTransport.StartClient())
            {
                ConnectionManager.TransportFailureEventHandler(true);
                IsListening = false;
                Shutdown();
                return false;
            }

            IsListening = true;

            OnClientStarted?.Invoke();
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
            ConnectionManager.LocalClient.SetRole(true, true, this);

            Initialize(true);

            IsListening = true;

            try
            {
                // If we failed to start then shutdown and notify user that the transport failed to start
                if (!NetworkConfig.NetworkTransport.StartServer())
                {
                    ConnectionManager.TransportFailureEventHandler(true);
                    ConnectionManager.LocalClient.SetRole(false, false);
                    IsListening = false;
                    Shutdown();
                    return false;
                }
            }
            catch (Exception)
            {
                ConnectionManager.LocalClient.SetRole(false, false);
                IsListening = false;
                throw;
            }

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

                ConnectionManager.HandleConnectionApproval(ServerClientId, response);
            }
            else
            {
                var response = new ConnectionApprovalResponse
                {
                    Approved = true,
                    CreatePlayerObject = NetworkConfig.PlayerPrefab != null
                };
                ConnectionManager.HandleConnectionApproval(ServerClientId, response);
            }

            SpawnManager.ServerSpawnSceneObjectsOnStartSweep();

            OnServerStarted?.Invoke();
            OnClientStarted?.Invoke();

            // This assures that any in-scene placed NetworkObject is spawned and
            // any associated NetworkBehaviours' netcode related properties are
            // set prior to invoking OnClientConnected.
            ConnectionManager.InvokeOnClientConnectedCallback(LocalClientId);

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
            NetworkConfig?.InitializePrefabs();

            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        // Ensures that the NetworkManager is cleaned up before OnDestroy is run on NetworkObjects and NetworkBehaviours when quitting the application.
        private void OnApplicationQuit()
        {
            OnDestroy();
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
                ConnectionManager.StopProcessingMessages = discardMessageQueue;
            }

            NetworkConfig.NetworkTransport.OnTransportEvent -= ConnectionManager.HandleNetworkEvent;
        }

        internal void ShutdownInternal()
        {
            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo(nameof(ShutdownInternal));
            }

            ConnectionManager.Shutdown();

            // We need to clean up NetworkObjects before we reset the IsServer
            // and IsClient properties. This provides consistency of these two
            // property values for NetworkObjects that are still spawned when
            // the shutdown cycle begins.
            SpawnManager?.DespawnAndDestroyNetworkObjects();
            SpawnManager?.ServerResetShudownStateForSceneObjects();
            SpawnManager = null;

            bool wasServer = ConnectionManager.LocalClient.IsServer;
            bool wasClient = ConnectionManager.LocalClient.IsClient;
            ConnectionManager.LocalClient.SetRole(false, false);


            if (NetworkConfig?.NetworkTransport != null)
            {
                NetworkConfig.NetworkTransport.OnTransportEvent -= ConnectionManager.HandleNetworkEvent;
            }

            DeferredMessageManager?.CleanupAllTriggers();

            // Let the NetworkSceneManager clean up its two SceneEvenData instances
            SceneManager?.Dispose();
            SceneManager = null;

            CustomMessagingManager = null;

            BehaviourUpdater?.Shutdown();
            BehaviourUpdater = null;

            // Time & tick systems should be the last system shutdown so other systems
            // can unsubscribe from tick updates and such.
            NetworkTimeSystem?.Shutdown();
            NetworkTickSystem = null;

            NetworkMetricsManager?.Shutdown();

            // This is required for handling the potential scenario where multiple NetworkManager instances are created.
            // See MTT-860 for more information
            if (IsListening)
            {
                //The Transport is set during initialization, thus it is possible for the Transport to be null
                NetworkConfig?.NetworkTransport?.Shutdown();
            }

            IsListening = false;
            m_ShuttingDown = false;

            if (wasClient)
            {
                // If we were a client, we want to know if we were a host
                // client or not. (why we pass in "wasServer")
                OnClientStopped?.Invoke(wasServer);
            }
            if (wasServer)
            {
                // If we were a server, we want to know if we were a host
                // or not. (why we pass in "wasClient")
                OnServerStopped?.Invoke(wasClient);
            }

            // This cleans up the internal prefabs list
            NetworkConfig?.Prefabs?.Shutdown();

            // Reset the configuration hash for next session in the event
            // that the prefab list changes
            NetworkConfig?.ClearConfigHash();
        }
        #endregion

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

        // Ensures that the NetworkManager is cleaned up before OnDestroy is run on NetworkObjects and NetworkBehaviours when unloading a scene with a NetworkManager
        private void OnSceneUnloaded(Scene scene)
        {
            if (scene == gameObject.scene)
            {
                OnDestroy();
            }
        }
    }
}
