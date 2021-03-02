using System;
using MLAPI.Transports;
using MLAPI.Serialization.Pooled;

namespace MLAPI.Messaging
{
    /// <summary>
    /// FrameQueueItem
    /// Container structure for RPCs written to the Queue Frame
    /// Used for both Inbound and Outbound RPCs
    /// NOTE: This structure will change in the near future and is in a state of flux.
    /// This will include removing specific properties or changing property types
    /// </summary>
    internal struct RpcFrameQueueItem
    {
        public NetworkUpdateStage UpdateStage;
        public RpcQueueContainer.QueueItemType QueueItemType;
        public ulong NetworkId; //Sender's network Identifier
        public NetworkChannel NetworkChannel;
        public ulong[] ClientNetworkIds; //Server invoked Client RPCs only
        public long StreamSize;
        public float Timestamp;
        public PooledNetworkWriter NetworkWriter;
        public PooledNetworkReader NetworkReader;
        public PooledNetworkBuffer NetworkBuffer;
        public ArraySegment<byte> MessageData;
    }
}