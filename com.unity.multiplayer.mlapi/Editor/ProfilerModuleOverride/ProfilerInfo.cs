using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MLAPI.Profiling;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEditorInternal;
using UnityEngine.Profiling;

namespace ProfilerModuleOverride
{
    public static class ProfilerInfo
    {
        // Operations
        public static MLAPIProfilerCounterValue<int> ConnectionsCounterValue =
        new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfConnections.ToString(),
            ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | Pro);

        public static MLAPIProfilerCounterValue<int> TickRateCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.ReceiveTickRate.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> TransportSendsCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfTransportSends.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> TransportSendQueuesCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfTransportSendQueues.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        //Messages
        public static MLAPIProfilerCounterValue<int> NamedMessagesCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfNamedMessages.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> UnnamedMessagesCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfUnnamedMessages.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> BytesSentCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberBytesSent.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> BytesReceivedCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberBytesReceived.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> NetworkVarsCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberNetworkVarsReceived.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        //RPCs
        public static MLAPIProfilerCounterValue<int> RPCsSentCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsSent.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> RPCsReceivedCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsReceived.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> RPCBatchesSentCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCBatchesSent.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> RPCBatchesReceivedCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCBatchesReceived.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> RPCQueueProcessedCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCQueueProcessed.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> RPCsInQueueSizeCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsInQueueSize.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        public static MLAPIProfilerCounterValue<int> RPCsOutQueueSizeCounterValue =
            new MLAPIProfilerCounterValue<int>(ProfilerCategory.Network, ProfilerConstants.NumberOfRPCsOutQueueSize.ToString(),
                ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);



        public static readonly Guid kNetworkingOperationsProfilerModuleGUID = new Guid("42c5aec2-fb86-4172-a384-34063f1bd332");

        public static readonly int kClientDataTag = 0;

        [Conditional("ENABLE_PROFILER")]
        public static void EmitClientToProfilerStream(string targetName, TickType eventType, uint bytes, string channelName, string messageName)
        {
            if (!Profiler.enabled)
                return;
            var clientInfo = new ClientInfo();
            var targetNameBytes = Encoding.ASCII.GetBytes(targetName);
            var channelNameBytes = Encoding.ASCII.GetBytes(channelName);
            var messageNameBytes = Encoding.ASCII.GetBytes(messageName);

            clientInfo.type = eventType;
            clientInfo.bytesSent = bytes;

            unsafe {
                fixed (byte* pSource = targetNameBytes) {
                    UnsafeUtility.MemCpy(clientInfo.targetName, pSource, targetNameBytes.Length);
                }
                fixed (byte* pSource = channelNameBytes) {
                    UnsafeUtility.MemCpy(clientInfo.channelName, pSource, channelNameBytes.Length);
                }
                fixed (byte* pSource = messageNameBytes) {
                    UnsafeUtility.MemCpy(clientInfo.messageName, pSource, messageNameBytes.Length);
                }
            }

            ConnectionsCounterValue.Value = 1;
            TickRateCounterValue.Value = 1;
            TransportSendsCounterValue.Value = 1;
            TransportSendQueuesCounterValue.Value = 1;

            NamedMessagesCounterValue.Value = 1;
            UnnamedMessagesCounterValue.Value = 1;
            BytesSentCounterValue.Value = 1;
            BytesReceivedCounterValue.Value = 1;
            NetworkVarsCounterValue.Value = 1;

            RPCsSentCounterValue.Value = 1;
            RPCsReceivedCounterValue.Value = 1;
            RPCBatchesSentCounterValue.Value = 1;
            RPCBatchesReceivedCounterValue.Value = 1;
            RPCQueueProcessedCounterValue.Value = 1;
            RPCsInQueueSizeCounterValue.Value = 1;
            RPCsOutQueueSizeCounterValue.Value = 1;

            ProfilerStatManager.bytesSent.Record((int)bytes);
            Profiler.EmitFrameMetaData(ProfilerInfo.kNetworkingOperationsProfilerModuleGUID, kClientDataTag, new[] { clientInfo });
        }
    }
}
