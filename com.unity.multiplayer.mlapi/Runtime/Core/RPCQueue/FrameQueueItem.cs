using System;
using MLAPI.Security;
using MLAPI.Serialization.Pooled;

namespace  MLAPI.Messaging
{
    /// <summary>
    /// FrameQueueItem
    /// Container structure for RPCs written to the Queue Frame
    /// Used for both Inbound and Outbound RPCs
    /// NOTE: This could eventually become obsolete as other systems mature
    /// </summary>
    public struct FrameQueueItem
    {
        public  NetworkUpdateManager.NetworkUpdateStages updateStage;
        public RpcQueueContainer.QueueItemType           queueItemType;
        public SecuritySendFlags                         sendFlags;
        public ulong                                     networkId;          //Sender's network Identifier
        public string                                    channel;
        public ulong[]                                   clientIds;          //Server invoked Client RPCs only
        public long                                      streamSize;
        public float                                     timeStamp;
        public PooledBitWriter                           streamWriter;
        public PooledBitReader                           streamReader;
        public PooledBitStream                           itemStream;
        public ArraySegment<byte>                        messageData;
    }
}
