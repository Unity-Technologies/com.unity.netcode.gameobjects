#if MULTIPLAYER_TOOLS
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;

namespace Unity.Netcode.Transports.UTP
{
    [BurstCompile]
    internal unsafe struct NetworkMetricsPipelineStage : INetworkPipelineStage
    {
        private static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> s_ReceiveFunction = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        private static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> s_SendFunction = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        private static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> s_InitializeConnectionFunction = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);

        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer,
            int staticInstanceBufferLength,
            NetworkSettings settings)
        {
            return new NetworkPipelineStage(
                s_ReceiveFunction,
                s_SendFunction,
                s_InitializeConnectionFunction,
                ReceiveCapacity: 0,
                SendCapacity: 0,
                HeaderCapacity: 0,
                SharedStateCapacity: UnsafeUtility.SizeOf<NetworkMetricsContext>());
        }

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
