
namespace Unity.Netcode
{
    /// <summary>
    /// Default NetworkTransport Message Sender
    /// <see cref="MessagingSystem"/>
    /// </summary>
    internal class DefaultMessageSender : IMessageSender
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

            m_NetworkTransport.Send(m_ConnectionManager.ClientIdToTransportId(clientId), sendBuffer, delivery);
        }
    }
}
