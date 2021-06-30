using System.Collections.Generic;

namespace MLAPI.Metrics
{
    public interface INetworkMetrics
    {
        void TrackNetworkObject(NetworkObject networkObject);

        void TrackNamedMessageSent(ulong receiverClientId, string messageName, ulong bytesCount);

        void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, ulong bytesCount);

        void TrackNamedMessageReceived(ulong senderClientId, string messageName, ulong bytesCount);

        void TrackUnnamedMessageSent(ulong receiverClientId, ulong bytesCount);

        void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, ulong bytesCount);

        void TrackUnnamedMessageReceived(ulong senderClientId, ulong bytesCount);

        void TrackNetworkVariableDeltaSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName,string variableName, ulong bytesCount);

        void TrackNetworkVariableDeltaReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName,string variableName, ulong bytesCount);

        void TrackObjectSpawnSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount);

        void TrackObjectSpawnReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount);

        void TrackObjectDestroySent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount);

        void TrackObjectDestroySent(IReadOnlyCollection<ulong> receiverClientIds, ulong networkObjectId, string gameObjectName, ulong bytesCount);

        void TrackObjectDestroyReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount);

        void TrackMultipleObjectSpawnSent(ulong receiverClientId, Dictionary<ulong,string> networkObjectIdToName, ulong bytesCount);

        void TrackMultipleObjectDestroySent(ulong receiverClientId, Dictionary<ulong,string> networkObjectIdToName, ulong bytesCount);

        void TrackMultipleObjectDestroySent(IReadOnlyCollection<ulong> receiverClientIds, Dictionary<ulong,string> networkObjectIdToName, ulong bytesCount);
        void DispatchFrame();
    }
}
