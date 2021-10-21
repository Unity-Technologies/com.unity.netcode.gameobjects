//#if MULTIPLAYER_TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Unity.Netcode
{
    internal partial class NetworkMetrics
    {
        partial void SetConnectionIdInternalInternal(ulong connectionId);
        partial void TrackTransportBytesSentInternal(long bytesCount);
        partial void TrackTransportBytesReceivedInternal(long bytesCount);
        partial void TrackNetworkMessageSentInternal(ulong receivedClientId, string messageType, long bytesCount);
        partial void TrackNetworkMessageReceivedInternal(ulong senderClientId, string messageType, long bytesCount);
        partial void TrackNamedMessageSentInternal(ulong receiverClientId, string messageName, long bytesCount);
        partial void TrackNamedMessageReceivedInternal(ulong senderClientId, string messageName, long bytesCount);
        partial void TrackNamedMessageSentInternal(IReadOnlyCollection<ulong> receiverClientIds, string messageName,
            long bytesCount);
        partial void TrackUnnamedMessageSentInternal(ulong receiverClientId, long bytesCount);
        partial void TrackUnnamedMessageSentInternal(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount);
        partial void TrackUnnamedMessageReceivedInternal(ulong senderClientId, long bytesCount);
        partial void TrackNetworkVariableDeltaSentInternal(
            ulong receiverClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount);
        partial void TrackNetworkVariableDeltaReceivedInternal(
            ulong senderClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount);
        partial void TrackOwnershipChangeReceivedInternal(ulong senderClientId, NetworkObject networkObject,
            long bytesCount);
        partial void TrackOwnershipChangeSentInternal(ulong receiverClientId, NetworkObject networkObject,
            long bytesCount);
        partial void TrackObjectSpawnSentInternal(ulong receiverClientId, NetworkObject networkObject, long bytesCount);
        partial void TrackObjectSpawnReceivedInternal(ulong senderClientId, NetworkObject networkObject,
            long bytesCount);
        partial void TrackObjectDestroySentInternal(ulong receiverClientId, NetworkObject networkObject,
            long bytesCount);
        partial void TrackObjectDestroyReceivedInternal(ulong senderClientId, NetworkObject networkObject,
            long bytesCount);
        partial void TrackRpcSentInternal(
            ulong receiverClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount);
        partial void TrackRpcSentInternal(
            ulong[] receiverClientIds,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount);
        partial void TrackRpcReceivedInternal(
            ulong senderClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount);
        partial void TrackServerLogSentInternal(ulong receiverClientId, uint logType, long bytesCount);
        partial void TrackServerLogReceivedInternal(ulong senderClientId, uint logType, long bytesCount);
        partial void TrackSceneEventSentInternal(IReadOnlyList<ulong> receiverClientIds, uint sceneEventType,
            string sceneName, long bytesCount);
        partial void TrackSceneEventSentInternal(ulong receiverClientId, uint sceneEventType, string sceneName,
            long bytesCount);
        partial void TrackSceneEventReceivedInternal(ulong senderClientId, uint sceneEventType, string sceneName,
            long bytesCount);
        partial void DispatchFrameInternal();

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void SetConnectionId(ulong connectionId)
        {
            SetConnectionIdInternalInternal(connectionId);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackTransportBytesSent(long bytesCount)
        {
            TrackTransportBytesSentInternal(bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackTransportBytesReceived(long bytesCount)
        {
            TrackTransportBytesReceivedInternal(bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackNetworkMessageSent(ulong receivedClientId, string messageType, long bytesCount)
        {
            TrackNetworkMessageSentInternal(receivedClientId, messageType, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackNetworkMessageReceived(ulong senderClientId, string messageType, long bytesCount)
        {
            TrackNetworkMessageReceivedInternal(senderClientId, messageType, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount)
        {
            TrackNamedMessageSentInternal(receiverClientId, messageName, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, long bytesCount)
        {
            TrackNamedMessageSentInternal(receiverClientIds, messageName, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, long bytesCount)
        {
            TrackNamedMessageReceivedInternal(senderClientId, messageName, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount)
        {
            TrackUnnamedMessageSentInternal(receiverClientId, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount)
        {
            TrackUnnamedMessageSentInternal(receiverClientIds, bytesCount);

        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackUnnamedMessageReceived(ulong senderClientId, long bytesCount)
        {
            TrackUnnamedMessageReceivedInternal(senderClientId, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackNetworkVariableDeltaSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            TrackNetworkVariableDeltaSentInternal(
                receiverClientId,
                networkObject, variableName,
                networkBehaviourName,
                bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackNetworkVariableDeltaReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            TrackNetworkVariableDeltaReceivedInternal(
                senderClientId,
                networkObject, variableName,
                networkBehaviourName,
                bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackOwnershipChangeSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            TrackOwnershipChangeSentInternal(receiverClientId, networkObject, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackOwnershipChangeReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            TrackOwnershipChangeReceivedInternal(senderClientId, networkObject, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackObjectSpawnSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            TrackObjectSpawnSentInternal(receiverClientId, networkObject, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackObjectSpawnReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            TrackObjectSpawnReceivedInternal(senderClientId, networkObject, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackObjectDestroySent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            TrackObjectDestroySentInternal(receiverClientId, networkObject, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackObjectDestroyReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            TrackObjectDestroyReceivedInternal(senderClientId, networkObject, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackRpcSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            TrackRpcSentInternal(receiverClientId, networkObject, rpcName, networkBehaviourName, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackRpcSent(
            ulong[] receiverClientIds,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            TrackRpcSentInternal(receiverClientIds, networkObject, rpcName, networkBehaviourName, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackRpcReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            TrackRpcReceivedInternal(senderClientId, networkObject, rpcName, networkBehaviourName, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount)
        {
            TrackServerLogSentInternal(receiverClientId, logType, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount)
        {
            TrackServerLogReceivedInternal(senderClientId, logType, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackSceneEventSent(IReadOnlyList<ulong> receiverClientIds, uint sceneEventType, string sceneName, long bytesCount)
        {
            TrackSceneEventSentInternal(receiverClientIds, sceneEventType, sceneName, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackSceneEventSent(ulong receiverClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            TrackSceneEventSentInternal(receiverClientId, sceneEventType, sceneName, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void TrackSceneEventReceived(ulong senderClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            TrackSceneEventReceivedInternal(senderClientId, sceneEventType, sceneName, bytesCount);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void DispatchFrame()
        {
            DispatchFrameInternal();
        }
    }


}
//#endif
