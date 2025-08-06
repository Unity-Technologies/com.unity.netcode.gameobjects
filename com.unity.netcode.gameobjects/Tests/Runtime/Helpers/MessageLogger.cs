using System;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    internal class MessageLogger : INetworkHooks
    {
        private NetworkManager m_OwningNetworkManager;
        public MessageLogger(NetworkManager owningNetworkManager)
        {
            m_OwningNetworkManager = owningNetworkManager;
        }

        public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
        {
            Debug.Log($"{(m_OwningNetworkManager.IsServer ? "Server" : "Client")} {m_OwningNetworkManager.LocalClientId}: Sending {message.GetType().FullName} to {clientId} with {delivery}");
        }

        public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
        {
        }

        public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
            Debug.Log($"{(m_OwningNetworkManager.IsServer ? "Server" : "Client")} {m_OwningNetworkManager.LocalClientId}: Receiving {messageType.FullName} from {senderId}");
        }

        public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
            Debug.Log($"{(m_OwningNetworkManager.IsServer ? "Server" : "Client")} {m_OwningNetworkManager.LocalClientId}: Sending a batch of to {clientId}: {messageCount} messages, {batchSizeInBytes} bytes, with {delivery}");
        }

        public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
            Debug.Log($"{(m_OwningNetworkManager.IsServer ? "Server" : "Client")} {m_OwningNetworkManager.LocalClientId}: Received a batch from {senderId}, {messageCount} messages, {batchSizeInBytes} bytes");
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
            Debug.Log($"{(m_OwningNetworkManager.IsServer ? "Server" : "Client")} {m_OwningNetworkManager.LocalClientId}: Handling message {message.GetType().FullName}");
        }

        public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
        }
    }
}
