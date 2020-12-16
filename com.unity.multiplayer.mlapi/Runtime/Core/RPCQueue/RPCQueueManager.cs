using System;
using System.Collections.Generic;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using MLAPI.Profiling;
using MLAPI.Configuration;

namespace MLAPI
{
    /// <summary>
    /// RPCQueueManager (Singleton Class)
    /// Handles the management of the RPC Queue which includes specifying when the Inbound or Outbound queue should be processed.
    ///
    /// </summary>
    public class RPCQueueManager
    {
        public enum QueueItemType
        {
            ServerRPC,
            ClientRPC,
            CreateObject,                   //MLAPI Constant *** We need to determine if these belong here ***
            DestroyObject,                  //MLAPI Constant

            NONE                            //Indicates end of frame
        }

        public enum RPCQueueProcessingTypes
        {
            SEND,
            RECEIVE,
        }

        private RPCQueueProcessing rpcQueueProcessing;

        private UInt32 OutboundFramesProcessed;
        private UInt32 InboundFramesProcessed;
        private UInt32 MaxFrameHistory;

        private int InboundStreamBufferIndex;
        private int OutBoundStreamBufferIndex;
        private bool IsTestingEnabled;
        private bool IsLoopbackEnabled;

        private Dictionary<QueueHistoryFrame.QueueFrameType, Dictionary<int,QueueHistoryFrame>> QueueHistory = new Dictionary<QueueHistoryFrame.QueueFrameType, Dictionary<int, QueueHistoryFrame>>();



        /// <summary>
        /// IsTesting
        /// Whether we are in testing mode or not (generally used for testing or debugging)
        /// </summary>
        /// <returns>true or false</returns>
        public bool IsTesting()
        {
            return IsTestingEnabled;
        }

        /// <summary>
        /// IsLoopBack
        /// Whether we are in loopback mode or not (generally used for testing or debugging)
        /// </summary>
        /// <returns>true or false</returns>

        public bool IsLoopBack()
        {
            return IsLoopbackEnabled;
        }

        /// <summary>
        /// GetStreamBufferFrameCount
        /// Returns how many frames have been processed (Inbound/Outbound)
        /// </summary>
        /// <param name="queueType"></param>
        /// <returns>number of frames procssed</returns>
        public uint GetStreamBufferFrameCount(QueueHistoryFrame.QueueFrameType queueType)
        {
            return queueType == QueueHistoryFrame.QueueFrameType.Inbound ? InboundFramesProcessed:OutboundFramesProcessed;
        }

        /// <summary>
        /// AddToInternalMLAPISendQueue
        /// NSS-TODO: This will need to be removed once we determine how we want to handle specific
        /// internal MLAPI commands relative to RPCS.
        /// Example: An network object is destroyed via server side (internal mlapi) command, but prior to this several RPCs are invoked for the to be destroyed object (Client RPC)
        /// If both the DestroyObject internal mlapi command and the ClientRPCs are received in the same frame but the internal mlapi DestroyObject command is processed prior to the
        /// RPCs being invoked then the object won't exist and additional warnings will be logged that the object no longer exists.
        /// The vices versa scenario (create and then RPCs sent) is an unlikely/improbable scenario, but just in case added the CreateObject to this special case scenario.
        ///
        /// To avoid the DestroyObject scenario, the internal MLAPI commands (DestroyObject and CreateObject) are always invoked after RPCs.
        /// </summary>
        /// <param name="queueItem">item to add to the internal MLAPI queue</param>
        public void AddToInternalMLAPISendQueue(FrameQueueItem queueItem)
        {
            if(rpcQueueProcessing != null)
            {
                rpcQueueProcessing.QueueInternalMLAPICommand(queueItem);
            }
        }

        /// <summary>
        /// ProcessAndFlushRPCQueue
        /// Will process the RPC queue and then move to the next available frame
        /// </summary>
        /// <param name="queueType"></param>
        public void ProcessAndFlushRPCQueue(RPCQueueProcessingTypes queueType)
        {
            if(rpcQueueProcessing != null)
            {
                switch(queueType)
                {
                    case RPCQueueProcessingTypes.RECEIVE:
                        {
                            rpcQueueProcessing.ProcessReceiveQueue();
                            break;
                        }
                    case RPCQueueProcessingTypes.SEND:
                        {
                            rpcQueueProcessing.ProcessSendQueue();
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// GetCurrentFrame
        /// Gets the current frame for the Inbound or Outbound queue
        /// </summary>
        /// <param name="qType"></param>
        /// <returns>QueueHistoryFrame</returns>
        public QueueHistoryFrame GetCurrentFrame(QueueHistoryFrame.QueueFrameType qType)
        {
            if(QueueHistory.ContainsKey(qType))
            {
                int StreamBufferIndex = GetStreamBufferIndex(qType);

                if(QueueHistory[qType].ContainsKey(StreamBufferIndex))
                {
                     return QueueHistory[qType][StreamBufferIndex];
                }

            }
            return null;
        }

        /// <summary>
        /// GetStreamBufferIndex
        /// Returns the queue type's current stream buffer index
        /// </summary>
        /// <param name="queueType"></param>
        /// <returns></returns>
        private int GetStreamBufferIndex(QueueHistoryFrame.QueueFrameType queueType)
        {
            return queueType == QueueHistoryFrame.QueueFrameType.Inbound ? InboundStreamBufferIndex:OutBoundStreamBufferIndex;
        }

        /// <summary>
        /// AdvanceFrameHistory
        /// Progresses the current frame to the next QueueHistoryFrame for the QueueHistoryFrame.QueueFrameType.
        /// All other frames other than the current frame is considered the live rollback history
        /// </summary>
        /// <param name="queueType"></param>
        public void AdvanceFrameHistory(QueueHistoryFrame.QueueFrameType queueType)
        {
            try
            {

                int StreamBufferIndex = GetStreamBufferIndex(queueType);

                if (!QueueHistory.ContainsKey(queueType))
                {
                    UnityEngine.Debug.LogError("You must initialize the RPCQueueManager before using MLAPI!");
                    return;
                }

                if(!QueueHistory[queueType].ContainsKey(StreamBufferIndex))
                {
                    UnityEngine.Debug.LogError("RPCQueueManager " + queueType.ToString() + " queue stream buffer index out of range! [" + StreamBufferIndex.ToString() + "]");
                    return;
                }

                QueueHistoryFrame queueHistoryItem = QueueHistory[queueType][StreamBufferIndex];

                if (queueHistoryItem.QueueItemOffsets.Count > 0 )
                {

                    if(queueType == QueueHistoryFrame.QueueFrameType.Inbound)
                    {
                        ProfilerStatManager.rpcInQueueSize.Record((int)queueHistoryItem.TotalSize);
                    }
                    else
                    {
                        ProfilerStatManager.rpcOutQueueSize.Record((int)queueHistoryItem.TotalSize);
                    }

                    //Roll to the next stream buffer
                    StreamBufferIndex++;

                    //If we have hit our maximum history, roll back over to the first one
                    if(StreamBufferIndex >= MaxFrameHistory)
                    {
                        StreamBufferIndex = 0;
                    }

                    if(queueType == QueueHistoryFrame.QueueFrameType.Inbound)
                    {
                        InboundStreamBufferIndex = StreamBufferIndex;
                    }
                    else
                    {
                        OutBoundStreamBufferIndex = StreamBufferIndex;
                    }


                    //If we already have a frame stored in this next queue history item, then clear it out for
                    //next frame when processed
                    if (QueueHistory[queueType].ContainsKey(StreamBufferIndex))
                    {
                        ResetQueueHistoryFrame(QueueHistory[queueType][StreamBufferIndex]);
                        IncrementAndSetQueueHistoryFrame(QueueHistory[queueType][StreamBufferIndex]);
                    }
                }
            }
            catch(Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
            }
        }

        /// <summary>
        /// IncrementAndSetQueueHistoryFrame
        /// Increments and sets frame count for this queue frame
        /// </summary>
        /// <param name="queueFrame">QueueHistoryFrame to be reset</param>
        void IncrementAndSetQueueHistoryFrame(QueueHistoryFrame queueFrame)
        {
            if(queueFrame.GetQueueFrameType() == QueueHistoryFrame.QueueFrameType.Inbound)
            {
                InboundFramesProcessed++;
                queueFrame.FrameNumber = InboundFramesProcessed;
            }
            else
            {
                OutboundFramesProcessed++;
                queueFrame.FrameNumber = OutboundFramesProcessed;
            }
        }

        /// <summary>
        /// ResetQueueHistoryFrame
        /// Resets the queue history frame passed to this method
        /// </summary>
        /// <param name="queueFrame">QueueHistoryFrame to be reset</param>
        void ResetQueueHistoryFrame(QueueHistoryFrame queueFrame)
        {
            queueFrame.TotalPackedSize = 0;
            queueFrame.TotalSize = 0;
            queueFrame.QueueItemOffsets.Clear();
            queueFrame.QueueStream.Position = 0;
            queueFrame.MarkCurrentStreamPosition();
        }




        /// <summary>
        /// AddQueueItemToInboundFrame
        /// Adds an RPC queue item to the outbound frame
        /// </summary>
        /// <param name="qItemType">type of rpc (client or server)</param>
        /// <param name="timeStamp">when it was received</param>
        /// <param name="sourceNetworkId">who sent the rpc</param>
        /// <param name="message">the message being received</param>
        public void AddQueueItemToInboundFrame(QueueItemType qItemType, float timeStamp, ulong sourceNetworkId, BitStream message)
        {

            QueueHistoryFrame queueHistoryItem = GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound);

            long StartPosition = queueHistoryItem.QueueStream.Position;

            //Write the packed version of the queueItem to our current queue history buffer
            queueHistoryItem.QueueWriter.WriteUInt16((ushort)qItemType);
            queueHistoryItem.QueueWriter.WriteUInt16((ushort)0);
            queueHistoryItem.QueueWriter.WriteSingle(timeStamp);
            queueHistoryItem.QueueWriter.WriteUInt64(sourceNetworkId);

            //NSS-TODO: Determine if we need to store the channel (Inbound only)
            queueHistoryItem.QueueWriter.WriteByte(0);

            //NSS-TODO: Determine if we need to store the clients (Inbound only)
            queueHistoryItem.QueueWriter.WriteInt32(0);

            //Always make sure we are positioned at the start of the stream
            long streamSize = message.Length;
            queueHistoryItem.QueueWriter.WriteInt64(streamSize);
            queueHistoryItem.QueueWriter.WriteInt64(message.Position);
            queueHistoryItem.QueueWriter.WriteBytes(message.GetBuffer(), streamSize);

            //Add the packed size to the offsets for parsing over various entries
            queueHistoryItem.QueueItemOffsets.Add((uint)queueHistoryItem.QueueStream.Position);

            //Calculate the packed size based on stream progression
            queueHistoryItem.TotalSize += (uint)(queueHistoryItem.QueueStream.Position - StartPosition);
        }

        /// <summary>
        /// BeginAddQueueItemToOutboundFrame
        /// Adds a queue item to the outbound queue frame
        /// </summary>
        /// <param name="qItemType">type of rpc (client or server)</param>
        /// <param name="timeStamp">when it was scheduled to be sent</param>
        /// <param name="channel">the channel to send it on</param>
        /// <param name="sendflags">security flags</param>
        /// <param name="sourceNetworkId">who is sending the rpc</param>
        /// <param name="targetNetworkIds">who the rpc is being sent to</param>
        /// <returns></returns>
        public PooledBitWriter BeginAddQueueItemToOutboundFrame(QueueItemType qItemType, float timeStamp, string channel, UInt16 sendflags, ulong sourceNetworkId, ulong[] targetNetworkIds)
        {
            QueueHistoryFrame queueHistoryItem = GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound);

            //Write the packed version of the queueItem to our current queue history buffer
            queueHistoryItem.QueueWriter.WriteUInt16((ushort)qItemType);
            queueHistoryItem.QueueWriter.WriteUInt16((ushort)sendflags);
            queueHistoryItem.QueueWriter.WriteSingle(timeStamp);
            queueHistoryItem.QueueWriter.WriteUInt64(sourceNetworkId);

            //NSS-TODO: Determine if we need to store the channel
            if(channel != String.Empty && channel != null)
            {
                queueHistoryItem.QueueWriter.WriteCharArray(channel.ToCharArray());
            }
            else
            {
                queueHistoryItem.QueueWriter.WriteByte(0);
            }

            if(targetNetworkIds != null && targetNetworkIds.Length != 0)
            {
                queueHistoryItem.QueueWriter.WriteInt32(targetNetworkIds.Length);

                for(int i = 0; i < targetNetworkIds.Length; i++)
                {
                    queueHistoryItem.QueueWriter.WriteUInt64(targetNetworkIds[i]);
                }
            }
            else
            {
                queueHistoryItem.QueueWriter.WriteInt32(0);
            }

            //Mark where we started in the stream to later determine the actual RPC message size (position before writing RPC message vs position after write has completed)
            queueHistoryItem.MarkCurrentStreamPosition();

            //Write a filler dummy size of 0 to hold this position in order to write to it once the RPC is done writing.
            queueHistoryItem.QueueWriter.WriteInt64(0);

            //Return the writer to the invoking method.
            return queueHistoryItem.QueueWriter;
        }

        /// <summary>
        /// EndAddQueueItemToOutboundFrame
        /// Signifies the end of this outbound RPC.
        /// We store final MSG size and track the total current frame queue size
        /// </summary>
        /// <param name="writer">writer that was used</param>
        public void EndAddQueueItemToOutboundFrame(BitWriter writer)
        {

            QueueHistoryFrame queueHistoryItem = GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound);
            PooledBitWriter pbWriter = (PooledBitWriter)writer;

            //Sanity check
            if(pbWriter != queueHistoryItem.QueueWriter)
            {
                 UnityEngine.Debug.LogError("RPCQueueManager " + QueueHistoryFrame.QueueFrameType.Outbound.ToString() + " passed writer is not the same as the current PooledBitWrite for the " +QueueHistoryFrame.QueueFrameType.Outbound.ToString() + "]!");
            }

            //The total size of the frame is the last known position of the stream
            queueHistoryItem.TotalSize = (uint)queueHistoryItem.QueueStream.Position;

            long CurrentPosition = queueHistoryItem.QueueStream.Position;
            ulong BitPosition = queueHistoryItem.QueueStream.BitPosition;

            //////////////////////////////////////////////////////////////
            //>>>> REPOSITIONING STREAM TO RPC MESSAGE SIZE LOCATION <<<<
            //////////////////////////////////////////////////////////////
            queueHistoryItem.QueueStream.Position = queueHistoryItem.GetCurrentMarkedPosition();

            Int64 MSGSize = (Int64)(queueHistoryItem.TotalSize - (queueHistoryItem.GetCurrentMarkedPosition() + 8));
            //Write the actual size of the RPC message
            queueHistoryItem.QueueWriter.WriteInt64(MSGSize);

            //////////////////////////////////////////////////////////////
            //<<<< REPOSITIONING STREAM BACK TO THE CURRENT TAIL >>>>
            //////////////////////////////////////////////////////////////
            queueHistoryItem.QueueStream.Position = CurrentPosition;
            queueHistoryItem.QueueStream.BitPosition = BitPosition;

            //Add the packed size to the offsets for parsing over various entries
            queueHistoryItem.QueueItemOffsets.Add((uint)queueHistoryItem.QueueStream.Position);

        }

        /// <summary>
        /// GetCurrentQueueHistoryFrame
        /// Gets the current queue history frame (inbound or outbound)
        /// </summary>
        /// <param name="frameType">inbound or outbound</param>
        /// <returns>QueueHistoryFrame or null</returns>
        private QueueHistoryFrame GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType frameType)
        {
            int StreamBufferIndex = GetStreamBufferIndex(frameType);

            if (!QueueHistory.ContainsKey(QueueHistoryFrame.QueueFrameType.Outbound))
            {
                UnityEngine.Debug.LogError("You must initialize the RPCQueueManager before using MLAPI!");
                return null;
            }

            if(!QueueHistory[QueueHistoryFrame.QueueFrameType.Outbound].ContainsKey(StreamBufferIndex))
            {
                UnityEngine.Debug.LogError("RPCQueueManager " + frameType.ToString() + " queue stream buffer index out of range! [" + StreamBufferIndex.ToString() + "]");
                return null;
            }
            return QueueHistory[frameType][StreamBufferIndex];
        }




        /// <summary>
        /// LoopbackSendFrame  - WIP
        /// Will copy the contents of the current outbound QueueHistoryFrame to the current inbound QueueHistoryFrame
        /// </summary>
        public void LoopbackSendFrame()
        {
            //If we do not have loop back or testing mode enabled then ignore the call
            if(IsLoopbackEnabled || IsTestingEnabled)
            {

                QueueHistoryFrame queueHistoryItemOutbound = GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound);
                if(queueHistoryItemOutbound.QueueItemOffsets.Count > 0)
                {
                    QueueHistoryFrame queueHistoryItemInbound = GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound);
                    ResetQueueHistoryFrame(queueHistoryItemInbound);
                    PooledBitStream pooledBitStream = PooledBitStream.Get();
                    FrameQueueItem frameQueueItem = queueHistoryItemOutbound.GetFirstQueueItem();
                    byte[] pooledBitStreamArray = pooledBitStream.GetBuffer();
                    while(frameQueueItem.QueueItemType != RPCQueueManager.QueueItemType.NONE)
                    {
                        pooledBitStream.SetLength(frameQueueItem.StreamSize);
                        pooledBitStream.Position = 0;
                        pooledBitStreamArray = pooledBitStream.GetBuffer();
                        Buffer.BlockCopy(frameQueueItem.MessageData.Array, frameQueueItem.MessageData.Offset, pooledBitStreamArray,0,(int)frameQueueItem.StreamSize);
                        pooledBitStream.Position = 1;

                        AddQueueItemToInboundFrame(frameQueueItem.QueueItemType, UnityEngine.Time.realtimeSinceStartup, frameQueueItem.NetworkId, pooledBitStream);
                        frameQueueItem = queueHistoryItemOutbound.GetNextQueueItem();
                    }
                }
            }
        }

        /// <summary>
        /// Initialize
        /// This should be called during primary initialization period (typically during NetworkingManager's Start method)
        /// This will allocate [maxFrameHistory] + [1 currentFrame] number of PooledBitStreams and keep them open until the session ends
        /// Note: For zero frame history set maxFrameHistory to zero
        /// </summary>
        /// <param name="maxFrameHistory"></param>
        public void Initialize(UInt32 maxFrameHistory)
        {
            ClearParameters();

            rpcQueueProcessing = new RPCQueueProcessing(this);

            MaxFrameHistory = maxFrameHistory + 1;

            if(IsLoopbackEnabled && MaxFrameHistory > 1)
            {
                String MSG = "Loopback is enabled but there are (" + MaxFrameHistory.ToString() + ") frames allocated for history!\n";
                MSG += "Adjusting to use 1 frames for loopback mode.  (Initialize RPC Queue Manager with 0 history frame buffers for LoopBack mode)\n";
                UnityEngine.Debug.LogWarning(MSG);
                MaxFrameHistory = 1;
            }

            if(!QueueHistory.ContainsKey(QueueHistoryFrame.QueueFrameType.Inbound))
            {
                QueueHistory.Add(QueueHistoryFrame.QueueFrameType.Inbound, new Dictionary<int, QueueHistoryFrame>());
            }
            if(!QueueHistory.ContainsKey(QueueHistoryFrame.QueueFrameType.Outbound))
            {
                QueueHistory.Add(QueueHistoryFrame.QueueFrameType.Outbound, new Dictionary<int, QueueHistoryFrame>());
            }

            for(int i = 0; i < MaxFrameHistory; i++)
            {
                if (!QueueHistory[QueueHistoryFrame.QueueFrameType.Inbound].ContainsKey(i))
                {
                    QueueHistoryFrame queueHistoryFrame = new QueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound);
                    queueHistoryFrame.FrameNumber = 0;
                    queueHistoryFrame.QueueStream = PooledBitStream.Get();
                    queueHistoryFrame.QueueStream.Position = 0;
                    queueHistoryFrame.QueueWriter = PooledBitWriter.Get(queueHistoryFrame.QueueStream);
                    queueHistoryFrame.QueueReader = PooledBitReader.Get(queueHistoryFrame.QueueStream);
                    queueHistoryFrame.QueueItemOffsets = new List<uint>();
                    QueueHistory[QueueHistoryFrame.QueueFrameType.Inbound].Add(i, queueHistoryFrame);
                }

                if (!QueueHistory[QueueHistoryFrame.QueueFrameType.Outbound].ContainsKey(i))
                {
                    QueueHistoryFrame queueHistoryFrame = new QueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound);
                    queueHistoryFrame.FrameNumber = 0;
                    queueHistoryFrame.QueueStream = PooledBitStream.Get();
                    queueHistoryFrame.QueueStream.Position = 0;
                    queueHistoryFrame.QueueWriter = PooledBitWriter.Get(queueHistoryFrame.QueueStream);
                    queueHistoryFrame.QueueReader = PooledBitReader.Get(queueHistoryFrame.QueueStream);
                    queueHistoryFrame.QueueItemOffsets = new List<uint>();
                    QueueHistory[QueueHistoryFrame.QueueFrameType.Outbound].Add(i, queueHistoryFrame);
                }
            }
        }

        public void SetLoopbackState(bool enabled)
        {
            IsLoopbackEnabled = enabled;
        }


        /// <summary>
        /// Constructor for RPCQueueManager
        /// </summary>
        /// <param name="isLoopBackEnabled">will redirect traffic back to the receive queue buffer (generally used for debugging or testing)</param>
        /// <param name="isTestingEnabled">used for detecting if we are running tests</param>
        public RPCQueueManager(bool isLoopBackEnabled = false, bool isTestingEnabled = false)
        {
            IsTestingEnabled = isTestingEnabled;
            IsLoopbackEnabled = isLoopBackEnabled;
        }

        /// <summary>
        /// Clears all declared parameters
        /// </summary>
        private void ClearParameters()
        {
            InboundStreamBufferIndex = 0;
            OutBoundStreamBufferIndex = 0;
            OutboundFramesProcessed = 0;
            InboundFramesProcessed = 0;
        }

        /// <summary>
        /// OnExiting
        /// Called upon exiting to assure any last messages are delivered.
        /// </summary>
        public void OnExiting()
        {
            if(rpcQueueProcessing != null)
            {
                //We need to make sure all internal messages (i.e. object destroy) are sent
                rpcQueueProcessing.InternalMessagesSendAndFlush();
            }

        }

        /// <summary>
        /// There might be a case where we want to make this class non-static and in that case we would replace this with Dispose and add the IDisposable interface
        /// For now, this should be called upon shutdown
        /// </summary>
        public void Shutdown()
        {

            foreach(KeyValuePair<QueueHistoryFrame.QueueFrameType,Dictionary<int,QueueHistoryFrame>> queueHistorySection in QueueHistory)
            {
                foreach(KeyValuePair<int,QueueHistoryFrame> queueHistoryItem in queueHistorySection.Value)
                {
                    if(queueHistoryItem.Value.QueueWriter != null)
                    {
                        queueHistoryItem.Value.QueueWriter.Dispose();
                    }

                    if(queueHistoryItem.Value.QueueReader != null)
                    {
                        queueHistoryItem.Value.QueueReader.Dispose();
                    }

                    if(queueHistoryItem.Value.QueueStream != null)
                    {
                        queueHistoryItem.Value.QueueStream.Dispose();
                    }
                }
            }
            QueueHistory.Clear();

            ClearParameters();
        }
    }
}
