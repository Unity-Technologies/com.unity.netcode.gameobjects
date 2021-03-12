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
        private static readonly ProfilerCounterValue<int> k_ConnectionsCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.Connections,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_TickRateCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.ReceiveTickRate,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        // Messages
        private static readonly ProfilerCounterValue<int> k_NamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfNamedMessages,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_UnnamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfUnnamedMessages,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_BytesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberBytesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_BytesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberBytesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_NetworkVarsCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberNetworkVarsReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        // RPCs
        private static readonly ProfilerCounterValue<int> k_RPCsSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCsReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCBatchesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCBatchesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCBatchesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCBatchesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCQueueProcessedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCQueueProcessed,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCsInQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsInQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCsOutQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsOutQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        [RuntimeInitializeOnLoadMethod]
        private static void RegisterMLAPIPerformanceEvent()
        {
            NetworkManager.OnPerformanceDataEvent += OnPerformanceTickData;
        }

        private static void OnPerformanceTickData(PerformanceTickData tickData)
        {
            // Operations
            UpdateIntCounter(tickData, k_ConnectionsCounterValue, ProfilerConstants.Connections);
            UpdateIntCounter(tickData, k_TickRateCounterValue, ProfilerConstants.ReceiveTickRate);

            // Messages
            UpdateIntCounter(tickData, k_NamedMessagesCounterValue, ProfilerConstants.NumberOfNamedMessages);
            UpdateIntCounter(tickData, k_UnnamedMessagesCounterValue, ProfilerConstants.NumberOfUnnamedMessages);
            UpdateIntCounter(tickData, k_BytesSentCounterValue, ProfilerConstants.NumberBytesSent);
            UpdateIntCounter(tickData, k_BytesReceivedCounterValue, ProfilerConstants.NumberBytesReceived);
            UpdateIntCounter(tickData, k_NetworkVarsCounterValue, ProfilerConstants.NumberNetworkVarsReceived);

            // RPCs
            UpdateIntCounter(tickData, k_RPCsSentCounterValue, ProfilerConstants.NumberOfRPCsSent);
            UpdateIntCounter(tickData, k_RPCsReceivedCounterValue, ProfilerConstants.NumberOfRPCsReceived);
            UpdateIntCounter(tickData, k_RPCBatchesSentCounterValue, ProfilerConstants.NumberOfRPCBatchesSent);
            UpdateIntCounter(tickData, k_RPCBatchesReceivedCounterValue, ProfilerConstants.NumberOfRPCBatchesReceived);
            UpdateIntCounter(tickData, k_RPCBatchesReceivedCounterValue, ProfilerConstants.NumberOfRPCQueueProcessed);
            UpdateIntCounter(tickData, k_RPCQueueProcessedCounterValue, ProfilerConstants.NumberOfRPCsInQueueSize);
            UpdateIntCounter(tickData, k_RPCsInQueueSizeCounterValue, ProfilerConstants.NumberOfRPCsOutQueueSize);
        }

        private static void UpdateIntCounter(PerformanceTickData tickData, ProfilerCounterValue<int> counter, string fieldName)
        {
            if (tickData.HasData(fieldName))
            {
                counter.Value += tickData.GetData(fieldName);
            }
        }
#endif
    }
}
