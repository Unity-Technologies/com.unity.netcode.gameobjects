using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Netcode
{
    internal interface IHeartbeatSystem
    {
        public float HeartbeatInterval { get; set; }

        public void Update(float time);

        public void ProcessHeartbeat(ulong clientId, float time);
    }

    internal class ServerHeartbeatSystem : IHeartbeatSystem
    {
        private const float k_InitialTime = -1;

        private Dictionary<ulong, float> m_ClientReceivedHeartbeats = new Dictionary<ulong, float>();
        private float m_LastSentHeartbeatTime = 0;
        private readonly IClientManager m_ClientManager;
        private readonly IMessageSender m_MessageSender;
        private readonly bool m_ShouldDisconnectOnMissedHeartbeats;

        public float HeartbeatInterval { get; set; }
        public float HeartbeatReceiveTimeout => HeartbeatInterval * 2;

        public ServerHeartbeatSystem(IClientManager clientManager, IMessageSender messageSender, bool shouldDisconnectOnMissedHeartbeats)
        {
            m_ClientManager = clientManager;
            m_MessageSender = messageSender;
            m_ShouldDisconnectOnMissedHeartbeats = shouldDisconnectOnMissedHeartbeats;
        }

        public void Update(float time)
        {
            Assert.IsTrue(HeartbeatInterval > 0, "Heartbeat Interval must be greater than 0.");

            if (m_ShouldDisconnectOnMissedHeartbeats)
            {
                foreach (var receivedHeartbeat in m_ClientReceivedHeartbeats)
                {
                    if (receivedHeartbeat.Value > 0 && time - receivedHeartbeat.Value > HeartbeatReceiveTimeout)
                    {
                        NetworkLog.LogWarning($"Heartbeat receive timeout exceeded for {receivedHeartbeat.Key}. Disconnecting.");
                        m_ClientManager.DisconnectClient(receivedHeartbeat.Key);
                    }
                }
            }

            if (time - m_LastSentHeartbeatTime > HeartbeatInterval)
            {
                var hbm = new HeartbeatMessage();
                m_MessageSender.SendMessage(in hbm, NetworkDelivery.Reliable, m_ClientManager.ConnectedClientsIds);
                m_LastSentHeartbeatTime = time;
            }
        }

        public void ProcessHeartbeat(ulong clientId, float time)
        {
            // Heartbeats are sent reliable, unordered, so make sure we're not overwriting with an older time
            if (m_ClientReceivedHeartbeats.TryGetValue(clientId, out var oldTime) && oldTime > time)
            {
                return;
            }

            m_ClientReceivedHeartbeats[clientId] = time;
        }

        public void OnClientConnected(ulong obj)
        {
            m_ClientReceivedHeartbeats.Add(obj, k_InitialTime);
        }

        public void OnClientDisconnected(ulong obj)
        {
            m_ClientReceivedHeartbeats.Remove(obj);
        }
    }

    internal class ClientHeartbeatSystem : IHeartbeatSystem
    {
        private const float k_InitialTime = -1;

        private float m_LastReceivedHeartbeatTime = k_InitialTime;
        private float m_LastSentHeartbeatTime = 0;
        private readonly NetworkManager m_NetworkManager;
        private readonly IMessageSender m_MessageSender;

        public float HeartbeatInterval { get; set; }

        public ClientHeartbeatSystem(NetworkManager networkManager, IMessageSender messageSender)
        {
            m_NetworkManager = networkManager;
            m_MessageSender = messageSender;
        }

        public float HeartbeatReceiveTimeout => 2 * HeartbeatInterval;

        public void Update(float time)
        {
            if (m_LastReceivedHeartbeatTime > 0 && time - m_LastReceivedHeartbeatTime > HeartbeatReceiveTimeout)
            {
                NetworkLog.LogInfo("Exceeded heartbeat receive timeout. Likely to be disconnected by server.");
            }

            if (time - m_LastSentHeartbeatTime > HeartbeatInterval)
            {
                var hbm = new HeartbeatMessage();
                m_MessageSender.SendMessage(in hbm, NetworkDelivery.Reliable, m_NetworkManager.ServerClientId);
            }
        }

        public void ProcessHeartbeat(ulong clientId, float time)
        {
            // Heartbeats are sent reliable, unordered, so make sure we're not overwriting with an older time
            if (m_LastReceivedHeartbeatTime < time)
            {
                m_LastReceivedHeartbeatTime = time;
            }
        }
    }
}
