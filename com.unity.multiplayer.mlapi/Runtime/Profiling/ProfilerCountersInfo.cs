using System;
#if UNITY_2020_2_OR_NEWER
using Unity.Profiling;
#endif
using UnityEngine;

namespace MLAPI.Profiling
{

    public static class ProfilerCountersInfo
    {
#if UNITY_2020_2_OR_NEWER && ENABLE_PROFILER
        // Operations
        public static ProfilerCounterValue<int> ConnectionsCounterValue =
        new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfConnections,
            ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> TickRateCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.ReceiveTickRate,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        //Messages
        public static ProfilerCounterValue<int> NamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfNamedMessages,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> UnnamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfUnnamedMessages,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> BytesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberBytesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> BytesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberBytesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> NetworkVarsCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberNetworkVarsReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        //RPCs
        public static ProfilerCounterValue<int> RPCsSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> RPCsReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> RPCBatchesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCBatchesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> RPCBatchesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCBatchesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> RPCQueueProcessedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCQueueProcessed,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> RPCsInQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsInQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static ProfilerCounterValue<int> RPCsOutQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsOutQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        [RuntimeInitializeOnLoadMethod]
        static void RegisterMLAPIPerformanceEvent()
        {
            NetworkingManager.OnPerformanceDataEvent += OnPerformanceTickData;
        }

        static void OnPerformanceTickData(PerformanceTickData tickData)
        {
            //Operations
            ConnectionsCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfConnections);
            TickRateCounterValue.Value += tickData.GetData(ProfilerConstants.ReceiveTickRate);

            //Messages
            NamedMessagesCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfNamedMessages);
            UnnamedMessagesCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfUnnamedMessages);
            BytesSentCounterValue.Value += tickData.GetData(ProfilerConstants.NumberBytesSent);
            BytesReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberBytesReceived);
            NetworkVarsCounterValue.Value += tickData.GetData(ProfilerConstants.NumberNetworkVarsReceived);

            //RPCs
            RPCsSentCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsSent);
            RPCsReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsReceived);
            RPCBatchesSentCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCBatchesSent);
            RPCBatchesReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCBatchesReceived);
            RPCBatchesReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCBatchesReceived);
            RPCQueueProcessedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCQueueProcessed);
            RPCsInQueueSizeCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsInQueueSize);
            RPCsOutQueueSizeCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsOutQueueSize);
        }
#endif
    }
}
