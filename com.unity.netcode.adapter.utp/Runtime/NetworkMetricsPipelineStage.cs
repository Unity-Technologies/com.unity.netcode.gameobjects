#if MULTIPLAYER_TOOLS
#if MULTIPLAYER_TOOLS_1_0_0_PRE_3
using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.Netcode
{
    [BurstCompile]
    internal unsafe struct NetworkMetricsPipelineStage : INetworkPipelineStage
    {
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunction = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunction = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunction = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);

        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer,
            int staticInstanceBufferLength,
            NetworkSettings settings)
        {
            return new NetworkPipelineStage(ReceiveFunction,
                                            SendFunction,
                                            InitializeConnectionFunction,
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
