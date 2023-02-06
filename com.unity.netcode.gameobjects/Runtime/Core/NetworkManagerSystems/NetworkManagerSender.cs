
namespace Unity.Netcode
{
    internal class NetworkManagerMessageSender : IMessageSender
    {
        private NetworkManager m_NetworkManager;

        public NetworkManagerMessageSender(NetworkManager manager)
        {
            m_NetworkManager = manager;
        }

        public void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData)
        {
            var sendBuffer = batchData.ToTempByteArray();

            m_NetworkManager.NetworkConfig.NetworkTransport.Send(m_NetworkManager.ConnectionManager.ClientIdToTransportId(clientId), sendBuffer, delivery);
        }
    }
}
