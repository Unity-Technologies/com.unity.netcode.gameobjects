using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Netcode.RuntimeTests
{
    internal class MessageCatcher<TMessageType> : INetworkHooks where TMessageType : INetworkMessage
    {
        private NetworkManager m_OwnerNetworkManager;

        public MessageCatcher(NetworkManager ownerNetworkManager)
        {
            m_OwnerNetworkManager = ownerNetworkManager;
        }

        private struct TriggerData
        {
            public FastBufferReader Reader;
            public NetworkMessageHeader Header;
            public ulong SenderId;
            public float Timestamp;
            public int SerializedHeaderSize;
        }
        private readonly List<TriggerData> m_CaughtMessages = new List<TriggerData>();

        public void ReleaseMessages()
        {

            foreach (var caughtSpawn in m_CaughtMessages)
            {
                // Reader will be disposed within HandleMessage
                m_OwnerNetworkManager.ConnectionManager.MessageManager.HandleMessage(caughtSpawn.Header, caughtSpawn.Reader, caughtSpawn.SenderId, caughtSpawn.Timestamp, caughtSpawn.SerializedHeaderSize);
            }
        }

        public int CaughtMessageCount => m_CaughtMessages.Count;

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

        public bool OnVerifyCanReceive(ulong senderId, Type messageType, FastBufferReader messageContent, ref NetworkContext context)
        {
            if (messageType == typeof(TMessageType))
            {
                m_CaughtMessages.Add(new TriggerData
                {
                    Reader = new FastBufferReader(messageContent, Allocator.Persistent),
                    Header = context.Header,
                    Timestamp = context.Timestamp,
                    SenderId = context.SenderId,
                    SerializedHeaderSize = context.SerializedHeaderSize
                });
                return false;
            }

            return true;
        }

        public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
        }

        public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
        }
    }
}
