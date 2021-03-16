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
        private static readonly ProfilerCounterValue<int> k_NamedMessageReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NamedMessageReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_UnnamedMessageReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.UnnamedMessageReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_NamedMessageSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NamedMessageSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_UnnamedMessageSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.UnnamedMessageSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_BytesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.ByteSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_BytesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.ByteReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_NetworkVarDeltasCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NetworkVarDeltas,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_NetworkVarUpdatesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NetworkVarUpdates,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        // RPCs
        private static readonly ProfilerCounterValue<int> k_RPCsSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCsReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCBatchesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcBatchesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCBatchesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcBatchesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCQueueProcessedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcQueueProcessed,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCsInQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcInQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static readonly ProfilerCounterValue<int> k_RPCsOutQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcOutQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        [RuntimeInitializeOnLoadMethod]
        private static void RegisterMLAPIPerformanceEvent()
        {
            InitializeCounters();
            NetworkManager.OnPerformanceDataEvent += OnPerformanceTickData;
        }

        private static void InitializeCounters()
        {
            k_ConnectionsCounterValue.Value = 0;
            k_TickRateCounterValue.Value = 0;

            k_NamedMessageReceivedCounterValue.Value = 0;
            k_UnnamedMessageReceivedCounterValue.Value = 0;
            k_NamedMessageSentCounterValue.Value = 0;
            k_UnnamedMessageSentCounterValue.Value = 0;
            k_BytesSentCounterValue.Value = 0;
            k_BytesReceivedCounterValue.Value = 0;
            k_NetworkVarDeltasCounterValue.Value = 0;
            k_NetworkVarUpdatesCounterValue.Value = 0;

            k_RPCsSentCounterValue.Value = 0;
            k_RPCsReceivedCounterValue.Value = 0;
            k_RPCBatchesSentCounterValue.Value = 0;
            k_RPCBatchesReceivedCounterValue.Value = 0;
            k_RPCQueueProcessedCounterValue.Value = 0;
            k_RPCsInQueueSizeCounterValue.Value = 0;
            k_RPCsOutQueueSizeCounterValue.Value = 0;
        }

        private static void OnPerformanceTickData(PerformanceTickData tickData)
        {
            // Operations
            UpdateIntCounter(tickData, k_ConnectionsCounterValue, ProfilerConstants.Connections);
            UpdateIntCounter(tickData, k_TickRateCounterValue, ProfilerConstants.ReceiveTickRate);

            // Messages
            UpdateIntCounter(tickData, k_NamedMessageReceivedCounterValue, ProfilerConstants.NamedMessageReceived);
            UpdateIntCounter(tickData, k_UnnamedMessageReceivedCounterValue, ProfilerConstants.UnnamedMessageReceived);
            UpdateIntCounter(tickData, k_NamedMessageSentCounterValue, ProfilerConstants.NamedMessageSent);
            UpdateIntCounter(tickData, k_UnnamedMessageSentCounterValue, ProfilerConstants.UnnamedMessageSent);
            UpdateIntCounter(tickData, k_BytesSentCounterValue, ProfilerConstants.ByteSent);
            UpdateIntCounter(tickData, k_BytesReceivedCounterValue, ProfilerConstants.ByteReceived);
            UpdateIntCounter(tickData, k_NetworkVarDeltasCounterValue, ProfilerConstants.NetworkVarDeltas);
            UpdateIntCounter(tickData, k_NetworkVarUpdatesCounterValue, ProfilerConstants.NetworkVarUpdates);

            // RPCs
            UpdateIntCounter(tickData, k_RPCsSentCounterValue, ProfilerConstants.RpcSent);
            UpdateIntCounter(tickData, k_RPCsReceivedCounterValue, ProfilerConstants.RpcReceived);
            UpdateIntCounter(tickData, k_RPCBatchesSentCounterValue, ProfilerConstants.RpcBatchesSent);
            UpdateIntCounter(tickData, k_RPCBatchesReceivedCounterValue, ProfilerConstants.RpcBatchesReceived);
            UpdateIntCounter(tickData, k_RPCBatchesReceivedCounterValue, ProfilerConstants.RpcQueueProcessed);
            UpdateIntCounter(tickData, k_RPCQueueProcessedCounterValue, ProfilerConstants.RpcInQueueSize);
            UpdateIntCounter(tickData, k_RPCsInQueueSizeCounterValue, ProfilerConstants.RpcOutQueueSize);
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
