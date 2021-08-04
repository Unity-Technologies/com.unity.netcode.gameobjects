using System.Collections.Generic;

namespace Unity.Netcode
{
    public interface INetworkMetrics
    {
        void TrackNetworkObject(NetworkObject networkObject);

        void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount);

        void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, long bytesCount);

        void TrackNamedMessageReceived(ulong senderClientId, string messageName, long bytesCount);

        void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount);

        void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount);

        void TrackUnnamedMessageReceived(ulong senderClientId, long bytesCount);

        void TrackNetworkVariableDeltaSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, string variableName, long bytesCount);

        void TrackNetworkVariableDeltaReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, string variableName, long bytesCount);

        void TrackOwnershipChangeSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount);

        void TrackOwnershipChangeReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount);

        void TrackObjectSpawnSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount);

        void TrackObjectSpawnReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount);

        void TrackObjectDestroySent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount);

        void TrackObjectDestroySent(IReadOnlyCollection<ulong> receiverClientIds, ulong networkObjectId, string gameObjectName, long bytesCount);

        void TrackObjectDestroyReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount);

        void TrackRpcSent(ulong receiverClientId, ulong networkObjectId, string rpcName, long bytesCount);

        void TrackRpcSent(ulong[] receiverClientIds, ulong networkObjectId, string rpcName, long bytesCount);

        void TrackRpcReceived(ulong senderClientId, ulong networkObjectId, string rpcName, long bytesCount);

        void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount);

        void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount);

        void DispatchFrame();
    }
}
