using System;

namespace Unity.Netcode
{
    public interface INetworkHooks
    {
        void OnBeforeSendMessage(ulong clientId, Type messageType, NetworkDelivery delivery);
        void OnAfterSendMessage(ulong clientId, Type messageType, NetworkDelivery delivery, int messageSizeBytes);
        void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes);
        void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes);
        void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery);
        void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery);
        void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes);
        void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes);


        bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery);
        bool OnVerifyCanReceive(ulong senderId, Type messageType);
    }
}
