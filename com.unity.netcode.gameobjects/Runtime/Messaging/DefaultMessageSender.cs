
namespace Unity.Netcode
{
    /// <summary>
    /// Default NetworkTransport Message Sender
    /// <see cref="NetworkMessageManager"/>
    /// </summary>
    internal class DefaultMessageSender : INetworkMessageSender
    {
        private NetworkTransport m_NetworkTransport;
        private NetworkConnectionManager m_ConnectionManager;

        public DefaultMessageSender(NetworkManager manager)
        {
            m_NetworkTransport = manager.NetworkConfig.NetworkTransport;
            m_ConnectionManager = manager.ConnectionManager;
        }

        public void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData)
        {
            var sendBuffer = batchData.ToTempByteArray();
            var (transportId, clientExists) = m_ConnectionManager.ClientIdToTransportId(clientId);

            if (!clientExists)
            {
                if (m_ConnectionManager.NetworkManager.LogLevel <= LogLevel.Error)
                {
                    NetworkLog.LogWarning("Trying to send a message to a client who doesn't have a transport connection");
                }

                return;
            }

            m_NetworkTransport.Send(transportId, sendBuffer, delivery);
        }
    }
}
