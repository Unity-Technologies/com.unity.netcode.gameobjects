using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
using Unity.Profiling;
#endif

namespace Unity.Netcode
{
    /// <summary>
    /// Default coroutine-based behaviour for handling PendingClient handshake timeouts.
    /// </summary>
    /// <seealso cref="ConnectionManager.SetApprovalTimeout"/>
    public class DefaultConnectionApprovalTimeout
    {
        private NetworkManager m_NetworkManager;

        /// <summary>
        /// Constructs a connection approval timeout handler.
        /// </summary>
        /// <param name="networkManager">The NetworkManager which contains the relevant NetworkConfig and ConnectionManager instances.</param>
        public DefaultConnectionApprovalTimeout(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        /// <summary>
        /// Starts a couroutine to monitor a given clientId for removal & disconnect if it does not complete connection
        /// handshake by the configured timeout.
        ///
        /// In the default case this method will exit without any effect when <see cref="ConnectionManager"/> removes
        /// the client from the <see cref="ConnectionManager.PendingClients"/> list upon connection approval or rejection.
        /// </summary>
        public void StartApprovalTimeout(ulong clientId)
        {
            m_NetworkManager.StartCoroutine(ApprovalTimeoutCoroutine(clientId));
        }

        private IEnumerator ApprovalTimeoutCoroutine(ulong clientId)
        {
            NetworkTime timeStarted = m_NetworkManager.LocalTime;

            //We yield every frame incase a pending client disconnects and someone else gets its connection id
            while ((m_NetworkManager.LocalTime - timeStarted).Time < m_NetworkManager.NetworkConfig.ClientConnectionBufferTimeout &&
                m_NetworkManager.ConnectionManager.PendingClients.ContainsKey(clientId))
            {
                yield return null;
            }

            if (m_NetworkManager.ConnectionManager.PendingClients.ContainsKey(clientId) &&
                !m_NetworkManager.ConnectionManager.ConnectedClients.ContainsKey(clientId))
            {
                // Timeout
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"Client {clientId} Handshake Timed Out");
                }

                m_NetworkManager.ConnectionManager.DisconnectClient(clientId);
            }
        }
    }

    public class ConnectionManager
    {
        public const ulong ServerClientId = 0;
        public delegate void SetApprovalTimeoutDelegate(ulong clientId);

        internal delegate void RemoveOwnedObjectsDelegate(NetworkClient client);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static ProfilerMarker s_TransportConnect = new ProfilerMarker($"{nameof(ConnectionManager)}.TransportConnect");
        private static ProfilerMarker s_TransportDisconnect = new ProfilerMarker($"{nameof(ConnectionManager)}.TransportDisconnect");
#endif

        public readonly ReadOnlyDictionary<ulong, PendingClient> PendingClients;
        /// <summary>
        /// Delegate function used to initialize a timeout process for pending client connections
        /// </summary>
        public SetApprovalTimeoutDelegate SetApprovalTimeout;

        /// <summary>
        /// Handler for cleanup (deletion or modification) of a NetworkClient's owned objects during disconnect.
        /// </summary>
        internal RemoveOwnedObjectsDelegate RemoveOwnedObjects;

        internal bool IsServer { private get; set; }
        internal NetworkTransport Transport { private get; set; }

        private Dictionary<ulong, NetworkClient> m_ConnectedClients = new Dictionary<ulong, NetworkClient>();
        private List<NetworkClient> m_ConnectedClientsList = new List<NetworkClient>();
        private List<ulong> m_ConnectedClientIds = new List<ulong>();

        private ulong m_NextClientId = 1;
        private Dictionary<ulong, ulong> m_ClientIdToTransportIdMap = new Dictionary<ulong, ulong>();
        private Dictionary<ulong, ulong> m_TransportIdToClientIdMap = new Dictionary<ulong, ulong>();

        /// <summary>
        /// Gets a dictionary of the clients that have been accepted by the transport but are still pending by the Netcode. This is only populated on the server.
        /// </summary>
        private readonly Dictionary<ulong, PendingClient> m_PendingClients = new Dictionary<ulong, PendingClient>();

        /// <summary>
        /// Invoked when <see cref="NetworkEvent.Connect"/> is received and a client enters the pending stage. Server and Pending Client only.
        /// </summary>
        public event Action<ulong> OnClientConnectionPendingCallback;

        /// <summary>
        /// The callback to invoke once a client completes the full connection process and initial state sync. Server and Connecting Client only.
        /// </summary>
        /// <remarks>If scene management is enabled, this callback is only invoked once scene data has been fully synchronized.</remarks>
        public event Action<ulong> OnClientConnectedCallback;

        /// <summary>
        /// The callback to invoke when a client disconnects. This callback is only ran on the server and on the local client that disconnects.
        /// </summary>
        public event Action<ulong> OnBeforeDisconnectCallback;

        /// <summary>
        /// The callback to invoke after a connection has been terminated. Server and Local Client only.
        /// </summary>
        internal event Action<ulong> OnAfterDisconnectCallback;

        /// <summary>
        /// Gets a dictionary of connected clients and their clientId keys. This is only accessible on the server.
        /// </summary>
        public IReadOnlyDictionary<ulong, NetworkClient> ConnectedClients
        {
            get
            {
                if (!IsServer)
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
                if (!IsServer)
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
                if (!IsServer)
                {
                    throw new NotServerException($"{nameof(m_ConnectedClientIds)} should only be accessed on server.");
                }
                return m_ConnectedClientIds;
            }
        }

        /// <summary>
        /// Gets the networkId of the server
        /// </summary>
        private ulong serverTransportId
        {
            get
            {
                if (Transport != null)
                {
                    return Transport.ServerClientId;
                }

                throw new NullReferenceException($"No transport configured in {nameof(ConnectionManager)}.");
            }
        }

        /// <summary>
        /// Creates a <see cref="ConnectionManager"/> with no default configuration.
        /// </summary>
        /// <remarks>Callers will typically want to set <see cref="SetApprovalTimeout"/>, <see cref="RemoveOwnedObjects"/>, <see cref="IsServer"/>, and <see cref="Transport"/>.</remarks>
        public ConnectionManager()
        {
            PendingClients = new ReadOnlyDictionary<ulong, PendingClient>(m_PendingClients);
        }

        public void Shutdown()
        {
            if (IsServer)
            {
                foreach (var connectedClient in m_ConnectedClients)
                {
                    if (connectedClient.Key == ServerClientId)
                    {
                        continue;
                    }

                    DisconnectClientTransport(connectedClient.Key);
                }

                foreach (var pendingClient in m_PendingClients)
                {
                    DisconnectClientTransport(pendingClient.Key);
                }
            }

            m_ConnectedClients.Clear();
            m_ConnectedClientsList.Clear();
            m_ConnectedClientIds.Clear();
            m_PendingClients.Clear();
        }

        /// <summary>
        /// Disconnects the remote client from transport and removes the client from any connected client lists.
        /// </summary>
        /// <param name="clientId">The ClientId to disconnect</param>
        public void DisconnectClient(ulong clientId)
        {
            if (!IsServer)
            {
                throw new NotServerException("Only server can disconnect remote clients. Use StopClient instead.");
            }

            if (ConnectedClients.TryGetValue(clientId, out NetworkClient client) && RemoveOwnedObjects != null)
            {
                RemoveOwnedObjects(client);
            }
            else
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo("Disconnect called for unknown client ID");
                }
            }

            RemoveConnectedClient(clientId);
            DisconnectClientTransport(clientId);
            SafeMultiInvoke(OnAfterDisconnectCallback, clientId);
        }

        public NetworkClient ApprovePendingClient(ulong clientId)
        {
            m_PendingClients.Remove(clientId);

            var client = new NetworkClient { ClientId = clientId };
            m_ConnectedClients.Add(clientId, client);
            m_ConnectedClientsList.Add(client);
            m_ConnectedClientIds.Add(client.ClientId);

            return client;
        }

        /// <summary>
        /// Remove the client from the pending clients list and close their connection
        /// </summary>
        public void RejectPendingClient(ulong clientId)
        {
            m_PendingClients.Remove(clientId);
            DisconnectClientTransport(clientId);
        }

        /// <summary>
        /// Processes the <see cref="NetworkEvent.Connect"/> event, adding the client to the pending clients list and
        /// initiating a connection approval timeout.
        /// </summary>
        internal void HandleConnectEvent(ulong transportId, bool isServer, NetworkConfig networkConfig, MessagingSystem messagingSystem)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_TransportConnect.Begin();
#endif

            ulong clientId = m_NextClientId++;
            m_ClientIdToTransportIdMap[clientId] = transportId;
            m_TransportIdToClientIdMap[transportId] = clientId;

            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo("Connection Established. Client approval pending.");
            }

            if (isServer)
            {
                m_PendingClients.Add(clientId, new PendingClient()
                {
                    ClientId = clientId,
                    ConnectionState = PendingClient.State.PendingConnection
                });
            }

            if (SetApprovalTimeout != null)
            {
                SetApprovalTimeout(clientId);
            }

            try
            {
                SafeMultiInvoke(OnClientConnectionPendingCallback, clientId);
            }
            catch (Exception e)
            {
                NetworkLog.LogError($"{nameof(OnClientConnectionPendingCallback)} callback failed.\n{e}");
            }

            if (!isServer)
            {
                var message = new ConnectionRequestMessage
                {
                    ConfigHash = networkConfig.GetConfig(),
                    ShouldSendConnectionData = networkConfig.ConnectionApproval,
                    ConnectionData = networkConfig.ConnectionData
                };
                messagingSystem.SendMessage(ref message, NetworkDelivery.ReliableSequenced, ServerClientId);
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_TransportConnect.End();
#endif
        }

        /// <summary>
        /// Processes the <see cref="NetworkEvent.Disconnect"/> event, removing client id and transport mappings.
        /// </summary>
        internal void HandleDisconnectEvent(ulong transportId, bool isServer)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_TransportDisconnect.Begin();
#endif
            ulong clientId = TransportIdToClientId(transportId);

            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo($"Disconnect Event From {clientId}");
            }

            try
            {
                SafeMultiInvoke(OnBeforeDisconnectCallback, clientId);
            }
            catch (Exception e)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogWarning(e.ToString());
                }
            }

            m_TransportIdToClientIdMap.Remove(transportId);
            m_ClientIdToTransportIdMap.Remove(clientId);

            if (ConnectedClients.TryGetValue(clientId, out NetworkClient client) && RemoveOwnedObjects != null)
            {
                RemoveOwnedObjects(client);
            }

            if (IsServer)
            {
                RemoveConnectedClient(clientId);
            }

            SafeMultiInvoke(OnAfterDisconnectCallback, clientId);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_TransportDisconnect.End();
#endif
        }

        internal ulong ClientIdToTransportId(ulong clientId)
        {
            return clientId == ServerClientId ? serverTransportId : m_ClientIdToTransportIdMap[clientId];
        }

        internal ulong TransportIdToClientId(ulong transportId)
        {
            return transportId == serverTransportId ? ServerClientId : m_TransportIdToClientIdMap[transportId];
        }

        // TODO This one probably needs a refactor/rethink, but in the spirit of avoiding breaking changes right now, keeping it as is
        internal void InvokeOnClientConnectedCallback(ulong clientId) => SafeMultiInvoke(OnClientConnectedCallback, clientId);

        /// <summary>
        /// Removes a clientId from the ConnectedClients lists without disconnecting their transport.
        /// </summary>
        private void RemoveConnectedClient(ulong clientId)
        {
            m_PendingClients.Remove(clientId);
            m_ConnectedClients.Remove(clientId);

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
        }

        private void DisconnectClientTransport(ulong clientId)
        {
            var transportId = ClientIdToTransportId(clientId);
            if (Transport != null)
            {
                Transport.DisconnectRemoteClient(transportId);
            }
        }

        private static void SafeMultiInvoke(Delegate del, params object[] args)
        {
            if (del == null)
            {
                return;
            }

            var exceptions = new List<Exception>();

            foreach (var handler in del.GetInvocationList())
            {
                try
                {
                    handler.Method.Invoke(handler.Target, args);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
