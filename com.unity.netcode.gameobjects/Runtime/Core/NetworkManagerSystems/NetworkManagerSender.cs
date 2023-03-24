
namespace Unity.Netcode
{
    internal class NetworkManagerMessageSender : IMessageSender
    {
        private NetworkTransport m_NetworkTransport;
        private NetworkConnectionManager m_ConnectionManager;

        public NetworkManagerMessageSender(NetworkManager manager)
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
