#pragma warning disable 618 // disable is obsolete
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Netcode.Transports.UNET
{
    public class UNetTransport : NetworkTransport
    {
        public enum SendMode
        {
            Immediately,
            Queued
        }

        private int m_UnreliableChannelId;
        private int m_UnreliableSequencedChannelId;
        private int m_ReliableChannelId;
        private int m_ReliableSequencedChannelId;
        private int m_ReliableFragmentedSequencedChannelId;

        // Inspector / settings
        public int MessageBufferSize = 1024 * 5;
        public int MaxConnections = 100;
        public int MaxSentMessageQueueSize = 128;

        public string ConnectAddress = "127.0.0.1";
        public int ConnectPort = 7777;
        public int ServerListenPort = 7777;

        public SendMode MessageSendMode = SendMode.Immediately;

        // Runtime / state
        private byte[] m_MessageBuffer;
        private WeakReference m_TemporaryBufferReference;

        // Lookup / translation
        private int m_ServerConnectionId;
        private int m_ServerHostId;

        public override ulong ServerClientId => GetNetcodeClientId(0, 0, true);

        protected void LateUpdate()
        {
            if (UnityEngine.Networking.NetworkTransport.IsStarted && MessageSendMode == SendMode.Queued)
            {
#if UNITY_WEBGL
                Debug.LogError("Cannot use queued sending mode for WebGL");
#else
                if (NetworkManager.Singleton.IsServer)
                {
                    foreach (var targetClient in NetworkManager.Singleton.ConnectedClientsList)
                    {
                        SendQueued(targetClient.ClientId);
                    }
                }
                else
                {
                    SendQueued(NetworkManager.Singleton.LocalClientId);
                }
#endif
            }
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            GetUNetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            byte[] buffer;
            if (payload.Offset > 0)
            {
                // UNET cant handle this, do a copy

                if (m_MessageBuffer.Length >= payload.Count)
                {
                    buffer = m_MessageBuffer;
                }
                else
                {
                    object bufferRef;
                    if (m_TemporaryBufferReference != null && ((bufferRef = m_TemporaryBufferReference.Target) != null) && ((byte[])bufferRef).Length >= payload.Count)
                    {
                        buffer = (byte[])bufferRef;
                    }
                    else
                    {
                        buffer = new byte[payload.Count];
                        m_TemporaryBufferReference = new WeakReference(buffer);
                    }
                }

                Buffer.BlockCopy(payload.Array, payload.Offset, buffer, 0, payload.Count);
            }
            else
            {
                buffer = payload.Array;
            }

            int channelId = -1;
            switch (networkDelivery)
            {
                case NetworkDelivery.Unreliable:
                    channelId = m_UnreliableChannelId;
                    break;
                case NetworkDelivery.UnreliableSequenced:
                    channelId = m_UnreliableSequencedChannelId;
                    break;
                case NetworkDelivery.Reliable:
                    channelId = m_ReliableChannelId;
                    break;
                case NetworkDelivery.ReliableSequenced:
                    channelId = m_ReliableSequencedChannelId;
                    break;
                case NetworkDelivery.ReliableFragmentedSequenced:
                    channelId = m_ReliableFragmentedSequencedChannelId;
                    break;
            }

            if (MessageSendMode == SendMode.Queued)
            {
#if UNITY_WEBGL
                Debug.LogError("Cannot use queued sending mode for WebGL");
#else
                UnityEngine.Networking.NetworkTransport.QueueMessageForSending(hostId, connectionId, channelId, buffer, payload.Count, out byte error);
#endif
            }
            else
            {
                UnityEngine.Networking.NetworkTransport.Send(hostId, connectionId, channelId, buffer, payload.Count, out byte error);
            }
        }

#if !UNITY_WEBGL
        private void SendQueued(ulong clientId)
        {
            GetUNetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            UnityEngine.Networking.NetworkTransport.SendQueuedMessages(hostId, connectionId, out _);
        }
#endif

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            var eventType = UnityEngine.Networking.NetworkTransport.Receive(out int hostId, out int connectionId, out _, m_MessageBuffer, m_MessageBuffer.Length, out int receivedSize, out byte error);

            clientId = GetNetcodeClientId((byte)hostId, (ushort)connectionId, false);
            receiveTime = Time.realtimeSinceStartup;

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

                eventType = UnityEngine.Networking.NetworkTransport.Receive(out hostId, out connectionId, out _, tempBuffer, tempBuffer.Length, out receivedSize, out error);
                payload = new ArraySegment<byte>(tempBuffer, 0, receivedSize);
            }
            else
            {
                payload = new ArraySegment<byte>(m_MessageBuffer, 0, receivedSize);
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
                case NetworkEventType.BroadcastEvent:
                case NetworkEventType.Nothing:
                default:
                    return NetworkEvent.Nothing;
            }
        }

        public override bool StartClient()
        {
            m_ServerHostId = UnityEngine.Networking.NetworkTransport.AddHost(new HostTopology(GetConfig(), 1), 0, null);
            m_ServerConnectionId = UnityEngine.Networking.NetworkTransport.Connect(m_ServerHostId, ConnectAddress, ConnectPort, 0, out byte error);
            return (NetworkError)error == NetworkError.Ok;
        }

        public override bool StartServer()
        {
            var topology = new HostTopology(GetConfig(), MaxConnections);
            UnityEngine.Networking.NetworkTransport.AddHost(topology, ServerListenPort, null);
            return true;
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            GetUNetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            UnityEngine.Networking.NetworkTransport.Disconnect((int)hostId, (int)connectionId, out byte error);
        }

        public override void DisconnectLocalClient()
        {
            UnityEngine.Networking.NetworkTransport.Disconnect(m_ServerHostId, m_ServerConnectionId, out byte error);
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            GetUNetConnectionDetails(clientId, out byte hostId, out ushort connectionId);

            return (ulong)UnityEngine.Networking.NetworkTransport.GetCurrentRTT((int)hostId, (int)connectionId, out byte error);
        }

        public override void Shutdown()
        {
            UnityEngine.Networking.NetworkTransport.Shutdown();
        }

        public override void Initialize()
        {
            m_MessageBuffer = new byte[MessageBufferSize];

            UnityEngine.Networking.NetworkTransport.Init();
        }

        private ulong GetNetcodeClientId(byte hostId, ushort connectionId, bool isServer)
        {
            if (isServer)
            {
                return 0;
            }

            return (connectionId | (ulong)hostId << 16) + 1;
        }

        private void GetUNetConnectionDetails(ulong clientId, out byte hostId, out ushort connectionId)
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

        private ConnectionConfig GetConfig()
        {
            var connectionConfig = new ConnectionConfig();

            m_UnreliableChannelId = connectionConfig.AddChannel(QosType.Unreliable);
            m_UnreliableSequencedChannelId = connectionConfig.AddChannel(QosType.UnreliableSequenced);
            m_ReliableChannelId = connectionConfig.AddChannel(QosType.Reliable);
            m_ReliableSequencedChannelId = connectionConfig.AddChannel(QosType.ReliableSequenced);
            m_ReliableFragmentedSequencedChannelId = connectionConfig.AddChannel(QosType.ReliableFragmentedSequenced);

            connectionConfig.MaxSentMessageQueueSize = (ushort)MaxSentMessageQueueSize;

            return connectionConfig;
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore 618 // restore is obsolete
