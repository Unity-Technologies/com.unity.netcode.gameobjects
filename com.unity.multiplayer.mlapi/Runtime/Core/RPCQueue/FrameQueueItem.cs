using System;
using MLAPI.Security;
using MLAPI.Serialization.Pooled;

namespace MLAPI
{
    /// <summary>
    /// FrameQueueItem
    /// Container structure for RPCs written to the Queue Frame
    /// Used for both Inbound and Outbound RPCs
    /// NOTE: This structure will change in the near future and is in a state of flux.
    /// This will include removing specific properties or changing property types (i.e. Channel could become a byte value)
    /// </summary>
    public struct FrameQueueItem
    {
        public RPCQueueContainer.QueueItemType QueueItemType;
        public SecuritySendFlags        SendFlags;
        public ulong                    NetworkId;          //Sender's network Identifier
        public string                   Channel;
        public ulong[]                  ClientIds;          //Server invoked Client RPCs only
        public long                     StreamSize;
        public PooledBitWriter          StreamWriter;
        public PooledBitReader          StreamReader;
        public PooledBitStream          ItemStream;
        public ArraySegment<byte>       MessageData;
    }
}
