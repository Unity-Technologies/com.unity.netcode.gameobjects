using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    internal class MetricHooks : INetworkHooks
    {
        private readonly NetworkManager m_NetworkManager;
        private readonly Dictionary<Type, string> m_CachedTypeNames = new();

        public MetricHooks(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
        {
        }

        public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
        {
            m_NetworkManager.NetworkMetrics.TrackNetworkMessageSent(clientId, GetNameForType(typeof(T)), messageSizeBytes);
        }

        public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
            m_NetworkManager.NetworkMetrics.TrackNetworkMessageReceived(senderId, GetNameForType(messageType), messageSizeBytes);
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

        public bool OnVerifyCanReceive(ulong senderId, Type messageType, FastBufferReader messageContent, ref NetworkContext context)
        {
            return true;
        }

        public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
            // TODO: Per-message metrics recording moved here
        }

        public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
            // TODO: Per-message metrics recording moved here
        }

        /// <summary>
        /// Gets the Name from a given type.
        /// </summary>
        private string GetNameForType(Type type)
        {
            if (m_CachedTypeNames.TryGetValue(type, out var cachedName))
            {
                return cachedName;
            }

            // type.Name does a reflection lookup that does a GC allocation
            // Grab the name once and save to a cache.
            var name = type.Name;
            m_CachedTypeNames.Add(type, name);
            return name;
        }
    }
}
