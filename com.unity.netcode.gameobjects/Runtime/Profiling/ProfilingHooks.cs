using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace Unity.Netcode
{
    internal class ProfilingHooks : INetworkHooks
    {
        private Dictionary<Type, ProfilerMarker> m_HandlerProfilerMarkers = new Dictionary<Type, ProfilerMarker>();
        private Dictionary<Type, ProfilerMarker> m_SenderProfilerMarkers = new Dictionary<Type, ProfilerMarker>();
        private readonly ProfilerMarker m_SendBatch = new ProfilerMarker($"{nameof(MessagingSystem)}.SendBatch");
        private readonly ProfilerMarker m_ReceiveBatch = new ProfilerMarker($"{nameof(MessagingSystem)}.ReceiveBatchBatch");

        private ProfilerMarker GetHandlerProfilerMarker(Type type)
        {
            var result = m_HandlerProfilerMarkers.TryGetValue(type, out var marker);
            if (result)
            {
                return marker;
            }

            marker = new ProfilerMarker($"{nameof(MessagingSystem)}.DeserializeAndHandle.{type.Name}");
            m_HandlerProfilerMarkers[type] = marker;
            return marker;
        }

        private ProfilerMarker GetSenderProfilerMarker(Type type)
        {
            var result = m_SenderProfilerMarkers.TryGetValue(type, out var marker);
            if (result)
            {
                return marker;
            }

            marker = new ProfilerMarker($"{nameof(MessagingSystem)}.SerializeAndEnqueue.{type.Name}");
            m_SenderProfilerMarkers[type] = marker;
            return marker;
        }

        public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
        {
            GetSenderProfilerMarker(typeof(T)).Begin();
        }

        public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
        {
            GetSenderProfilerMarker(typeof(T)).End();
        }

        public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
            GetHandlerProfilerMarker(messageType).Begin();
        }

        public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
            GetHandlerProfilerMarker(messageType).End();
        }

        public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
            m_SendBatch.Begin();
        }

        public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
            m_SendBatch.End();
        }

        public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
            m_ReceiveBatch.Begin();
        }

        public void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
            m_ReceiveBatch.End();
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
            // nop
        }

        public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
            // nop
        }
    }
}
