#pragma warning disable 618
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using MLAPI.Exceptions;
using MLAPI.Logging;
using MLAPI.Profiling;
using MLAPI.Transports.Tasks;
using UnityEngine.Networking;

namespace MLAPI.Transports.UNET
{
    public class UNetTransport : NetworkTransport, ITransportProfilerData
    {
        public enum SendMode
        {
            Immediately,
            Queued
        }

        private static ProfilingDataStore s_TransportProfilerData = new ProfilingDataStore();
        public static bool ProfilerEnabled;

        // Inspector / settings
        public int MessageBufferSize = 1024 * 5;
        public int MaxConnections = 100;
        public int MaxSentMessageQueueSize = 128;

        public string ConnectAddress = "127.0.0.1";
        public int ConnectPort = 7777;
        public int ServerListenPort = 7777;
        public int ServerWebsocketListenPort = 8887;
        public bool SupportWebsocket = false;

        // user-definable channels.  To add your own channel, do something of the form:
        //  #define MY_CHANNEL 0
        //  ...
        //  transport.Channels.Add(
        //     new UNetChannel()
        //       {
        //         Id = Channel.ChannelUnused + MY_CHANNEL,  <<-- must offset from reserved channel offset in MLAPI SDK
        //         Type = QosType.Unreliable
        //       }
        //  );
        public List<UNetChannel> Channels = new List<UNetChannel>();

        // Relay
        public bool UseMLAPIRelay = false;
        public string MLAPIRelayAddress = "184.72.104.138";
        public int MLAPIRelayPort = 8888;

        public SendMode MessageSendMode = SendMode.Immediately;

        // Runtime / state
        private byte[] m_MessageBuffer;
        private WeakReference m_TemporaryBufferReference;

        // Lookup / translation
        private readonly Dictionary<NetworkChannel, int> m_ChannelNameToId = new Dictionary<NetworkChannel, int>();
        private readonly Dictionary<int, NetworkChannel> m_ChannelIdToName = new Dictionary<int, NetworkChannel>();
        private int m_ServerConnectionId;
        private int m_ServerHostId;

        private SocketTask m_ConnectTask;
        public override ulong ServerClientId => GetMLAPIClientId(0, 0, true);

        protected void LateUpdate()
        {
            if (UnityEngine.Networking.NetworkTransport.IsStarted && MessageSendMode == SendMode.Queued)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        SendQueued(NetworkManager.Singleton.ConnectedClientsList[i].ClientId);
                    }
                }
                else
                {
                    SendQueued(NetworkManager.Singleton.LocalClientId);
                }
            }
        }

        public override void Send(ulong clientId, ArraySegment<byte> data, NetworkChannel networkChannel)
        {
            if (ProfilerEnabled)
            {
                s_TransportProfilerData.Increment(ProfilerConstants.NumberOfTransportSends);
            }

            GetUNetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            int channelId = 0;

            if (m_ChannelNameToId.ContainsKey(networkChannel))
            {
                channelId = m_ChannelNameToId[networkChannel];
            }
            else
            {
                channelId = m_ChannelNameToId[NetworkChannel.Internal];
            }

            byte[] buffer;

            if (data.Offset > 0)
            {
                // UNET cant handle this, do a copy

                if (m_MessageBuffer.Length >= data.Count)
                {
                    buffer = m_MessageBuffer;
                }
                else
                {
                    object bufferRef = null;
                    if (m_TemporaryBufferReference != null && ((bufferRef = m_TemporaryBufferReference.Target) != null) && ((byte[])bufferRef).Length >= data.Count)
                    {
                        buffer = (byte[])bufferRef;
                    }
                    else
                    {
                        buffer = new byte[data.Count];
                        m_TemporaryBufferReference = new WeakReference(buffer);
                    }
                }

                Buffer.BlockCopy(data.Array, data.Offset, buffer, 0, data.Count);
            }
            else
            {
                buffer = data.Array;
            }

            if (MessageSendMode == SendMode.Queued)
            {
                RelayTransport.QueueMessageForSending(hostId, connectionId, channelId, buffer, data.Count, out byte error);
            }
            else
            {
                RelayTransport.Send(hostId, connectionId, channelId, buffer, data.Count, out byte error);
            }
        }


        public void SendQueued(ulong clientId)
        {
            if (ProfilerEnabled)
            {
                s_TransportProfilerData.Increment(ProfilerConstants.NumberOfTransportSendQueues);
            }

            GetUNetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            RelayTransport.SendQueuedMessages(hostId, connectionId, out byte error);
        }

        public override NetworkEvent PollEvent(out ulong clientId, out NetworkChannel networkChannel, out ArraySegment<byte> payload, out float receiveTime)
        {
            var eventType = RelayTransport.Receive(out int hostId, out int connectionId, out int channelId, m_MessageBuffer, m_MessageBuffer.Length, out int receivedSize, out byte error);

            clientId = GetMLAPIClientId((byte)hostId, (ushort)connectionId, false);
            receiveTime = UnityEngine.Time.realtimeSinceStartup;

            var networkError = (NetworkError)error;
            if (networkError == NetworkError.MessageToLong)
            {
                byte[] tempBuffer;

                if (m_TemporaryBufferReference != null && m_TemporaryBufferReference.IsAlive && ((byte[])m_TemporaryBufferReference.Target).Length >= receivedSize)
                {
                    tempBuffer = (byte[])m_TemporaryBufferReference.Target;
                }
                else
                {
                    tempBuffer = new byte[receivedSize];
                    m_TemporaryBufferReference = new WeakReference(tempBuffer);
                }

                eventType = RelayTransport.Receive(out hostId, out connectionId, out channelId, tempBuffer, tempBuffer.Length, out receivedSize, out error);
                payload = new ArraySegment<byte>(tempBuffer, 0, receivedSize);
            }
            else
            {
                payload = new ArraySegment<byte>(m_MessageBuffer, 0, receivedSize);
            }

            if (m_ChannelIdToName.ContainsKey(channelId))
            {
                networkChannel = m_ChannelIdToName[channelId];
            }
            else
            {
                networkChannel = NetworkChannel.Internal;
            }

            if (m_ConnectTask != null && hostId == m_ServerHostId && connectionId == m_ServerConnectionId)
            {
                if (eventType == NetworkEventType.ConnectEvent)
                {
                    // We just got a response to our connect request.
                    m_ConnectTask.Message = null;
                    m_ConnectTask.SocketError = networkError == NetworkError.Ok ? System.Net.Sockets.SocketError.Success : System.Net.Sockets.SocketError.SocketError;
                    m_ConnectTask.State = null;
                    m_ConnectTask.Success = networkError == NetworkError.Ok;
                    m_ConnectTask.TransportCode = (byte)networkError;
                    m_ConnectTask.TransportException = null;
                    m_ConnectTask.IsDone = true;

                    m_ConnectTask = null;
                }
                else if (eventType == NetworkEventType.DisconnectEvent)
                {
                    // We just got a response to our connect request.
                    m_ConnectTask.Message = null;
                    m_ConnectTask.SocketError = System.Net.Sockets.SocketError.SocketError;
                    m_ConnectTask.State = null;
                    m_ConnectTask.Success = false;
                    m_ConnectTask.TransportCode = (byte)networkError;
                    m_ConnectTask.TransportException = null;
                    m_ConnectTask.IsDone = true;

                    m_ConnectTask = null;
                }
            }

            if (networkError == NetworkError.Timeout)
            {
                // In UNET. Timeouts are not disconnects. We have to translate that here.
                eventType = NetworkEventType.DisconnectEvent;
            }

            // Translate NetworkEventType to NetEventType
            switch (eventType)
            {
                case NetworkEventType.DataEvent:
                    return NetworkEvent.Data;
                case NetworkEventType.ConnectEvent:
                    return NetworkEvent.Connect;
                case NetworkEventType.DisconnectEvent:
                    return NetworkEvent.Disconnect;
                case NetworkEventType.Nothing:
                    return NetworkEvent.Nothing;
                case NetworkEventType.BroadcastEvent:
                    return NetworkEvent.Nothing;
            }

            return NetworkEvent.Nothing;
        }

        public override SocketTasks StartClient()
        {
            var socketTask = SocketTask.Working;

            m_ServerHostId = RelayTransport.AddHost(new HostTopology(GetConfig(), 1), false);
            m_ServerConnectionId = RelayTransport.Connect(m_ServerHostId, ConnectAddress, ConnectPort, 0, out byte error);

            var connectError = (NetworkError)error;

            switch (connectError)
            {
                case NetworkError.Ok:
                    socketTask.Success = true;
                    socketTask.TransportCode = error;
                    socketTask.SocketError = System.Net.Sockets.SocketError.Success;
                    socketTask.IsDone = false;

                    // We want to continue to wait for the successful connect
                    m_ConnectTask = socketTask;
                    break;
                default:
                    socketTask.Success = false;
                    socketTask.TransportCode = error;
                    socketTask.SocketError = System.Net.Sockets.SocketError.SocketError;
                    socketTask.IsDone = true;
                    break;
            }

            return socketTask.AsTasks();
        }

        public override SocketTasks StartServer()
        {
            var topology = new HostTopology(GetConfig(), MaxConnections);

            if (SupportWebsocket)
            {
                if (!UseMLAPIRelay)
                {
                    int websocketHostId = UnityEngine.Networking.NetworkTransport.AddWebsocketHost(topology, ServerWebsocketListenPort);
                }
                else
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Cannot create websocket host when using MLAPI relay");
                }
            }

            int normalHostId = RelayTransport.AddHost(topology, ServerListenPort, true);

            return SocketTask.Done.AsTasks();
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            GetUNetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            RelayTransport.Disconnect((int)hostId, (int)connectionId, out byte error);
        }

        public override void DisconnectLocalClient()
        {
            RelayTransport.Disconnect(m_ServerHostId, m_ServerConnectionId, out byte error);
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            GetUNetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            if (UseMLAPIRelay)
            {
                return 0;
            }
            else
            {
                return (ulong)UnityEngine.Networking.NetworkTransport.GetCurrentRTT((int)hostId, (int)connectionId, out byte error);
            }
        }

        public override void Shutdown()
        {
            m_ChannelIdToName.Clear();
            m_ChannelNameToId.Clear();
            UnityEngine.Networking.NetworkTransport.Shutdown();
        }

        public override void Init()
        {
            UpdateRelay();

            m_MessageBuffer = new byte[MessageBufferSize];

            s_TransportProfilerData.Clear();

            UnityEngine.Networking.NetworkTransport.Init();
        }

        public ulong GetMLAPIClientId(byte hostId, ushort connectionId, bool isServer)
        {
            if (isServer)
            {
                return 0;
            }
            else
            {
                return ((ulong)connectionId | (ulong)hostId << 16) + 1;
            }
        }

        public void GetUNetConnectionDetails(ulong clientId, out byte hostId, out ushort connectionId)
        {
            if (clientId == 0)
            {
                hostId = (byte)m_ServerHostId;
                connectionId = (ushort)m_ServerConnectionId;
            }
            else
            {
                hostId = (byte)((clientId - 1) >> 16);
                connectionId = (ushort)((clientId - 1));
            }
        }

        public ConnectionConfig GetConfig()
        {
            var connectionConfig = new ConnectionConfig();

            // MLAPI built-in channels
            for (int i = 0; i < MLAPI_CHANNELS.Length; i++)
            {
                int channelId = AddMLAPIChannel(MLAPI_CHANNELS[i].Delivery, connectionConfig);

                m_ChannelIdToName.Add(channelId, MLAPI_CHANNELS[i].Channel);
                m_ChannelNameToId.Add(MLAPI_CHANNELS[i].Channel, channelId);
            }

            // Custom user-added channels
            for (int i = 0; i < Channels.Count; i++)
            {
                int channelId = AddUNETChannel(Channels[i].Type, connectionConfig);

                if (m_ChannelNameToId.ContainsKey(Channels[i].Id))
                {
                    throw new InvalidChannelException($"Channel {channelId} already exists");
                }

                m_ChannelIdToName.Add(channelId, Channels[i].Id);
                m_ChannelNameToId.Add(Channels[i].Id, channelId);
            }

            connectionConfig.MaxSentMessageQueueSize = (ushort)MaxSentMessageQueueSize;

            return connectionConfig;
        }

        public int AddMLAPIChannel(NetworkDelivery type, ConnectionConfig config)
        {
            switch (type)
            {
                case NetworkDelivery.Unreliable:
                    return config.AddChannel(QosType.Unreliable);
                case NetworkDelivery.Reliable:
                    return config.AddChannel(QosType.Reliable);
                case NetworkDelivery.ReliableSequenced:
                    return config.AddChannel(QosType.ReliableSequenced);
                case NetworkDelivery.ReliableFragmentedSequenced:
                    return config.AddChannel(QosType.ReliableFragmentedSequenced);
                case NetworkDelivery.UnreliableSequenced:
                    return config.AddChannel(QosType.UnreliableSequenced);
            }

            return 0;
        }

        public int AddUNETChannel(QosType type, ConnectionConfig config)
        {
            switch (type)
            {
                case QosType.Unreliable:
                    return config.AddChannel(QosType.Unreliable);
                case QosType.UnreliableFragmented:
                    return config.AddChannel(QosType.UnreliableFragmented);
                case QosType.UnreliableSequenced:
                    return config.AddChannel(QosType.UnreliableSequenced);
                case QosType.Reliable:
                    return config.AddChannel(QosType.Reliable);
                case QosType.ReliableFragmented:
                    return config.AddChannel(QosType.ReliableFragmented);
                case QosType.ReliableSequenced:
                    return config.AddChannel(QosType.ReliableSequenced);
                case QosType.StateUpdate:
                    return config.AddChannel(QosType.StateUpdate);
                case QosType.ReliableStateUpdate:
                    return config.AddChannel(QosType.ReliableStateUpdate);
                case QosType.AllCostDelivery:
                    return config.AddChannel(QosType.AllCostDelivery);
                case QosType.UnreliableFragmentedSequenced:
                    return config.AddChannel(QosType.UnreliableFragmentedSequenced);
                case QosType.ReliableFragmentedSequenced:
                    return config.AddChannel(QosType.ReliableFragmentedSequenced);
            }

            return 0;
        }

        private void UpdateRelay()
        {
            RelayTransport.Enabled = UseMLAPIRelay;
            RelayTransport.RelayAddress = MLAPIRelayAddress;
            RelayTransport.RelayPort = (ushort)MLAPIRelayPort;
        }

        public void BeginNewTick()
        {
            s_TransportProfilerData.Clear();
        }

        public IReadOnlyDictionary<string, int> GetTransportProfilerData()
        {
            return s_TransportProfilerData.GetReadonly();
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore 618