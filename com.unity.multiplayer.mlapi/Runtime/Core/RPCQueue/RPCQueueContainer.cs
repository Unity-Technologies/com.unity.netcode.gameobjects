using System;
using System.Collections.Generic;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using MLAPI.Profiling;

namespace MLAPI
{
    /// <summary>
    /// RpcQueueContainer
    /// Handles the management of an Rpc Queue
    /// </summary>
    public class RpcQueueContainer
    {
        public enum QueueItemType
        {
            ServerRpc,
            ClientRpc,
            CreateObject, //MLAPI Constant *** We need to determine if these belong here ***
            DestroyObject, //MLAPI Constant

            None //Indicates end of frame
        }

        public enum RPCQueueProcessingTypes
        {
            Send,
            Receive,
        }

        private readonly Dictionary<QueueHistoryFrame.QueueFrameType, Dictionary<int, QueueHistoryFrame>> QueueHistory = new Dictionary<QueueHistoryFrame.QueueFrameType, Dictionary<int, QueueHistoryFrame>>();

        private readonly bool       IsTestingEnabled;
        private RpcQueueProcessing  rpcQueueProcessing;

        private uint    m_OutboundFramesProcessed;
        private uint    m_InboundFramesProcessed;
        private uint    m_MaxFrameHistory;
        private int     m_InboundStreamBufferIndex;
        private int     m_OutBoundStreamBufferIndex;
        private bool    m_IsLoopbackEnabled;

        /// <summary>
        /// IsLoopBack
        /// Whether we are in loopback mode or not (generally used for testing or debugging)
        /// </summary>
        /// <returns>true or false</returns>
        public bool IsLoopBack()
        {
            return m_IsLoopbackEnabled;
        }

        /// <summary>
        /// GetStreamBufferFrameCount
        /// Returns how many frames have been processed (Inbound/Outbound)
        /// </summary>
        /// <param name="queueType"></param>
        /// <returns>number of frames procssed</returns>
        public uint GetStreamBufferFrameCount(QueueHistoryFrame.QueueFrameType queueType)
        {
            return queueType == QueueHistoryFrame.QueueFrameType.Inbound ? m_InboundFramesProcessed:m_OutboundFramesProcessed;
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
            rpcQueueProcessing?.QueueInternalMLAPICommand(queueItem);
        }

        /// <summary>
        /// ProcessAndFlushRPCQueue
        /// Will process the RPC queue and then move to the next available frame
        /// </summary>
        /// <param name="queueType"></param>
        public void ProcessAndFlushRPCQueue(RPCQueueProcessingTypes queueType)
        {
            if (rpcQueueProcessing == null)
            {
                return;
            }

            switch (queueType)
            {
                case RPCQueueProcessingTypes.Receive:
                {
                    rpcQueueProcessing.ProcessReceiveQueue();
                    break;
                }
                case RPCQueueProcessingTypes.Send:
                {
                    rpcQueueProcessing.ProcessSendQueue();
                    break;
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
            if (QueueHistory.ContainsKey(qType))
            {
                int StreamBufferIndex = GetStreamBufferIndex(qType);

                if (QueueHistory[qType].ContainsKey(StreamBufferIndex))
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
            return queueType == QueueHistoryFrame.QueueFrameType.Inbound ? m_InboundStreamBufferIndex : m_OutBoundStreamBufferIndex;
        }

        /// <summary>
        /// AdvanceFrameHistory
        /// Progresses the current frame to the next QueueHistoryFrame for the QueueHistoryFrame.QueueFrameType.
        /// All other frames other than the current frame is considered the live rollback history
        /// </summary>
        /// <param name="queueType"></param>
        public void AdvanceFrameHistory(QueueHistoryFrame.QueueFrameType queueType)
        {
            int StreamBufferIndex = GetStreamBufferIndex(queueType);

            if (!QueueHistory.ContainsKey(queueType))
            {
                UnityEngine.Debug.LogError("You must initialize the RpcQueueContainer before using MLAPI!");
                return;
            }

            if (!QueueHistory[queueType].ContainsKey(StreamBufferIndex))
            {
                UnityEngine.Debug.LogError("RpcQueueContainer " + queueType + " queue stream buffer index out of range! [" + StreamBufferIndex + "]");
                return;
            }

            QueueHistoryFrame queueHistoryItem = QueueHistory[queueType][StreamBufferIndex];

            if (queueHistoryItem.queueItemOffsets.Count > 0)
            {
                if (queueType == QueueHistoryFrame.QueueFrameType.Inbound)
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
                if (StreamBufferIndex >= m_MaxFrameHistory)
                {
                    StreamBufferIndex = 0;
                }

                if (queueType == QueueHistoryFrame.QueueFrameType.Inbound)
                {
                    m_InboundStreamBufferIndex = StreamBufferIndex;
                }
                else
                {
                    m_OutBoundStreamBufferIndex = StreamBufferIndex;
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

        /// <summary>
        /// IncrementAndSetQueueHistoryFrame
        /// Increments and sets frame count for this queue frame
        /// </summary>
        /// <param name="queueFrame">QueueHistoryFrame to be reset</param>
        private void IncrementAndSetQueueHistoryFrame(QueueHistoryFrame queueFrame)
        {
            if (queueFrame.GetQueueFrameType() == QueueHistoryFrame.QueueFrameType.Inbound)
            {
                m_InboundFramesProcessed++;
            }
            else
            {
                m_OutboundFramesProcessed++;
            }
        }

        /// <summary>
        /// ResetQueueHistoryFrame
        /// Resets the queue history frame passed to this method
        /// </summary>
        /// <param name="queueFrame">QueueHistoryFrame to be reset</param>
        private static void ResetQueueHistoryFrame(QueueHistoryFrame queueFrame)
        {
            queueFrame.TotalSize = 0;
            queueFrame.queueItemOffsets.Clear();
            queueFrame.queueStream.Position = 0;
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
        internal void AddQueueItemToInboundFrame(QueueItemType qItemType, float timeStamp, ulong sourceNetworkId, BitStream message)
        {
            QueueHistoryFrame queueHistoryItem = GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound);

            long StartPosition = queueHistoryItem.queueStream.Position;

            //Write the packed version of the queueItem to our current queue history buffer
            queueHistoryItem.queueWriter.WriteUInt16((ushort)qItemType);
            queueHistoryItem.queueWriter.WriteUInt16((ushort)0);
            queueHistoryItem.queueWriter.WriteSingle(timeStamp);
            queueHistoryItem.queueWriter.WriteUInt64(sourceNetworkId);

            //Inbound we copy the entire packet and store the position offset
            long streamSize = message.Length;
            queueHistoryItem.queueWriter.WriteInt64(streamSize);
            queueHistoryItem.queueWriter.WriteInt64(message.Position);
            queueHistoryItem.queueWriter.WriteBytes(message.GetBuffer(), streamSize);

            //Add the packed size to the offsets for parsing over various entries
            queueHistoryItem.queueItemOffsets.Add((uint)queueHistoryItem.queueStream.Position);

            //Calculate the packed size based on stream progression
            queueHistoryItem.TotalSize += (uint)(queueHistoryItem.queueStream.Position - StartPosition);
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
        public PooledBitWriter BeginAddQueueItemToOutboundFrame(QueueItemType qItemType, float timeStamp, string channel, ushort sendflags, ulong sourceNetworkId, ulong[] targetNetworkIds)
        {
            QueueHistoryFrame queueHistoryItem = GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound);

            //Write the packed version of the queueItem to our current queue history buffer
            queueHistoryItem.queueWriter.WriteUInt16((ushort)qItemType);
            queueHistoryItem.queueWriter.WriteUInt16(sendflags);
            queueHistoryItem.queueWriter.WriteSingle(timeStamp);
            queueHistoryItem.queueWriter.WriteUInt64(sourceNetworkId);

            //NSS-TODO: Determine if we need to store the channel
            if (!string.IsNullOrEmpty(channel))
            {
                queueHistoryItem.queueWriter.WriteCharArray(channel.ToCharArray());
            }
            else
            {
                queueHistoryItem.queueWriter.WriteByte(0);
            }

            if (targetNetworkIds != null && targetNetworkIds.Length != 0)
            {
                queueHistoryItem.queueWriter.WriteInt32(targetNetworkIds.Length);

                for (int i = 0; i < targetNetworkIds.Length; i++)
                {
                    queueHistoryItem.queueWriter.WriteUInt64(targetNetworkIds[i]);
                }
            }
            else
            {
                queueHistoryItem.queueWriter.WriteInt32(0);
            }

            //Mark where we started in the stream to later determine the actual RPC message size (position before writing RPC message vs position after write has completed)
            queueHistoryItem.MarkCurrentStreamPosition();

            //Write a filler dummy size of 0 to hold this position in order to write to it once the RPC is done writing.
            queueHistoryItem.queueWriter.WriteInt64(0);

            //Return the writer to the invoking method.
            return queueHistoryItem.queueWriter;
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
            if (pbWriter != queueHistoryItem.queueWriter)
            {
                UnityEngine.Debug.LogError("RpcQueueContainer " + QueueHistoryFrame.QueueFrameType.Outbound + " passed writer is not the same as the current PooledBitWrite for the " + QueueHistoryFrame.QueueFrameType.Outbound + "]!");
            }

            //The total size of the frame is the last known position of the stream
            queueHistoryItem.TotalSize = (uint)queueHistoryItem.queueStream.Position;

            long CurrentPosition = queueHistoryItem.queueStream.Position;
            ulong BitPosition = queueHistoryItem.queueStream.BitPosition;

            //////////////////////////////////////////////////////////////
            //>>>> REPOSITIONING STREAM TO RPC MESSAGE SIZE LOCATION <<<<
            //////////////////////////////////////////////////////////////
            queueHistoryItem.queueStream.Position = queueHistoryItem.GetCurrentMarkedPosition();

            long MSGSize = (long)(queueHistoryItem.TotalSize - (queueHistoryItem.GetCurrentMarkedPosition() + 8));
            //Write the actual size of the RPC message
            queueHistoryItem.queueWriter.WriteInt64(MSGSize);

            //////////////////////////////////////////////////////////////
            //<<<< REPOSITIONING STREAM BACK TO THE CURRENT TAIL >>>>
            //////////////////////////////////////////////////////////////
            queueHistoryItem.queueStream.Position = CurrentPosition;
            queueHistoryItem.queueStream.BitPosition = BitPosition;

            //Add the packed size to the offsets for parsing over various entries
            queueHistoryItem.queueItemOffsets.Add((uint)queueHistoryItem.queueStream.Position);
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
                UnityEngine.Debug.LogError("You must initialize the RpcQueueContainer before using MLAPI!");
                return null;
            }

            if (!QueueHistory[QueueHistoryFrame.QueueFrameType.Outbound].ContainsKey(StreamBufferIndex))
            {
                UnityEngine.Debug.LogError("RpcQueueContainer " + frameType + " queue stream buffer index out of range! [" + StreamBufferIndex + "]");
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
            if (m_IsLoopbackEnabled || IsTestingEnabled)
            {
                QueueHistoryFrame queueHistoryItemOutbound = GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound);
                if (queueHistoryItemOutbound.queueItemOffsets.Count > 0)
                {
                    QueueHistoryFrame queueHistoryItemInbound = GetCurrentQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound);
                    ResetQueueHistoryFrame(queueHistoryItemInbound);
                    PooledBitStream pooledBitStream = PooledBitStream.Get();
                    FrameQueueItem frameQueueItem = queueHistoryItemOutbound.GetFirstQueueItem();
                    while (frameQueueItem.queueItemType != RpcQueueContainer.QueueItemType.None)
                    {
                        pooledBitStream.SetLength(frameQueueItem.streamSize);
                        pooledBitStream.Position = 0;
                        byte[] pooledBitStreamArray = pooledBitStream.GetBuffer();
                        Buffer.BlockCopy(frameQueueItem.messageData.Array ?? Array.Empty<byte>(), frameQueueItem.messageData.Offset, pooledBitStreamArray, 0, (int)frameQueueItem.streamSize);
                        pooledBitStream.Position = 1;

                        AddQueueItemToInboundFrame(frameQueueItem.queueItemType, UnityEngine.Time.realtimeSinceStartup, frameQueueItem.networkId, pooledBitStream);
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
        public void Initialize(uint maxFrameHistory)
        {
            ClearParameters();

            rpcQueueProcessing = new RpcQueueProcessing();

            m_MaxFrameHistory = maxFrameHistory + 1;

            if (m_IsLoopbackEnabled && m_MaxFrameHistory > 1)
            {
                string MSG = "Loopback is enabled but there are (" + m_MaxFrameHistory + ") frames allocated for history!\n";
                MSG += "Adjusting to use 1 frames for loopback mode.  (Initialize RPC Queue Manager with 0 history frame buffers for LoopBack mode)\n";
                UnityEngine.Debug.LogWarning(MSG);
                m_MaxFrameHistory = 1;
            }

            if (!QueueHistory.ContainsKey(QueueHistoryFrame.QueueFrameType.Inbound))
            {
                QueueHistory.Add(QueueHistoryFrame.QueueFrameType.Inbound, new Dictionary<int, QueueHistoryFrame>());
            }

            if (!QueueHistory.ContainsKey(QueueHistoryFrame.QueueFrameType.Outbound))
            {
                QueueHistory.Add(QueueHistoryFrame.QueueFrameType.Outbound, new Dictionary<int, QueueHistoryFrame>());
            }

            for (int i = 0; i < m_MaxFrameHistory; i++)
            {
                if (!QueueHistory[QueueHistoryFrame.QueueFrameType.Inbound].ContainsKey(i))
                {
                    QueueHistoryFrame queueHistoryFrame = new QueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound);
                    queueHistoryFrame.queueStream = PooledBitStream.Get();
                    queueHistoryFrame.queueStream.Position = 0;
                    queueHistoryFrame.queueWriter = PooledBitWriter.Get(queueHistoryFrame.queueStream);
                    queueHistoryFrame.queueReader = PooledBitReader.Get(queueHistoryFrame.queueStream);
                    queueHistoryFrame.queueItemOffsets = new List<uint>();
                    QueueHistory[QueueHistoryFrame.QueueFrameType.Inbound].Add(i, queueHistoryFrame);
                }

                if (!QueueHistory[QueueHistoryFrame.QueueFrameType.Outbound].ContainsKey(i))
                {
                    QueueHistoryFrame queueHistoryFrame = new QueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound);
                    queueHistoryFrame.queueStream = PooledBitStream.Get();
                    queueHistoryFrame.queueStream.Position = 0;
                    queueHistoryFrame.queueWriter = PooledBitWriter.Get(queueHistoryFrame.queueStream);
                    queueHistoryFrame.queueReader = PooledBitReader.Get(queueHistoryFrame.queueStream);
                    queueHistoryFrame.queueItemOffsets = new List<uint>();
                    QueueHistory[QueueHistoryFrame.QueueFrameType.Outbound].Add(i, queueHistoryFrame);
                }
            }
        }

        /// <summary>
        /// SetLoopbackState
        /// Primarily used for testing Rpc, this will loopback any ServerRpcs invoked by a Host
        /// This allows one to test an Rpc locally without having to have both a client and a host/server.
        /// </summary>
        /// <param name="enabled">true or false</param>
        public void SetLoopbackState(bool enabled)
        {
            m_IsLoopbackEnabled = enabled;
        }

        /// <summary>
        /// Constructor for RpcQueueContainer
        /// </summary>
        /// <param name="isLoopBackEnabled">will redirect traffic back to the receive queue buffer (generally used for debugging or testing)</param>
        /// <param name="isTestingEnabled">used for detecting if we are running tests</param>
        public RpcQueueContainer(bool isLoopBackEnabled = false, bool isTestingEnabled = false)
        {
            IsTestingEnabled = isTestingEnabled;
            m_IsLoopbackEnabled = isLoopBackEnabled;
        }

        /// <summary>
        /// Clears all declared parameters
        /// </summary>
        private void ClearParameters()
        {
            m_InboundStreamBufferIndex  = 0;
            m_OutBoundStreamBufferIndex = 0;
            m_OutboundFramesProcessed   = 0;
            m_InboundFramesProcessed    = 0;
        }

        /// <summary>
        /// OnExiting
        /// Called upon exiting to assure any last messages are delivered.
        /// </summary>
        public void OnExiting()
        {
            //We need to make sure all internal messages (i.e. object destroy) are sent
            rpcQueueProcessing?.InternalMessagesSendAndFlush();
        }

        /// <summary>
        /// There might be a case where we want to make this class non-static and in that case we would replace this with Dispose and add the IDisposable interface
        /// For now, this should be called upon shutdown
        /// </summary>
        public void Shutdown()
        {
            foreach (KeyValuePair<QueueHistoryFrame.QueueFrameType, Dictionary<int, QueueHistoryFrame>> queueHistorySection in QueueHistory)
            {
                foreach (KeyValuePair<int, QueueHistoryFrame> queueHistoryItem in queueHistorySection.Value)
                {
                    queueHistoryItem.Value.queueWriter?.Dispose();
                    queueHistoryItem.Value.queueReader?.Dispose();
                    queueHistoryItem.Value.queueStream?.Dispose();
                }
            }

            QueueHistory.Clear();

            ClearParameters();
        }
    }
}
