using System;
using MLAPI.Transports;
using MLAPI.Serialization.Pooled;

namespace  MLAPI.Messaging
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
        public NetworkUpdateStage                        updateStage;
        public RpcQueueContainer.QueueItemType           queueItemType;
        public ulong                                     networkId;          //Sender's network Identifier
        public NetworkChannel                            networkChannel;
        public ulong[]                                   clientIds;          //Server invoked Client RPCs only
        public long                                      streamSize;
        public float                                     timeStamp;
        public PooledNetworkWriter                       streamWriter;
        public PooledNetworkReader                       streamReader;
        public PooledNetworkBuffer                       itemBuffer;
        public ArraySegment<byte>                        messageData;
    }
}
