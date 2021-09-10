using System.IO;
using Unity.Profiling;

namespace Unity.Netcode
{
    internal class InternalMessageHandlerProfilingDecorator : IInternalMessageHandler
    {
        private readonly ProfilerMarker m_HandleSceneEvent = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleSceneEvent)}");
        private readonly ProfilerMarker m_HandleUnnamedMessage = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleUnnamedMessage)}");
        private readonly ProfilerMarker m_HandleNamedMessage = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleNamedMessage)}");
        private readonly ProfilerMarker m_MessageReceiveQueueItemServerRpc = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(MessageReceiveQueueItem)}.{nameof(MessageQueueContainer.MessageType.ServerRpc)}");
        private readonly ProfilerMarker m_MessageReceiveQueueItemClientRpc = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(MessageReceiveQueueItem)}.{nameof(MessageQueueContainer.MessageType.ClientRpc)}");
        private readonly ProfilerMarker m_MessageReceiveQueueItemInternalMessage = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(MessageReceiveQueueItem)}.InternalMessage");

        private readonly IInternalMessageHandler m_MessageHandler;

        internal InternalMessageHandlerProfilingDecorator(IInternalMessageHandler messageHandler)
        {
            m_MessageHandler = messageHandler;
        }

        public NetworkManager NetworkManager => m_MessageHandler.NetworkManager;

        public void MessageReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, MessageQueueContainer.MessageType messageType)
        {
            switch (messageType)
            {
                case MessageQueueContainer.MessageType.ServerRpc:
                    m_MessageReceiveQueueItemServerRpc.Begin();
                    break;
                case MessageQueueContainer.MessageType.ClientRpc:
                    m_MessageReceiveQueueItemClientRpc.Begin();
                    break;
                default:
                    m_MessageReceiveQueueItemInternalMessage.Begin();
                    break;
            }

            m_MessageHandler.MessageReceiveQueueItem(clientId, stream, receiveTime, messageType);

            switch (messageType)
            {
                case MessageQueueContainer.MessageType.ServerRpc:
                    m_MessageReceiveQueueItemServerRpc.End();
                    break;
                case MessageQueueContainer.MessageType.ClientRpc:
                    m_MessageReceiveQueueItemClientRpc.End();
                    break;
                default:
                    m_MessageReceiveQueueItemInternalMessage.End();
                    break;
            }
        }

        public void HandleUnnamedMessage(ulong clientId, Stream stream)
        {
            m_HandleUnnamedMessage.Begin();

            m_MessageHandler.HandleUnnamedMessage(clientId, stream);

            m_HandleUnnamedMessage.End();
        }

        public void HandleNamedMessage(ulong clientId, Stream stream)
        {
            m_HandleNamedMessage.Begin();

            m_MessageHandler.HandleNamedMessage(clientId, stream);

            m_HandleNamedMessage.End();
        }

        public void HandleSceneEvent(ulong clientId, Stream stream)
        {
            m_HandleSceneEvent.Begin();

            m_MessageHandler.HandleSceneEvent(clientId, stream);

            m_HandleSceneEvent.End();
        }
    }
}
