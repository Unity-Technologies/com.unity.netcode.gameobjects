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
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        // Messages
        private static readonly ProfilerCounterValue<int> k_NamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NamedMessagesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_UnnamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.UnnamedMessagesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_BytesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.BytesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_BytesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.BytesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_NetworkVarsCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NetworkVarsReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        // RPCs
        private static readonly ProfilerCounterValue<int> k_RPCsSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RPCsSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCsReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RPCsReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCBatchesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RPCBatchesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCBatchesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RPCBatchesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCQueueProcessedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RPCQueueProcessed,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCsInQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RPCsInQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCsOutQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RPCsOutQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        [RuntimeInitializeOnLoadMethod]
        private static void RegisterMLAPIPerformanceEvent()
        {
            NetworkManager.OnPerformanceDataEvent += OnPerformanceTickData;
        }

        private static void OnPerformanceTickData(PerformanceTickData tickData)
        {
            // Operations
            k_ConnectionsCounterValue.Value = tickData.GetData(ProfilerConstants.Connections);
            k_TickRateCounterValue.Value = tickData.GetData(ProfilerConstants.ReceiveTickRate);

            // Messages
            k_NamedMessagesCounterValue.Value = tickData.GetData(ProfilerConstants.NamedMessagesReceived);
            k_UnnamedMessagesCounterValue.Value = tickData.GetData(ProfilerConstants.UnnamedMessagesReceived);
            k_BytesSentCounterValue.Value = tickData.GetData(ProfilerConstants.BytesSent);
            k_BytesReceivedCounterValue.Value = tickData.GetData(ProfilerConstants.BytesReceived);
            k_NetworkVarsCounterValue.Value = tickData.GetData(ProfilerConstants.NetworkVarsReceived);

            // RPCs
            k_RPCsSentCounterValue.Value = tickData.GetData(ProfilerConstants.RPCsSent);
            k_RPCsReceivedCounterValue.Value = tickData.GetData(ProfilerConstants.RPCsReceived);
            k_RPCBatchesSentCounterValue.Value = tickData.GetData(ProfilerConstants.RPCBatchesSent);
            k_RPCBatchesReceivedCounterValue.Value = tickData.GetData(ProfilerConstants.RPCBatchesReceived);
            k_RPCBatchesReceivedCounterValue.Value = tickData.GetData(ProfilerConstants.RPCBatchesReceived);
            k_RPCQueueProcessedCounterValue.Value = tickData.GetData(ProfilerConstants.RPCQueueProcessed);
            k_RPCsInQueueSizeCounterValue.Value = tickData.GetData(ProfilerConstants.RPCsInQueueSize);
            k_RPCsOutQueueSizeCounterValue.Value = tickData.GetData(ProfilerConstants.RPCsOutQueueSize);
        }
#endif
    }
}
