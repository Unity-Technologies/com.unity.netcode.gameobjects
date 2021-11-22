using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.Netcode
{
    [BurstCompile]
    public unsafe struct MetricsPipelineStage : INetworkPipelineStage
    {
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);

        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] param)
        {
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: UnsafeUtility.SizeOf<int>(),
                SendCapacity: UnsafeUtility.SizeOf<int>(),
                HeaderCapacity: UnsafeUtility.SizeOf<ushort>(),
                SharedStateCapacity: 0
            );
        }

        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext pipelineContext, ref InboundRecvBuffer inboundRecvBuffer, ref NetworkPipelineStage.Requests requests, int systemHeaderSize)
        {
            Debug.Log("RECEIVING PACKET");
            UnityTransport.Metrics.TrackPacketReceived(1);
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext context, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests requests, int systemHeaderSize)
        {
            Debug.Log("SENDING PACKET");
            UnityTransport.Metrics.TrackPacketSent(1 );
            return 0;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.InitializeConnectionDelegate))]
        private static void InitializeConnection(byte* staticInstanceBuffer, int staticInstanceBufferLength,
            byte* sendProcessBuffer, int sendProcessBufferLength, byte* recvProcessBuffer, int recvProcessBufferLength,
            byte* sharedProcessBuffer, int sharedProcessBufferLength)
        {
            Debug.Log("CONNECTION INITIALIZE");
        }
    }
}
