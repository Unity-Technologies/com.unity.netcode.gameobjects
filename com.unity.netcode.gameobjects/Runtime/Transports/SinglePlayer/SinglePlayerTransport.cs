using System;
using System.Collections.Generic;

namespace Unity.Netcode.Transports.SinglePlayer
{
    /// <summary>
    /// A transport that can be used to run a Netcode for GameObjects session in "single player" mode
    /// by assigning this transport to the <see cref="NetworkConfig.NetworkTransport"/> property before
    /// starting as a host.
    /// </summary>
    /// <remarks>
    /// You can only start as a host when using this transport.
    /// </remarks>
    public class SinglePlayerTransport : NetworkTransport
    {
        /// <inheritdoc/>
        public override ulong ServerClientId { get; } = 0;

        internal static string NotStartingAsHostErrorMessage = $"When using {nameof(SinglePlayerTransport)}, you must start a hosted session so both client and server are available locally.";

        private struct MessageData
        {
            public ulong FromClientId;
            public ArraySegment<byte> Payload;
            public NetworkEvent Event;
            public float AvailableTime;
        }

        private static Dictionary<ulong, Queue<MessageData>> s_MessageQueue = new Dictionary<ulong, Queue<MessageData>>();

        private bool m_Initialized;
        private ulong m_TransportId = 0;
        private NetworkManager m_NetworkManager;


        /// <inheritdoc/>
        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            var copy = new byte[payload.Array.Length];
            Array.Copy(payload.Array, copy, payload.Array.Length);
            s_MessageQueue[clientId].Enqueue(new MessageData
            {
                FromClientId = m_TransportId,
                Payload = new ArraySegment<byte>(copy, payload.Offset, payload.Count),
                Event = NetworkEvent.Data,
                AvailableTime = (float)m_NetworkManager.LocalTime.FixedTime,
            });
        }

        /// <inheritdoc/>
        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (s_MessageQueue[m_TransportId].Count > 0)
            {
                var data = s_MessageQueue[m_TransportId].Peek();
                if (data.AvailableTime > m_NetworkManager.LocalTime.FixedTime)
                {
                    clientId = 0;
                    payload = new ArraySegment<byte>();
                    receiveTime = 0;
                    return NetworkEvent.Nothing;
                }

                s_MessageQueue[m_TransportId].Dequeue();
                clientId = data.FromClientId;
                payload = data.Payload;
                receiveTime = m_NetworkManager.LocalTime.TimeAsFloat;
                if (m_NetworkManager.IsServer && data.Event == NetworkEvent.Connect)
                {
                    s_MessageQueue[data.FromClientId].Enqueue(new MessageData { Event = NetworkEvent.Connect, FromClientId = ServerClientId, Payload = new ArraySegment<byte>() });
                }
                return data.Event;
            }
            clientId = 0;
            payload = new ArraySegment<byte>();
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This will always return false for <see cref="SinglePlayerTransport"/>.
        /// Always use <see cref="StartServer"/>.
        /// </remarks>
        public override bool StartClient()
        {
            NetworkLog.LogError(NotStartingAsHostErrorMessage);
            return false;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Always use <see cref="NetworkManager.StartHost"/> when hosting a local single player session.
        /// </remarks>
        public override bool StartServer()
        {
            s_MessageQueue[ServerClientId] = new Queue<MessageData>();
            if (!m_NetworkManager.LocalClient.IsHost && m_NetworkManager.LocalClient.IsServer)
            {
                NetworkLog.LogError(NotStartingAsHostErrorMessage);
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override void DisconnectRemoteClient(ulong clientId)
        {
            s_MessageQueue[clientId].Enqueue(new MessageData { Event = NetworkEvent.Disconnect, FromClientId = m_TransportId, Payload = new ArraySegment<byte>() });
        }

        /// <inheritdoc/>
        public override void DisconnectLocalClient()
        {
            s_MessageQueue[ServerClientId].Enqueue(new MessageData { Event = NetworkEvent.Disconnect, FromClientId = m_TransportId, Payload = new ArraySegment<byte>() });
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Will always return 0 since this transport is for a local single player session.
        /// </remarks>
        public override ulong GetCurrentRtt(ulong clientId)
        {
            return 0;
        }

        /// <inheritdoc/>
        public override void Shutdown()
        {
            s_MessageQueue.Clear();
            m_TransportId = 0;
        }

        /// <inheritdoc/>
        public override void Initialize(NetworkManager networkManager = null)
        {
            s_MessageQueue.Clear();
            m_NetworkManager = networkManager;
        }
    }
}
