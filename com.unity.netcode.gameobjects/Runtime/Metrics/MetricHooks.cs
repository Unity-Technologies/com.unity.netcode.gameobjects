using System;

namespace Unity.Netcode
{
    internal class MetricHooks : INetworkHooks
    {
        private readonly NetworkManager m_NetworkManager;

        public MetricHooks(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }


        public void OnBeforeSendMessage(ulong clientId, Type messageType, NetworkDelivery delivery)
        {
        }

        public void OnAfterSendMessage(ulong clientId, Type messageType, NetworkDelivery delivery, int messageSizeBytes)
        {
            m_NetworkManager.NetworkMetrics.TrackNetworkMessageSent(clientId, messageType.Name, messageSizeBytes);
        }

        public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
            m_NetworkManager.NetworkMetrics.TrackNetworkMessageReceived(senderId, messageType.Name, messageSizeBytes);
        }

        public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
            m_NetworkManager.NetworkMetrics.TrackTransportBytesSent(batchSizeInBytes);
        }

        public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
            m_NetworkManager.NetworkMetrics.TrackTransportBytesReceived(batchSizeInBytes);
        }

        public void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
        }

        public bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery)
        {
            return true;
        }

        public bool OnVerifyCanReceive(ulong senderId, Type messageType)
        {
            return true;
        }
    }
}
