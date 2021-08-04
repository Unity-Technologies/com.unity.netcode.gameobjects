using System.Collections.Generic;

namespace Unity.Netcode
{
    public class NullNetworkMetrics : INetworkMetrics
    {
        public void TrackNetworkObject(NetworkObject networkObject)
        {
        }

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount)
        {
        }

        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, long bytesCount)
        {
        }

        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, long bytesCount)
        {
        }

        public void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount)
        {
        }

        public void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount)
        {
        }

        public void TrackUnnamedMessageReceived(ulong senderClientId, long bytesCount)
        {
        }

        public void TrackNetworkVariableDeltaSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, string variableName, long bytesCount)
        {
        }

        public void TrackNetworkVariableDeltaReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, string variableName, long bytesCount)
        {
        }

        public void TrackOwnershipChangeSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
        }

        public void TrackOwnershipChangeReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
        }

        public void TrackObjectSpawnSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
        }

        public void TrackObjectSpawnReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
        }

        public void TrackObjectDestroySent(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
        }

        public void TrackObjectDestroySent(IReadOnlyCollection<ulong> receiverClientIds, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
        }

        public void TrackObjectDestroyReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
        }

        public void TrackRpcSent(ulong receiverClientId, ulong networkObjectId, string rpcName, long bytesCount)
        {
        }

        public void TrackRpcSent(ulong[] receiverClientIds, ulong networkObjectId, string rpcName, long bytesCount)
        {
        }

        public void TrackRpcReceived(ulong senderClientId, ulong networkObjectId, string rpcName, long bytesCount)
        {
        }

        public void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount)
        {
        }

        public void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount)
        {
        }

        public void DispatchFrame()
        {
        }
    }
}
