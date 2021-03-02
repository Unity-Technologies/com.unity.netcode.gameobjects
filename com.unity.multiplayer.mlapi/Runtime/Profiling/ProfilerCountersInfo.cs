using System;
#if UNITY_2020_2_OR_NEWER
using Unity.Profiling;
#endif
using UnityEngine;

namespace MLAPI.Profiling
{

    internal static class ProfilerCountersInfo
    {
#if UNITY_2020_2_OR_NEWER && ENABLE_PROFILER
        // Operations
        private static ProfilerCounterValue<int> s_ConnectionsCounterValue =
        new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfConnections,
            ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_TickRateCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.ReceiveTickRate,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        //Messages
        private static ProfilerCounterValue<int> s_NamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfNamedMessages,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_UnnamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfUnnamedMessages,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_BytesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberBytesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_BytesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberBytesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_NetworkVarsCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberNetworkVarsReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        //RPCs
        private static ProfilerCounterValue<int> s_RPCsSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_RPCsReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_RPCBatchesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCBatchesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_RPCBatchesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCBatchesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_RPCQueueProcessedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCQueueProcessed,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_RPCsInQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsInQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<int> s_RPCsOutQueueSizeCounterValue =
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
            s_ConnectionsCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfConnections);
            s_TickRateCounterValue.Value += tickData.GetData(ProfilerConstants.ReceiveTickRate);

            //Messages
            s_NamedMessagesCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfNamedMessages);
            s_UnnamedMessagesCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfUnnamedMessages);
            s_BytesSentCounterValue.Value += tickData.GetData(ProfilerConstants.NumberBytesSent);
            s_BytesReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberBytesReceived);
            s_NetworkVarsCounterValue.Value += tickData.GetData(ProfilerConstants.NumberNetworkVarsReceived);

            //RPCs
            s_RPCsSentCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsSent);
            s_RPCsReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsReceived);
            s_RPCBatchesSentCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCBatchesSent);
            s_RPCBatchesReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCBatchesReceived);
            s_RPCBatchesReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCBatchesReceived);
            s_RPCQueueProcessedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCQueueProcessed);
            s_RPCsInQueueSizeCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsInQueueSize);
            s_RPCsOutQueueSizeCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsOutQueueSize);
        }
#endif
    }
}
