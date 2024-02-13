namespace Unity.Netcode
{
    internal class AnticipationSystem
    {
        internal ulong LastAnticipationAck;
        internal double LastAnticipationAckTime;

        internal ulong AnticipationCounter;

        private NetworkManager m_NetworkManager;

        public AnticipationSystem(NetworkManager manager)
        {
            m_NetworkManager = manager;
        }

        public void Sync()
        {
            if (!m_NetworkManager.ShutdownInProgress && !m_NetworkManager.ConnectionManager.LocalClient.IsServer && m_NetworkManager.ConnectionManager.LocalClient.IsConnected)
            {
                var message = new AnticipationCounterSyncPingMessage { Counter = AnticipationCounter, Time = m_NetworkManager.LocalTime.Time };
                m_NetworkManager.MessageManager.SendMessage(ref message, NetworkDelivery.Reliable, NetworkManager.ServerClientId);
            }

            ++AnticipationCounter;
        }
    }
}
