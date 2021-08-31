using System;

namespace Unity.Netcode
{
    public interface INetworkHooks
    {
        void OnSendMessage(ulong clientId, Type messageType, NetworkChannel channel, NetworkDelivery delivery);
        void OnReceiveMessage(ulong senderId, Type messageType, NetworkChannel channel);
        void OnSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery);
        void OnReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes);
    }
}