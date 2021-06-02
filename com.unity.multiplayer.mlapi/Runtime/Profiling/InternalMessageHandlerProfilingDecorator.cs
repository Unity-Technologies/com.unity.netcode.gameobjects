using System;
using System.IO;
using MLAPI.Messaging;
using MLAPI.Messaging.Buffering;
using Unity.Profiling;

namespace MLAPI.Profiling
{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
    internal class InternalMessageHandlerProfilingDecorator : IInternalMessageHandler
    {
        static readonly ProfilerMarker s_HandleConnectionRequest = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleConnectionRequest)}");
        static readonly ProfilerMarker s_HandleConnectionApproved = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleConnectionApproved)}");
        static readonly ProfilerMarker s_HandleAddObject = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleAddObject)}");
        static readonly ProfilerMarker s_HandleDestroyObject = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleDestroyObject)}");
        static readonly ProfilerMarker s_HandleSwitchScene = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleSwitchScene)}");
        static readonly ProfilerMarker s_HandleClientSwitchSceneCompleted = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleClientSwitchSceneCompleted)}");
        static readonly ProfilerMarker s_HandleChangeOwner = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleChangeOwner)}");
        static readonly ProfilerMarker s_HandleAddObjects = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleAddObjects)}");
        static readonly ProfilerMarker s_HandleDestroyObjects = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleDestroyObjects)}");
        static readonly ProfilerMarker s_HandleTimeSync = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleTimeSync)}");
        static readonly ProfilerMarker s_HandleNetworkVariableDelta = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleNetworkVariableDelta)}");
        static readonly ProfilerMarker s_HandleNetworkVariableUpdate = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleNetworkVariableUpdate)}");
        static readonly ProfilerMarker s_HandleUnnamedMessage = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleUnnamedMessage)}");
        static readonly ProfilerMarker s_HandleNamedMessage = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleNamedMessage)}");
        static readonly ProfilerMarker s_HandleNetworkLog = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleNetworkLog)}");
        static readonly ProfilerMarker s_RpcReceiveQueueItemServerRpc = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(RpcReceiveQueueItem)}.{nameof(RpcQueueContainer.QueueItemType.ServerRpc)}");
        static readonly ProfilerMarker s_RpcReceiveQueueItemClientRpc = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(RpcReceiveQueueItem)}.{nameof(RpcQueueContainer.QueueItemType.ClientRpc)}");
        static readonly ProfilerMarker s_HandleAllClientsSwitchSceneCompleted = new ProfilerMarker($"{nameof(InternalMessageHandler)}.{nameof(HandleAllClientsSwitchSceneCompleted)}");
        
        readonly IInternalMessageHandler m_MessageHandler;
        
        internal InternalMessageHandlerProfilingDecorator(IInternalMessageHandler messageHandler)
        {
            m_MessageHandler = messageHandler;
        }

        public NetworkManager NetworkManager => m_MessageHandler.NetworkManager;

        public void HandleConnectionRequest(ulong clientId, Stream stream)
        {
            s_HandleConnectionRequest.Begin();

            m_MessageHandler.HandleConnectionRequest(clientId, stream);

            s_HandleConnectionRequest.End();
        }

        public void HandleConnectionApproved(ulong clientId, Stream stream, float receiveTime)
        {
            s_HandleConnectionApproved.Begin();

            m_MessageHandler.HandleConnectionApproved(clientId, stream, receiveTime);

            s_HandleConnectionApproved.End();
        }

        public void HandleAddObject(ulong clientId, Stream stream)
        {
            s_HandleAddObject.Begin();

            m_MessageHandler.HandleAddObject(clientId, stream);

            s_HandleAddObject.End();
        }

        public void HandleDestroyObject(ulong clientId, Stream stream)
        {
            s_HandleDestroyObject.Begin();

            m_MessageHandler.HandleDestroyObject(clientId, stream);

            s_HandleDestroyObject.End();
        }

        public void HandleSwitchScene(ulong clientId, Stream stream)
        {
            s_HandleSwitchScene.Begin();

            m_MessageHandler.HandleSwitchScene(clientId, stream);

            s_HandleSwitchScene.End();
        }

        public void HandleClientSwitchSceneCompleted(ulong clientId, Stream stream)
        {
            s_HandleClientSwitchSceneCompleted.Begin();

            m_MessageHandler.HandleClientSwitchSceneCompleted(clientId, stream);

            s_HandleClientSwitchSceneCompleted.End();
        }

        public void HandleChangeOwner(ulong clientId, Stream stream)
        {
            s_HandleChangeOwner.Begin();

            m_MessageHandler.HandleChangeOwner(clientId, stream);

            s_HandleChangeOwner.End();
        }

        public void HandleAddObjects(ulong clientId, Stream stream)
        {
            s_HandleAddObjects.Begin();

            m_MessageHandler.HandleAddObjects(clientId, stream);

            s_HandleAddObjects.End();
        }

        public void HandleDestroyObjects(ulong clientId, Stream stream)
        {
            s_HandleDestroyObjects.Begin();

            m_MessageHandler.HandleDestroyObjects(clientId, stream);

            s_HandleDestroyObjects.End();
        }

        public void HandleTimeSync(ulong clientId, Stream stream, float receiveTime)
        {
            s_HandleTimeSync.Begin();

            m_MessageHandler.HandleTimeSync(clientId, stream, receiveTime);

            s_HandleTimeSync.End();
        }

        public void HandleNetworkVariableDelta(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
            s_HandleNetworkVariableDelta.Begin();

            m_MessageHandler.HandleNetworkVariableDelta(clientId, stream, bufferCallback, bufferPreset);

            s_HandleNetworkVariableDelta.End();
        }

        public void HandleNetworkVariableUpdate(ulong clientId, Stream stream, Action<ulong, PreBufferPreset> bufferCallback, PreBufferPreset bufferPreset)
        {
            s_HandleNetworkVariableUpdate.Begin();

            m_MessageHandler.HandleNetworkVariableUpdate(clientId, stream, bufferCallback, bufferPreset);

            s_HandleNetworkVariableUpdate.End();
        }

        public void RpcReceiveQueueItem(ulong clientId, Stream stream, float receiveTime, RpcQueueContainer.QueueItemType queueItemType)
        {
            switch (queueItemType)
            {
                case RpcQueueContainer.QueueItemType.ServerRpc:
                    s_RpcReceiveQueueItemServerRpc.Begin();
                    break;
                case RpcQueueContainer.QueueItemType.ClientRpc:
                    s_RpcReceiveQueueItemClientRpc.Begin();
                    break;
            }

            m_MessageHandler.RpcReceiveQueueItem(clientId, stream, receiveTime, queueItemType);

            switch (queueItemType)
            {
                case RpcQueueContainer.QueueItemType.ServerRpc:
                    s_RpcReceiveQueueItemServerRpc.End();
                    break;
                case RpcQueueContainer.QueueItemType.ClientRpc:
                    s_RpcReceiveQueueItemClientRpc.End();
                    break;
            }
        }

        public void HandleUnnamedMessage(ulong clientId, Stream stream)
        {
            s_HandleUnnamedMessage.Begin();

            m_MessageHandler.HandleUnnamedMessage(clientId, stream);

            s_HandleUnnamedMessage.End();
        }

        public void HandleNamedMessage(ulong clientId, Stream stream)
        {
            s_HandleNamedMessage.Begin();

            m_MessageHandler.HandleNamedMessage(clientId, stream);

            s_HandleNamedMessage.End();
        }

        public void HandleNetworkLog(ulong clientId, Stream stream)
        {
            s_HandleNetworkLog.Begin();

            m_MessageHandler.HandleNetworkLog(clientId, stream);

            s_HandleNetworkLog.End();
        }

        public void HandleAllClientsSwitchSceneCompleted(ulong clientId, Stream stream)
        {
            s_HandleAllClientsSwitchSceneCompleted.Begin();

            m_MessageHandler.HandleAllClientsSwitchSceneCompleted(clientId, stream);

            s_HandleAllClientsSwitchSceneCompleted.End();
        }
    }
#endif
}