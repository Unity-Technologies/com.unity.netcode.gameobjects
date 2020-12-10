using System;
using System.Collections.Generic;
using MLAPI.Security;
using MLAPI.Serialization.Pooled;


namespace MLAPI
{
    /// <summary>
    /// FrameQueueItem
    /// Container structure for RPCs written to the Queue Frame
    /// Used for both Inbound and Outbound RPCs
    /// NOTE: This could eventually become obsolete as other systems mature
    /// </summary>
    public struct FrameQueueItem
    {       
        public RPCQueueManager.QueueItemType QueueItemType;
        public SecuritySendFlags        SendFlags;
        public float                    TimeStamp;          //WIP: This is a temporary value in place of network tic/frame         
        public ulong                    NetworkId;          //Sender's network Identifier        
        public String                   Channel;
        public List<ulong>              ClientIds;          //Server invoked Client RPCs only
        public long                     StreamSize;
        public PooledBitWriter          StreamWriter;
        public PooledBitReader          StreamReader;
        public PooledBitStream          ItemStream;
        public ArraySegment<byte>       MessageData;
    }
}
