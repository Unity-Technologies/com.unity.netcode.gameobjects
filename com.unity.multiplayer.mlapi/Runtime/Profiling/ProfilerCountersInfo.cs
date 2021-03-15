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
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.Connection,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_TickRateCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.ReceiveTickRate,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        // Messages
        private static readonly ProfilerCounterValue<int> k_NamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NamedMessageReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_UnnamedMessagesCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.UnnamedMessageReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_BytesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.ByteSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_BytesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.ByteReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_NetworkVarsCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NetworkVarReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        // RPCs
        private static readonly ProfilerCounterValue<int> k_RPCsSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCsReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCBatchesSentCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcBatchesSent,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCBatchesReceivedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcBatchesReceived,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCQueueProcessedCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcQueueProcessed,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCsInQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcInQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static readonly ProfilerCounterValue<int> k_RPCsOutQueueSizeCounterValue =
            new ProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.RpcOutQueueSize,
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        [RuntimeInitializeOnLoadMethod]
        private static void RegisterMLAPIPerformanceEvent()
        {
            NetworkManager.OnPerformanceDataEvent += OnPerformanceTickData;
        }

        private static void OnPerformanceTickData(PerformanceTickData tickData)
        {
            // Operations
            k_ConnectionsCounterValue.Value = tickData.GetData(ProfilerConstants.Connection);
            k_TickRateCounterValue.Value = tickData.GetData(ProfilerConstants.ReceiveTickRate);

            // Messages
            k_NamedMessagesCounterValue.Value = tickData.GetData(ProfilerConstants.NamedMessageReceived);
            k_UnnamedMessagesCounterValue.Value = tickData.GetData(ProfilerConstants.UnnamedMessageReceived);
            k_BytesSentCounterValue.Value = tickData.GetData(ProfilerConstants.ByteSent);
            k_BytesReceivedCounterValue.Value = tickData.GetData(ProfilerConstants.ByteReceived);
            k_NetworkVarsCounterValue.Value = tickData.GetData(ProfilerConstants.NetworkVarReceived);

            // RPCs
            k_RPCsSentCounterValue.Value = tickData.GetData(ProfilerConstants.RpcSent);
            k_RPCsReceivedCounterValue.Value = tickData.GetData(ProfilerConstants.RpcReceived);
            k_RPCBatchesSentCounterValue.Value = tickData.GetData(ProfilerConstants.RpcBatchesSent);
            k_RPCBatchesReceivedCounterValue.Value = tickData.GetData(ProfilerConstants.RpcBatchesReceived);
            k_RPCBatchesReceivedCounterValue.Value = tickData.GetData(ProfilerConstants.RpcBatchesReceived);
            k_RPCQueueProcessedCounterValue.Value = tickData.GetData(ProfilerConstants.RpcQueueProcessed);
            k_RPCsInQueueSizeCounterValue.Value = tickData.GetData(ProfilerConstants.RpcInQueueSize);
            k_RPCsOutQueueSizeCounterValue.Value = tickData.GetData(ProfilerConstants.RpcOutQueueSize);
        }
#endif
    }
}
