#if MULTIPLAYER_TOOLS
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;

namespace Unity.Netcode.Transports.UTP
{
    /// <summary>
    /// A pipeline stage that tracks some internal metrics that are then used by the multiplayer
    /// tools package. This should only be used when creating a custom <see cref="NetworkDriver"/>
    /// with <see cref="INetworkStreamDriverConstructor"/> if compatibility with the multiplayer
    /// tools package is desired. In that situation, this stage needs to be registered with the
    /// constructed driver with <see cref="NetworkDriver.RegisterPipelineStage{T}"/>.
    /// </summary>
    [BurstCompile]
    public unsafe struct NetworkMetricsPipelineStage : INetworkPipelineStage
    {
        /// <inheritdoc/>
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer,
            int staticInstanceBufferLength,
            NetworkSettings settings)
        {
            return new NetworkPipelineStage(
                new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive),
                new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send),
                new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection),
                ReceiveCapacity: 0,
                SendCapacity: 0,
                HeaderCapacity: 0,
                SharedStateCapacity: UnsafeUtility.SizeOf<NetworkMetricsContext>());
        }

        /// <inheritdoc/>
        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext networkPipelineContext,
            ref InboundRecvBuffer inboundReceiveBuffer,
            ref NetworkPipelineStage.Requests requests,
            int systemHeaderSize)
        {
            var networkMetricContext = (NetworkMetricsContext*)networkPipelineContext.internalSharedProcessBuffer;
            networkMetricContext->PacketReceivedCount++;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext networkPipelineContext,
            ref InboundSendBuffer inboundSendBuffer,
            ref NetworkPipelineStage.Requests requests,
            int systemHeaderSize)
        {
            var networkMetricContext = (NetworkMetricsContext*)networkPipelineContext.internalSharedProcessBuffer;
            networkMetricContext->PacketSentCount++;
            return 0;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.InitializeConnectionDelegate))]
        private static void InitializeConnection(byte* staticInstanceBuffer, int staticInstanceBufferLength,
            byte* sendProcessBuffer, int sendProcessBufferLength, byte* receiveProcessBuffer, int receiveProcessBufferLength,
            byte* sharedProcessBuffer, int sharedProcessBufferLength)
        {
            var networkMetricContext = (NetworkMetricsContext*)sharedProcessBuffer;
            networkMetricContext->PacketSentCount = 0;
            networkMetricContext->PacketReceivedCount = 0;
        }
    }
}
#endif
#endif
