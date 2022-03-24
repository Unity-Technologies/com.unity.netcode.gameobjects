using System.Collections.Generic;

namespace Unity.Netcode
{
    internal interface INetworkMetrics
    {
        void SetConnectionId(ulong connectionId);

        void TrackTransportBytesSent(long bytesCount);

        void TrackTransportBytesReceived(long bytesCount);

        void TrackNetworkMessageSent(ulong receivedClientId, string messageType, long bytesCount);

        void TrackNetworkMessageReceived(ulong senderClientId, string messageType, long bytesCount);

        void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount);

        void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, long bytesCount);

        void TrackNamedMessageReceived(ulong senderClientId, string messageName, long bytesCount);

        void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount);

        void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount);

        void TrackUnnamedMessageReceived(ulong senderClientId, long bytesCount);

        void TrackNetworkVariableDeltaSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount);

        void TrackNetworkVariableDeltaReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount);

        void TrackOwnershipChangeSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount);

        void TrackOwnershipChangeReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount);

        void TrackObjectSpawnSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount);

        void TrackObjectSpawnReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount);

        void TrackObjectDestroySent(ulong receiverClientId, NetworkObject networkObject, long bytesCount);

        void TrackObjectDestroyReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount);

        void TrackRpcSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount);

        void TrackRpcSent(
            ulong[] receiverClientIds,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount);

        void TrackRpcReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount);

        void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount);

        void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount);

        void TrackSceneEventSent(IReadOnlyList<ulong> receiverClientIds, uint sceneEventType, string sceneName, long bytesCount);

        void TrackSceneEventSent(ulong receiverClientId, uint sceneEventType, string sceneName, long bytesCount);

        void TrackSceneEventReceived(ulong senderClientId, uint sceneEventType, string sceneName, long bytesCount);

        void TrackPacketSent(uint packetCount);

        void TrackPacketReceived(uint packetCount);

        void UpdateRttToServer(int rtt);

        void UpdateNetworkObjectsCount(int count);

        void UpdateConnectionsCount(int count);

        void UpdatePacketLoss(float packetLoss);

        void DispatchFrame();
    }
}
