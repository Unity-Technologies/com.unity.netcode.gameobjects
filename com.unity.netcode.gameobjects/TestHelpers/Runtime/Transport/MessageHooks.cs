using System;

namespace Unity.Netcode.TestHelpers.Runtime
{
    internal class MessageHooks : INetworkHooks
    {
        public bool IsWaiting;
        public delegate bool MessageReceiptCheck(object receivedMessage);
        public MessageReceiptCheck ReceiptCheck;

        public static bool CheckForMessageOfType<T>(object receivedMessage) where T : INetworkMessage
        {
            return receivedMessage is T;
        }

        public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
        {
        }

        public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
        {
        }

        public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
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

        public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
        }

        public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
            if (IsWaiting && (ReceiptCheck == null || ReceiptCheck.Invoke(message)))
            {
                IsWaiting = false;
            }
        }
    }
}
