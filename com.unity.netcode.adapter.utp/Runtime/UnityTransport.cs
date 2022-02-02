using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using NetcodeNetworkEvent = Unity.Netcode.NetworkEvent;
using TransportNetworkEvent = Unity.Networking.Transport.NetworkEvent;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Networking.Transport.Utilities;
using Unity.Netcode.UTP.Utilities;

namespace Unity.Netcode
{
    /// <summary>
    /// Provides an interface that overrides the ability to create your own drivers and pipelines
    /// </summary>
    public interface INetworkStreamDriverConstructor
    {
        void CreateDriver(
            UnityTransport transport,
            out NetworkDriver driver,
            out NetworkPipeline unreliableFragmentedPipeline,
            out NetworkPipeline unreliableSequencedFragmentedPipeline,
            out NetworkPipeline reliableSequencedPipeline);
    }

    public static class ErrorUtilities
    {
        private const string k_NetworkSuccess = "Success";
        private const string k_NetworkIdMismatch = "NetworkId is invalid, likely caused by stale connection {0}.";
        private const string k_NetworkVersionMismatch = "NetworkVersion is invalid, likely caused by stale connection {0}.";
        private const string k_NetworkStateMismatch = "Sending data while connecting on connection {0} is not allowed.";
        private const string k_NetworkPacketOverflow = "Unable to allocate packet due to buffer overflow.";
        private const string k_NetworkSendQueueFull = "Currently unable to queue packet as there is too many in-flight " +
            " packets. This could be because the send queue size ('Max Send Queue Size') is too small.";
        private const string k_NetworkHeaderInvalid = "Invalid Unity Transport Protocol header.";
        private const string k_NetworkDriverParallelForErr = "The parallel network driver needs to process a single unique connection per job, processing a single connection multiple times in a parallel for is not supported.";
        private const string k_NetworkSendHandleInvalid = "Invalid NetworkInterface Send Handle. Likely caused by pipeline send data corruption.";
        private const string k_NetworkArgumentMismatch = "Invalid NetworkEndpoint Arguments.";

        public static string ErrorToString(Networking.Transport.Error.StatusCode error, ulong connectionId)
        {
            switch (error)
            {
                case Networking.Transport.Error.StatusCode.Success:
                    return k_NetworkSuccess;
                case Networking.Transport.Error.StatusCode.NetworkIdMismatch:
                    return string.Format(k_NetworkIdMismatch, connectionId);
                case Networking.Transport.Error.StatusCode.NetworkVersionMismatch:
                    return string.Format(k_NetworkVersionMismatch, connectionId);
                case Networking.Transport.Error.StatusCode.NetworkStateMismatch:
                    return string.Format(k_NetworkStateMismatch, connectionId);
                case Networking.Transport.Error.StatusCode.NetworkPacketOverflow:
                    return k_NetworkPacketOverflow;
                case Networking.Transport.Error.StatusCode.NetworkSendQueueFull:
                    return k_NetworkSendQueueFull;
                case Networking.Transport.Error.StatusCode.NetworkHeaderInvalid:
                    return k_NetworkHeaderInvalid;
                case Networking.Transport.Error.StatusCode.NetworkDriverParallelForErr:
                    return k_NetworkDriverParallelForErr;
                case Networking.Transport.Error.StatusCode.NetworkSendHandleInvalid:
                    return k_NetworkSendHandleInvalid;
                case Networking.Transport.Error.StatusCode.NetworkArgumentMismatch:
                    return k_NetworkArgumentMismatch;
            }

            return $"Unknown ErrorCode {Enum.GetName(typeof(Networking.Transport.Error.StatusCode), error)}";
        }
    }

    public class UnityTransport : NetworkTransport, INetworkStreamDriverConstructor
    {
        public enum ProtocolType
        {
            UnityTransport,
            RelayUnityTransport,
        }

        private enum State
        {
            Disconnected,
            Listening,
            Connected,
        }

        public const int InitialMaxPacketQueueSize = 128;
        public const int InitialMaxPayloadSize = 6 * 1024;
        public const int InitialMaxSendQueueSize = 16 * InitialMaxPayloadSize;

        private static ConnectionAddressData s_DefaultConnectionAddressData = new ConnectionAddressData()
        { Address = "127.0.0.1", Port = 7777, ServerListenAddress = string.Empty };

#pragma warning disable IDE1006 // Naming Styles
        public static INetworkStreamDriverConstructor s_DriverConstructor;
#pragma warning restore IDE1006 // Naming Styles
        public INetworkStreamDriverConstructor DriverConstructor => s_DriverConstructor != null ? s_DriverConstructor : this;

        [Tooltip("Which protocol should be selected (Relay/Non-Relay).")]
        [SerializeField] private ProtocolType m_ProtocolType;

        [Tooltip("The maximum amount of packets that can be in the internal send/receive queues. " +
            "Basically this is how many packets can be sent/received in a single update/frame.")]
        [SerializeField] private int m_MaxPacketQueueSize = InitialMaxPacketQueueSize;

        [Tooltip("The maximum size of a payload that can be handled by the transport.")]
        [FormerlySerializedAs("m_SendQueueBatchSize")]
        [SerializeField] private int m_MaxPayloadSize = InitialMaxPayloadSize;

        [Tooltip("The maximum size in bytes of the transport send queue. The send queue accumulates messages for " +
            "batching and stores messages when other internal send queues are full. If you routinely observe an " +
            "error about too many in-flight packets, try increasing this.")]
        [SerializeField] private int m_MaxSendQueueSize = InitialMaxSendQueueSize;

        [Tooltip("A timeout in milliseconds after which a heartbeat is sent if there is no activity.")]
        [SerializeField] private int m_HeartbeatTimeoutMS = NetworkParameterConstants.HeartbeatTimeoutMS;

        [Tooltip("A timeout in milliseconds indicating how long we will wait until we send a new connection attempt.")]
        [SerializeField] private int m_ConnectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS;

        [Tooltip("The maximum amount of connection attempts we will try before disconnecting.")]
        [SerializeField] private int m_MaxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts;

        [Tooltip("A timeout in milliseconds indicating how long we will wait for a connection event, before we " +
            "disconnect it. The connection needs to receive data from the connected endpoint within this timeout. " +
            "Note that with heartbeats enabled, simply not sending any data will not be enough to trigger this " +
            "timeout (since heartbeats count as connection events).")]
        [SerializeField] private int m_DisconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS;

        [Serializable]
        public struct ConnectionAddressData
        {
            [Tooltip("IP address of the server (address to which clients will connect to).")]
            [SerializeField] public string Address;

            [Tooltip("UDP port of the server.")]
            [SerializeField] public ushort Port;

            [Tooltip("IP address the server will listen on. If not provided, will use 'Address'.")]
            [SerializeField] public string ServerListenAddress;

            private static NetworkEndPoint ParseNetworkEndpoint(string ip, ushort port)
            {
                if (!NetworkEndPoint.TryParse(ip, port, out var endpoint))
                {
                    Debug.LogError($"Invalid network endpoint: {ip}:{port}.");
                    return default;
                }

                return endpoint;
            }

            public NetworkEndPoint ServerEndPoint => ParseNetworkEndpoint(Address, Port);

            public NetworkEndPoint ListenEndPoint => ParseNetworkEndpoint(
                (ServerListenAddress == string.Empty) ? Address : ServerListenAddress, Port);

            [Obsolete("Use ServerEndPoint or ListenEndPoint properties instead.")]
            public static implicit operator NetworkEndPoint(ConnectionAddressData d) =>
                ParseNetworkEndpoint(d.Address, d.Port);

            [Obsolete("Construct manually from NetworkEndPoint.Address and NetworkEndPoint.Port instead.")]
            public static implicit operator ConnectionAddressData(NetworkEndPoint d) =>
                new ConnectionAddressData() { Address = d.Address.Split(':')[0], Port = d.Port, ServerListenAddress = string.Empty };
        }

        public ConnectionAddressData ConnectionData = s_DefaultConnectionAddressData;

        private State m_State = State.Disconnected;
        private NetworkDriver m_Driver;
        private NetworkSettings m_NetworkSettings;
        private byte[] m_MessageBuffer;
        private NetworkConnection m_ServerConnection;
        private ulong m_ServerClientId;

        private NetworkPipeline m_UnreliableFragmentedPipeline;
        private NetworkPipeline m_UnreliableSequencedFragmentedPipeline;
        private NetworkPipeline m_ReliableSequencedPipeline;

        public override ulong ServerClientId => m_ServerClientId;

        public ProtocolType Protocol => m_ProtocolType;

        private RelayServerData m_RelayServerData;

#if UNITY_EDITOR
        private static int ClientPacketDelayMs => UnityEditor.EditorPrefs.GetInt($"NetcodeGameObjects_{Application.productName}_ClientDelay");
        private static int ClientPacketJitterMs => UnityEditor.EditorPrefs.GetInt($"NetcodeGameObjects_{Application.productName}_ClientJitter");
        private static int ClientPacketDropRate => UnityEditor.EditorPrefs.GetInt($"NetcodeGameObjects_{Application.productName}_ClientDropRate");
#elif DEVELOPMENT_BUILD
        public static int ClientPacketDelayMs = 0;
        public static int ClientPacketJitterMs = 0;
        public static int ClientPacketDropRate = 0;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public SimulatorUtility.Parameters ClientSimulatorParameters
        {
            get
            {
                var packetDelay = ClientPacketDelayMs;
                var jitter = ClientPacketJitterMs;
                if (jitter > packetDelay)
                {
                    jitter = packetDelay;
                }

                var packetDrop = ClientPacketDropRate;
                int networkRate = 60; // TODO: read from some better place
                // All 3 packet types every frame stored for maximum delay, doubled for safety margin
                int maxPackets = 2 * (networkRate * 3 * packetDelay + 999) / 1000;
                return new SimulatorUtility.Parameters
                {
                    MaxPacketSize = NetworkParameterConstants.MTU,
                    MaxPacketCount = maxPackets,
                    PacketDelayMs = packetDelay,
                    PacketJitterMs = jitter,
                    PacketDropPercentage = packetDrop
                };
            }
        }
#endif

        /// <summary>
        /// SendQueue dictionary is used to batch events instead of sending them immediately.
        /// </summary>
        private readonly Dictionary<SendTarget, BatchedSendQueue> m_SendQueue = new Dictionary<SendTarget, BatchedSendQueue>();

        // Since reliable messages may be spread out over multiple transport payloads, it's possible
        // to receive only parts of a message in an update. We thus keep the reliable receive queues
        // around to avoid losing partial messages.
        private readonly Dictionary<ulong, BatchedReceiveQueue> m_ReliableReceiveQueues = new Dictionary<ulong, BatchedReceiveQueue>();

        private void InitDriver()
        {
            DriverConstructor.CreateDriver(
                this,
                out m_Driver,
                out m_UnreliableFragmentedPipeline,
                out m_UnreliableSequencedFragmentedPipeline,
                out m_ReliableSequencedPipeline);
        }

        private void DisposeInternals()
        {
            if (m_Driver.IsCreated)
            {
                m_Driver.Dispose();
            }

            m_NetworkSettings.Dispose();

            foreach (var queue in m_SendQueue.Values)
            {
                queue.Dispose();
            }

            m_SendQueue.Clear();
        }

        private NetworkPipeline SelectSendPipeline(NetworkDelivery delivery)
        {
            switch (delivery)
            {
                case NetworkDelivery.Unreliable:
                    return m_UnreliableFragmentedPipeline;

                case NetworkDelivery.UnreliableSequenced:
                    return m_UnreliableSequencedFragmentedPipeline;

                case NetworkDelivery.Reliable:
                case NetworkDelivery.ReliableSequenced:
                case NetworkDelivery.ReliableFragmentedSequenced:
                    return m_ReliableSequencedPipeline;

                default:
                    Debug.LogError($"Unknown {nameof(NetworkDelivery)} value: {delivery}");
                    return NetworkPipeline.Null;
            }
        }

        private bool ClientBindAndConnect()
        {
            var serverEndpoint = default(NetworkEndPoint);

            if (m_ProtocolType == ProtocolType.RelayUnityTransport)
            {
                //This comparison is currently slow since RelayServerData does not implement a custom comparison operator that doesn't use
                //reflection, but this does not live in the context of a performance-critical loop, it runs once at initial connection time.
                if (m_RelayServerData.Equals(default(RelayServerData)))
                {
                    Debug.LogError("You must call SetRelayServerData() at least once before calling StartRelayServer.");
                    return false;
                }

                m_NetworkSettings.WithRelayParameters(ref m_RelayServerData);
            }
            else
            {
                serverEndpoint = ConnectionData.ServerEndPoint;
            }

            InitDriver();

            int result = m_Driver.Bind(NetworkEndPoint.AnyIpv4);
            if (result != 0)
            {
                Debug.LogError("Client failed to bind");
                return false;
            }

            m_ServerConnection = m_Driver.Connect(serverEndpoint);
            m_ServerClientId = ParseClientId(m_ServerConnection);

            return true;
        }

        private bool ServerBindAndListen(NetworkEndPoint endPoint)
        {
            InitDriver();

            int result = m_Driver.Bind(endPoint);
            if (result != 0)
            {
                Debug.LogError("Server failed to bind");
                return false;
            }

            result = m_Driver.Listen();
            if (result != 0)
            {
                Debug.LogError("Server failed to listen");
                return false;
            }

            m_State = State.Listening;
            return true;
        }

        private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
        {
            unsafe
            {
                fixed (byte* ptr = allocationIdBytes)
                {
                    return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
                }
            }
        }

        private static RelayHMACKey ConvertFromHMAC(byte[] hmac)
        {
            unsafe
            {
                fixed (byte* ptr = hmac)
                {
                    return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
                }
            }
        }

        private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
        {
            unsafe
            {
                fixed (byte* ptr = connectionData)
                {
                    return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
                }
            }
        }

        internal void SetMaxPayloadSize(int maxPayloadSize)
        {
            m_MaxPayloadSize = maxPayloadSize;
        }

        private void SetProtocol(ProtocolType inProtocol)
        {
            m_ProtocolType = inProtocol;
        }

        public void SetRelayServerData(string ipv4Address, ushort port, byte[] allocationIdBytes, byte[] keyBytes,
            byte[] connectionDataBytes, byte[] hostConnectionDataBytes = null, bool isSecure = false)
        {
            RelayConnectionData hostConnectionData;

            if (!NetworkEndPoint.TryParse(ipv4Address, port, out var serverEndpoint))
            {
                Debug.LogError($"Invalid address {ipv4Address}:{port}");

                // We set this to default to cause other checks to fail to state you need to call this
                // function again.
                m_RelayServerData = default;
                return;
            }

            var allocationId = ConvertFromAllocationIdBytes(allocationIdBytes);
            var key = ConvertFromHMAC(keyBytes);
            var connectionData = ConvertConnectionData(connectionDataBytes);

            if (hostConnectionDataBytes != null)
            {
                hostConnectionData = ConvertConnectionData(hostConnectionDataBytes);
            }
            else
            {
                hostConnectionData = connectionData;
            }

            m_RelayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationId, ref connectionData,
                ref hostConnectionData, ref key, isSecure);
            m_RelayServerData.ComputeNewNonce();


            SetProtocol(ProtocolType.RelayUnityTransport);
        }

        /// <summary>Set the relay server data for the host.</summary>
        /// <param name="ipAddress">IP address of the relay server.</param>
        /// <param name="port">UDP port of the relay server.</param>
        /// <param name="allocationId">Allocation ID as a byte array.</param>
        /// <param name="key">Allocation key as a byte array.</param>
        /// <param name="connectionData">Connection data as a byte array.</param>
        /// <param name="isSecure">Whether the connection is secure (uses DTLS).</param>
        public void SetHostRelayData(string ipAddress, ushort port, byte[] allocationId, byte[] key,
            byte[] connectionData, bool isSecure = false)
        {
            SetRelayServerData(ipAddress, port, allocationId, key, connectionData, isSecure: isSecure);
        }

        /// <summary>Set the relay server data for the host.</summary>
        /// <param name="ipAddress">IP address of the relay server.</param>
        /// <param name="port">UDP port of the relay server.</param>
        /// <param name="allocationId">Allocation ID as a byte array.</param>
        /// <param name="key">Allocation key as a byte array.</param>
        /// <param name="connectionData">Connection data as a byte array.</param>
        /// <param name="hostConnectionData">Host's connection data as a byte array.</param>
        /// <param name="isSecure">Whether the connection is secure (uses DTLS).</param>
        public void SetClientRelayData(string ipAddress, ushort port, byte[] allocationId, byte[] key,
            byte[] connectionData, byte[] hostConnectionData, bool isSecure = false)
        {
            SetRelayServerData(ipAddress, port, allocationId, key, connectionData, hostConnectionData, isSecure);
        }

        /// <summary>
        /// Sets IP and Port information. This will be ignored if using the Unity Relay and you should call <see cref="SetRelayServerData"/>
        /// </summary>
        public void SetConnectionData(string ipv4Address, ushort port, string listenAddress = null)
        {
            ConnectionData = new ConnectionAddressData
            {
                Address = ipv4Address,
                Port = port,
                ServerListenAddress = listenAddress ?? string.Empty
            };

            SetProtocol(ProtocolType.UnityTransport);
        }

        /// <summary>
        /// Sets IP and Port information. This will be ignored if using the Unity Relay and you should call <see cref="SetRelayServerData"/>
        /// </summary>
        public void SetConnectionData(NetworkEndPoint endPoint, NetworkEndPoint listenEndPoint = default)
        {
            string serverAddress = endPoint.Address.Split(':')[0];

            string listenAddress = string.Empty;
            if (listenEndPoint != default)
            {
                listenAddress = listenEndPoint.Address.Split(':')[0];
                if (endPoint.Port != listenEndPoint.Port)
                {
                    Debug.LogError($"Port mismatch between server and listen endpoints ({endPoint.Port} vs {listenEndPoint.Port}).");
                }
            }

            SetConnectionData(serverAddress, endPoint.Port, listenAddress);
        }

        private bool StartRelayServer()
        {
            //This comparison is currently slow since RelayServerData does not implement a custom comparison operator that doesn't use
            //reflection, but this does not live in the context of a performance-critical loop, it runs once at initial connection time.
            if (m_RelayServerData.Equals(default(RelayServerData)))
            {
                Debug.LogError("You must call SetRelayServerData() at least once before calling StartRelayServer.");
                return false;
            }
            else
            {
                m_NetworkSettings.WithRelayParameters(ref m_RelayServerData);
                return ServerBindAndListen(NetworkEndPoint.AnyIpv4);
            }
        }

        // Send as many batched messages from the queue as possible.
        private void SendBatchedMessages(SendTarget sendTarget, BatchedSendQueue queue)
        {
            var clientId = sendTarget.ClientId;
            var connection = ParseClientId(clientId);
            var pipeline = sendTarget.NetworkPipeline;

            while (!queue.IsEmpty)
            {
                var result = m_Driver.BeginSend(pipeline, connection, out var writer);
                if (result != (int)Networking.Transport.Error.StatusCode.Success)
                {
                    Debug.LogError("Error sending the message: " +
                        ErrorUtilities.ErrorToString((Networking.Transport.Error.StatusCode)result, clientId));
                    return;
                }

                // We don't attempt to send entire payloads over the reliable pipeline. Instead we
                // fragment it manually. This is safe and easy to do since the reliable pipeline
                // basically implements a stream, so as long as we separate the different messages
                // in the stream (the send queue does that automatically) we are sure they'll be
                // reassembled properly at the other end. This allows us to lift the limit of ~44KB
                // on reliable payloads (because of the reliable window size).
                var written = pipeline == m_ReliableSequencedPipeline
                    ? queue.FillWriterWithBytes(ref writer) : queue.FillWriterWithMessages(ref writer);

                result = m_Driver.EndSend(writer);
                if (result == written)
                {
                    // Batched message was sent successfully. Remove it from the queue.
                    queue.Consume(written);
                }
                else
                {
                    // Some error occured. If it's just the UTP queue being full, then don't log
                    // anything since that's okay (the unsent message(s) are still in the queue
                    // and we'll retry sending the later);
                    if (result != (int)Networking.Transport.Error.StatusCode.NetworkSendQueueFull)
                    {
                        Debug.LogError("Error sending the message: " +
                            ErrorUtilities.ErrorToString((Networking.Transport.Error.StatusCode)result, clientId));
                    }

                    return;
                }
            }
        }

        private bool AcceptConnection()
        {
            var connection = m_Driver.Accept();

            if (connection == default(NetworkConnection))
            {
                return false;
            }

            InvokeOnTransportEvent(NetcodeNetworkEvent.Connect,
                ParseClientId(connection),
                default,
                Time.realtimeSinceStartup);

            return true;

        }

        private void ReceiveMessages(ulong clientId, NetworkPipeline pipeline, DataStreamReader dataReader)
        {
            BatchedReceiveQueue queue;
            if (pipeline == m_ReliableSequencedPipeline)
            {
                if (m_ReliableReceiveQueues.TryGetValue(clientId, out queue))
                {
                    queue.PushReader(dataReader);
                }
                else
                {
                    queue = new BatchedReceiveQueue(dataReader);
                    m_ReliableReceiveQueues[clientId] = queue;
                }
            }
            else
            {
                queue = new BatchedReceiveQueue(dataReader);
            }

            while (!queue.IsEmpty)
            {
                var message = queue.PopMessage();
                if (message == default)
                {
                    // Only happens if there's only a partial message in the queue (rare).
                    break;
                }

                InvokeOnTransportEvent(NetcodeNetworkEvent.Data, clientId, message, Time.realtimeSinceStartup);
            }
        }

        private bool ProcessEvent()
        {
            var eventType = m_Driver.PopEvent(out var networkConnection, out var reader, out var pipeline);

            switch (eventType)
            {
                case TransportNetworkEvent.Type.Connect:
                    {
                        InvokeOnTransportEvent(NetcodeNetworkEvent.Connect,
                            ParseClientId(networkConnection),
                            default(ArraySegment<byte>),
                            Time.realtimeSinceStartup);

                        m_State = State.Connected;
                        return true;
                    }
                case TransportNetworkEvent.Type.Disconnect:
                    {
                        if (m_ServerConnection.IsCreated)
                        {
                            m_ServerConnection = default;

                            var reason = reader.ReadByte();
                            if (reason == (byte)Networking.Transport.Error.DisconnectReason.MaxConnectionAttempts)
                            {
                                Debug.LogError("Client failed to connect to server");
                            }
                        }

                        m_ReliableReceiveQueues.Remove(ParseClientId(networkConnection));

                        InvokeOnTransportEvent(NetcodeNetworkEvent.Disconnect,
                            ParseClientId(networkConnection),
                            default(ArraySegment<byte>),
                            Time.realtimeSinceStartup);

                        m_State = State.Disconnected;
                        return true;
                    }
                case TransportNetworkEvent.Type.Data:
                    {
                        ReceiveMessages(ParseClientId(networkConnection), pipeline, reader);
                        return true;
                    }
            }

            return false;
        }

        private void Update()
        {
            if (m_Driver.IsCreated)
            {
                foreach (var kvp in m_SendQueue)
                {
                    SendBatchedMessages(kvp.Key, kvp.Value);
                }

                m_Driver.ScheduleUpdate().Complete();

                while (AcceptConnection() && m_Driver.IsCreated)
                {
                    ;
                }

                while (ProcessEvent() && m_Driver.IsCreated)
                {
                    ;
                }

#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
                ExtractNetworkMetrics();
#endif
            }
        }

        private void OnDestroy()
        {
            DisposeInternals();
        }

#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
        private void ExtractNetworkMetrics()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                var ngoConnectionIds = NetworkManager.Singleton.ConnectedClients.Keys;
                foreach (var ngoConnectionId in ngoConnectionIds)
                {
                    if (ngoConnectionId == 0 && NetworkManager.Singleton.IsHost)
                    {
                        continue;
                    }
                    ExtractNetworkMetricsForClient(NetworkManager.Singleton.ClientIdToTransportId(ngoConnectionId));
                }
            }
            else
            {
                ExtractNetworkMetricsForClient(NetworkManager.Singleton.ClientIdToTransportId(NetworkManager.Singleton.ServerClientId));
            }
        }

        private void ExtractNetworkMetricsForClient(ulong transportClientId)
        {
            var networkConnection =  ParseClientId(transportClientId);
            ExtractNetworkMetricsFromPipeline(m_UnreliableFragmentedPipeline, networkConnection);
            ExtractNetworkMetricsFromPipeline(m_UnreliableSequencedFragmentedPipeline, networkConnection);
            ExtractNetworkMetricsFromPipeline(m_ReliableSequencedPipeline, networkConnection);
        }

        private void ExtractNetworkMetricsFromPipeline(NetworkPipeline pipeline, NetworkConnection networkConnection)
        {
            //Don't need to dispose of the buffers, they are filled with data pointers.
            m_Driver.GetPipelineBuffers(pipeline,
                NetworkPipelineStageCollection.GetStageId(typeof(NetworkMetricsPipelineStage)),
                networkConnection,
                out _,
                out _,
                out var sharedBuffer);

            unsafe
            {
                var networkMetricsContext = (NetworkMetricsContext*)sharedBuffer.GetUnsafePtr();

                NetworkMetrics.TrackPacketSent(networkMetricsContext->PacketSentCount);
                NetworkMetrics.TrackPacketReceived(networkMetricsContext->PacketReceivedCount);

                networkMetricsContext->PacketSentCount = 0;
                networkMetricsContext->PacketReceivedCount = 0;
            }
        }
#endif

        private static unsafe ulong ParseClientId(NetworkConnection utpConnectionId)
        {
            return *(ulong*)&utpConnectionId;
        }

        private static unsafe NetworkConnection ParseClientId(ulong netcodeConnectionId)
        {
            return *(NetworkConnection*)&netcodeConnectionId;
        }

        public override void DisconnectLocalClient()
        {
            if (m_State == State.Connected)
            {
                if (m_Driver.Disconnect(ParseClientId(m_ServerClientId)) == 0)
                {

                    m_State = State.Disconnected;

                    // If we successfully disconnect we dispatch a local disconnect message
                    // this how uNET and other transports worked and so this is just keeping with the old behavior
                    // should be also noted on the client this will call shutdown on the NetworkManager and the Transport
                    InvokeOnTransportEvent(NetcodeNetworkEvent.Disconnect,
                        m_ServerClientId,
                        default(ArraySegment<byte>),
                        Time.realtimeSinceStartup);
                }
            }
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            Debug.Assert(m_State == State.Listening, "DisconnectRemoteClient should be called on a listening server");

            if (m_State == State.Listening)
            {
                var connection = ParseClientId(clientId);

                if (m_Driver.GetConnectionState(connection) != NetworkConnection.State.Disconnected)
                {
                    m_Driver.Disconnect(connection);
                }

                // we need to cleanup any SendQueues for this connectionID;
                var keys = new NativeList<SendTarget>(16, Allocator.Temp); // use nativelist and manual foreach to avoid allocations
                foreach (var key in m_SendQueue.Keys)
                {
                    if (key.ClientId == clientId)
                    {
                        keys.Add(key);
                    }
                }

                foreach (var queue in keys)
                {
                    m_SendQueue[queue].Dispose();
                    m_SendQueue.Remove(queue);
                }
                keys.Dispose();
            }
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            return 0;
        }

        public override void Initialize()
        {
            Debug.Assert(sizeof(ulong) == UnsafeUtility.SizeOf<NetworkConnection>(),
                "Netcode connection id size does not match UTP connection id size");

            m_NetworkSettings = new NetworkSettings(Allocator.Persistent);

            // If the user sends a message of exactly m_MaxPayloadSize in length, we need to
            // account for the overhead of its length when we store it in the send queue.
            var fragmentationCapacity = m_MaxPayloadSize + BatchedSendQueue.PerMessageOverhead;

            m_NetworkSettings
                .WithFragmentationStageParameters(payloadCapacity: fragmentationCapacity)
                .WithBaselibNetworkInterfaceParameters(
                    receiveQueueCapacity: m_MaxPacketQueueSize,
                    sendQueueCapacity: m_MaxPacketQueueSize);
        }

        public override NetcodeNetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = default;
            payload = default;
            receiveTime = default;
            return NetcodeNetworkEvent.Nothing;
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            if (payload.Count > m_MaxPayloadSize)
            {
                Debug.LogError($"Payload of size {payload.Count} larger than configured 'Max Payload Size' ({m_MaxPayloadSize}).");
                return;
            }

            var pipeline = SelectSendPipeline(networkDelivery);

            var sendTarget = new SendTarget(clientId, pipeline);
            if (!m_SendQueue.TryGetValue(sendTarget, out var queue))
            {
                queue = new BatchedSendQueue(Math.Max(m_MaxSendQueueSize, m_MaxPayloadSize));
                m_SendQueue.Add(sendTarget, queue);
            }

            if (!queue.PushMessage(payload))
            {
                Debug.LogError($"Couldn't add payload of size {payload.Count} to batched send queue. " +
                    $"Perhaps configured 'Max Send Queue Size' ({m_MaxSendQueueSize}) is too small for workload.");
                return;
            }
        }

        public override bool StartClient()
        {
            if (m_Driver.IsCreated)
            {
                return false;
            }

            return ClientBindAndConnect();
        }

        public override bool StartServer()
        {
            if (m_Driver.IsCreated)
            {
                return false;
            }

            switch (m_ProtocolType)
            {
                case ProtocolType.UnityTransport:
                    return ServerBindAndListen(ConnectionData.ListenEndPoint);
                case ProtocolType.RelayUnityTransport:
                    return StartRelayServer();
                default:
                    return false;
            }
        }

        public override void Shutdown()
        {
            if (!m_Driver.IsCreated)
            {
                return;
            }

            // Flush the driver's internal send queue. If we're shutting down because the
            // NetworkManager is shutting down, it probably has disconnected some peer(s)
            // in the process and we want to get these disconnect messages on the wire.
            m_Driver.ScheduleFlushSend(default).Complete();

            DisposeInternals();

            // We must reset this to zero because UTP actually re-uses clientIds if there is a clean disconnect
            m_ServerClientId = 0;
        }

        public void CreateDriver(UnityTransport transport, out NetworkDriver driver,
            out NetworkPipeline unreliableFragmentedPipeline,
            out NetworkPipeline unreliableSequencedFragmentedPipeline,
            out NetworkPipeline reliableSequencedPipeline)
        {
#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
            NetworkPipelineStageCollection.RegisterPipelineStage(new NetworkMetricsPipelineStage());
#endif
            var maxFrameTimeMS = 0;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            maxFrameTimeMS = 100;

            var simulatorParams = ClientSimulatorParameters;

            m_NetworkSettings.AddRawParameterStruct(ref simulatorParams);
#endif
            m_NetworkSettings.WithNetworkConfigParameters(
                maxConnectAttempts: transport.m_MaxConnectAttempts,
                connectTimeoutMS: transport.m_ConnectTimeoutMS,
                disconnectTimeoutMS: transport.m_DisconnectTimeoutMS,
                heartbeatTimeoutMS: transport.m_HeartbeatTimeoutMS,
                maxFrameTimeMS: maxFrameTimeMS);

            driver = NetworkDriver.Create(m_NetworkSettings);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (simulatorParams.PacketDelayMs > 0 || simulatorParams.PacketDropInterval > 0)
            {
                unreliableFragmentedPipeline = driver.CreatePipeline(
                    typeof(FragmentationPipelineStage),
                    typeof(SimulatorPipelineStage),
                    typeof(SimulatorPipelineStageInSend)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                );
                unreliableSequencedFragmentedPipeline = driver.CreatePipeline(
                    typeof(FragmentationPipelineStage),
                    typeof(UnreliableSequencedPipelineStage),
                    typeof(SimulatorPipelineStage),
                    typeof(SimulatorPipelineStageInSend)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                    );
                reliableSequencedPipeline = driver.CreatePipeline(
                    typeof(ReliableSequencedPipelineStage),
                    typeof(SimulatorPipelineStage),
                    typeof(SimulatorPipelineStageInSend)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                    );
            }
            else
#endif
            {

                unreliableFragmentedPipeline = driver.CreatePipeline(
                    typeof(FragmentationPipelineStage)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                );
                unreliableSequencedFragmentedPipeline = driver.CreatePipeline(
                    typeof(FragmentationPipelineStage),
                    typeof(UnreliableSequencedPipelineStage)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                );
                reliableSequencedPipeline = driver.CreatePipeline(
                    typeof(ReliableSequencedPipelineStage)
#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
                    ,typeof(NetworkMetricsPipelineStage)
#endif
                );
            }
        }

        // -------------- Utility Types -------------------------------------------------------------------------------


        /// <summary>
        /// Cached information about reliability mode with a certain client
        /// </summary>
        private struct SendTarget : IEquatable<SendTarget>
        {
            public readonly ulong ClientId;
            public readonly NetworkPipeline NetworkPipeline;

            public SendTarget(ulong clientId, NetworkPipeline networkPipeline)
            {
                ClientId = clientId;
                NetworkPipeline = networkPipeline;
            }

            public bool Equals(SendTarget other)
            {
                return ClientId == other.ClientId && NetworkPipeline.Equals(other.NetworkPipeline);
            }

            public override bool Equals(object obj)
            {
                return obj is SendTarget other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (ClientId.GetHashCode() * 397) ^ NetworkPipeline.GetHashCode();
                }
            }
        }
    }
}
