using System;
using System.Collections.Generic;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using MLAPI.Profiling;
using MLAPI.Transports;

namespace MLAPI.Messaging
{
    /// <summary>
    /// RpcQueueContainer
    /// Handles the management of an Rpc Queue
    /// </summary>
    public class RpcQueueContainer : GenericUpdateLoopSystem
    {
        private const int k_MinQueueHistory = 2; //We need a minimum of 2 queue history buffers in order to properly handle looping back Rpcs when a host

        public enum QueueItemType
        {
            ServerRpc,
            ClientRpc,
            CreateObject, //MLAPI Constant *** We need to determine if these belong here ***
            DestroyObject, //MLAPI Constant

            None //Indicates end of frame
        }

        public enum RpcQueueProcessingTypes
        {
            Send,
            Receive,
        }

        //Inbound and Outbound QueueHistoryFrames
        private readonly Dictionary<QueueHistoryFrame.QueueFrameType, Dictionary<int, Dictionary<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame>>> QueueHistory =
            new Dictionary<QueueHistoryFrame.QueueFrameType, Dictionary<int, Dictionary<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame>>>();


        private RpcQueueProcessor m_RpcQueueProcessor;

        private uint m_OutboundFramesProcessed;
        private uint m_InboundFramesProcessed;
        private uint m_MaxFrameHistory;
        private int m_InboundStreamBufferIndex;
        private int m_OutBoundStreamBufferIndex;
        private bool m_IsTestingEnabled;
        private bool m_ProcessUpdateStagesExternally;
        private bool m_IsNotUsingBatching;

        public bool IsUsingBatching()
        {
            return !m_IsNotUsingBatching;
        }

        public void EnableBatchedRpcs(bool isbatchingEnabled)
        {
            m_IsNotUsingBatching = !isbatchingEnabled;
        }

        /// <summary>
        /// PreUpdateStage
        /// Predefined internal network loop update system action
        /// </summary>
        void PreUpdateStage()
        {
            ProcessAndFlushRPCQueue(RpcQueueContainer.RpcQueueProcessingTypes.Receive, NetworkUpdateManager.NetworkUpdateStage.PreUpdate);
        }

        /// <summary>
        /// FixedUpdateStage
        /// Predefined internal network loop update system action
        /// </summary>
        void FixedUpdateStage()
        {
            ProcessAndFlushRPCQueue(RpcQueueContainer.RpcQueueProcessingTypes.Receive, NetworkUpdateManager.NetworkUpdateStage.FixedUpdate);
        }

        /// <summary>
        /// UpdateStage
        /// Predefined internal network loop update system action
        /// </summary>
        void UpdateStage()
        {
            ProcessAndFlushRPCQueue(RpcQueueContainer.RpcQueueProcessingTypes.Receive, NetworkUpdateManager.NetworkUpdateStage.Update);
        }

        /// <summary>
        /// LateUpdateStage
        /// Predefined internal network loop update system action
        /// </summary>
        void LateUpdateStage()
        {
            ProcessAndFlushRPCQueue(RpcQueueContainer.RpcQueueProcessingTypes.Receive, NetworkUpdateManager.NetworkUpdateStage.LateUpdate);
            ProcessAndFlushRPCQueue(RpcQueueContainer.RpcQueueProcessingTypes.Send, NetworkUpdateManager.NetworkUpdateStage.LateUpdate);
        }

        protected override Action InternalRegisterNetworkUpdateStage(NetworkUpdateManager.NetworkUpdateStage stage)
        {
            Action updateStageAction = null;
            if (!m_ProcessUpdateStagesExternally)
            {
                switch (stage)
                {
                    case NetworkUpdateManager.NetworkUpdateStage.PreUpdate:
                    {
                        updateStageAction = PreUpdateStage;
                        break;
                    }
                    case NetworkUpdateManager.NetworkUpdateStage.FixedUpdate:
                    {
                        updateStageAction = FixedUpdateStage;
                        break;
                    }
                    case NetworkUpdateManager.NetworkUpdateStage.Update:
                    {
                        updateStageAction = UpdateStage;
                        break;
                    }
                    case NetworkUpdateManager.NetworkUpdateStage.LateUpdate:
                    {
                        updateStageAction = LateUpdateStage;
                        break;
                    }
                }
            }

            return updateStageAction;
        }


        /// <summary>
        /// GetStreamBufferFrameCount
        /// Returns how many frames have been processed (Inbound/Outbound)
        /// </summary>
        /// <param name="queueType"></param>
        /// <returns>number of frames procssed</returns>
        public uint GetStreamBufferFrameCount(QueueHistoryFrame.QueueFrameType queueType)
        {
            return queueType == QueueHistoryFrame.QueueFrameType.Inbound ? m_InboundFramesProcessed : m_OutboundFramesProcessed;
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
        public void AddToInternalMLAPISendQueue(RpcFrameQueueItem queueItem)
        {
            m_RpcQueueProcessor.QueueInternalMLAPICommand(queueItem);
        }

        /// <summary>
        /// ProcessAndFlushRPCQueue
        /// Will process the RPC queue and then move to the next available frame
        /// </summary>
        /// <param name="queueType"></param>
        public void ProcessAndFlushRPCQueue(RpcQueueProcessingTypes queueType, NetworkUpdateManager.NetworkUpdateStage currentUpdateStage)
        {
            if (m_RpcQueueProcessor == null)
            {
                return;
            }

            switch (queueType)
            {
                case RpcQueueProcessingTypes.Receive:
                {
                    m_RpcQueueProcessor.ProcessReceiveQueue(currentUpdateStage);
                    break;
                }
                case RpcQueueProcessingTypes.Send:
                {
                    m_RpcQueueProcessor.ProcessSendQueue();
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
        public QueueHistoryFrame GetCurrentFrame(QueueHistoryFrame.QueueFrameType qType, NetworkUpdateManager.NetworkUpdateStage currentUpdateStage)
        {
            if (QueueHistory.ContainsKey(qType))
            {
                int StreamBufferIndex = GetStreamBufferIndex(qType);

                if (QueueHistory[qType].ContainsKey(StreamBufferIndex))
                {
                    if (QueueHistory[qType][StreamBufferIndex].ContainsKey(currentUpdateStage))
                    {
                        return QueueHistory[qType][StreamBufferIndex][currentUpdateStage];
                    }
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


            foreach (KeyValuePair<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame> queueHistoryByUpdates in QueueHistory[queueType][StreamBufferIndex])
            {
                QueueHistoryFrame queueHistoryItem = queueHistoryByUpdates.Value;
                //This only gets reset when we advanced to next frame (do not reset this in the ResetQueueHistoryFrame)
                queueHistoryItem.hasLoopbackData = false;
                if (queueHistoryItem.queueItemOffsets.Count > 0)
                {
                    if (queueType == QueueHistoryFrame.QueueFrameType.Inbound)
                    {
                        ProfilerStatManager.rpcInQueueSize.Record((int)queueHistoryItem.totalSize);
                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsInQueueSize, (int)queueHistoryItem.totalSize);
                    }
                    else
                    {
                        ProfilerStatManager.rpcOutQueueSize.Record((int)queueHistoryItem.totalSize);
                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsOutQueueSize, (int)queueHistoryItem.totalSize);
                    }
                }

                ResetQueueHistoryFrame(queueHistoryItem);
                IncrementAndSetQueueHistoryFrame(queueHistoryItem);
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
            //If we are dirt and have loopback data then don't clear this frame
            if (queueFrame.isDirty && !queueFrame.hasLoopbackData)
            {
                queueFrame.totalSize = 0;
                queueFrame.queueItemOffsets.Clear();
                queueFrame.queueStream.Position = 0;
                queueFrame.MarkCurrentStreamPosition();
                queueFrame.isDirty = false;
            }
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
            long originalPosition = message.Position;
            PooledBitReader BR = PooledBitReader.Get(message);

            var longValue = BR.ReadUInt64Packed(); // NetworkObjectId (temporary, we reset position just below)

            var shortValue = BR.ReadUInt16Packed(); // NetworkBehaviourId (temporary, we reset position just below)

            ushort updateStageValue = BR.ReadUInt16Packed();
            BR.Dispose();
            BR = null;

            NetworkUpdateManager.NetworkUpdateStage updateStage = NetworkUpdateManager.NetworkUpdateStage.Update;
            if (System.Enum.IsDefined(typeof(NetworkUpdateManager.NetworkUpdateStage), (int)updateStageValue))
            {
                updateStage = (NetworkUpdateManager.NetworkUpdateStage)updateStageValue;
            }

            message.Position = originalPosition;
            QueueHistoryFrame queueHistoryItem = GetQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound, updateStage);
            queueHistoryItem.isDirty = true;

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
            queueHistoryItem.totalSize += (uint)(queueHistoryItem.queueStream.Position - StartPosition);
        }

        /// <summary>
        /// SetLoopBackFrameItem
        /// ***Temporary fix for host mode loopback RPC writer work-around
        /// Sets the next frame inbond buffer as the loopback queue history frame in the current frame's outbound buffer
        /// </summary>
        /// <param name="updateStage"></param>
        public void SetLoopBackFrameItem(NetworkUpdateManager.NetworkUpdateStage updateStage)
        {
            //Get the next frame's inbound queue history frame
            QueueHistoryFrame loopbackHistoryframe =  GetQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound,updateStage,true);

            //Get the current frame's outbound queue history frame
            QueueHistoryFrame queueHistoryItem = GetQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound,NetworkUpdateManager.NetworkUpdateStage.LateUpdate,false);

            if (queueHistoryItem != null)
            {
                queueHistoryItem.loopbackHistoryFrame = loopbackHistoryframe;
            }
            else
            {
                UnityEngine.Debug.LogError("Could not find the outbound QueueHistoryFrame!");
            }
        }

        /// <summary>
        /// GetLoopBackWriter
        /// Gets the loop back writer for the history frame (if one exists)
        /// ***Temporary fix for host mode loopback RPC writer work-around
        /// </summary>
        /// <param name="queueFrameType"></param>
        /// <param name="updateStage"></param>
        /// <returns></returns>
        public QueueHistoryFrame GetLoopBackHistoryFrame( QueueHistoryFrame.QueueFrameType queueFrameType,NetworkUpdateManager.NetworkUpdateStage updateStage)
        {
            return GetQueueHistoryFrame(queueFrameType,updateStage,false);
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
        public PooledBitWriter BeginAddQueueItemToFrame(QueueItemType qItemType, float timeStamp, Channel channel, ushort sendflags, ulong sourceNetworkId, ulong[] targetNetworkIds,
            QueueHistoryFrame.QueueFrameType queueFrameType, NetworkUpdateManager.NetworkUpdateStage updateStage)
        {
            bool getNextFrame = false;
            if (NetworkingManager.Singleton.IsHost && queueFrameType == QueueHistoryFrame.QueueFrameType.Inbound)
            {
                getNextFrame = true;
            }

            QueueHistoryFrame queueHistoryItem = GetQueueHistoryFrame(queueFrameType, updateStage, getNextFrame);
            queueHistoryItem.isDirty = true;

            //Write the packed version of the queueItem to our current queue history buffer
            queueHistoryItem.queueWriter.WriteUInt16((ushort)qItemType);
            queueHistoryItem.queueWriter.WriteUInt16(sendflags);
            queueHistoryItem.queueWriter.WriteSingle(timeStamp);
            queueHistoryItem.queueWriter.WriteUInt64(sourceNetworkId);

            if (queueFrameType != QueueHistoryFrame.QueueFrameType.Inbound)
            {
                queueHistoryItem.queueWriter.WriteByte((byte)channel);

                if (targetNetworkIds != null && targetNetworkIds.Length != 0)
                {
                    //In the event the host is one of the networkIds, for outbound we want to ignore it (at this spot only!!)
                    //Get a count of clients we are going to send to (and write into the buffer)
                    var numberOfClients = 0;
                    for (int i = 0; i < targetNetworkIds.Length; i++)
                    {
                        if (NetworkingManager.Singleton.IsHost && targetNetworkIds[i] == NetworkingManager.Singleton.ServerClientId)
                        {
                            continue;
                        }

                        numberOfClients++;
                    }

                    //Write our total number of clients
                    queueHistoryItem.queueWriter.WriteInt32(numberOfClients);

                    //Now write the cliend ids
                    for (int i = 0; i < targetNetworkIds.Length; i++)
                    {
                        if (NetworkingManager.Singleton.IsHost && targetNetworkIds[i] == NetworkingManager.Singleton.ServerClientId)
                        {
                            continue;
                        }

                        queueHistoryItem.queueWriter.WriteUInt64(targetNetworkIds[i]);
                    }
                }
                else
                {
                    queueHistoryItem.queueWriter.WriteInt32(0);
                }
            }

            //Mark where we started in the stream to later determine the actual RPC message size (position before writing RPC message vs position after write has completed)
            queueHistoryItem.MarkCurrentStreamPosition();

            //Write a filler dummy size of 0 to hold this position in order to write to it once the RPC is done writing.
            queueHistoryItem.queueWriter.WriteInt64(0);

            if (NetworkingManager.Singleton.IsHost && queueFrameType == QueueHistoryFrame.QueueFrameType.Inbound)
            {
                if (!IsUsingBatching())
                {
                    queueHistoryItem.queueWriter.WriteInt64(1);
                }
                else
                {
                    queueHistoryItem.queueWriter.WriteInt64(0);
                }
                queueHistoryItem.hasLoopbackData = true;    //The only case for this is when it is the Host
            }

            //Return the writer to the invoking method.
            return queueHistoryItem.queueWriter;
        }

        /// <summary>
        /// EndAddQueueItemToOutboundFrame
        /// Signifies the end of this outbound RPC.
        /// We store final MSG size and track the total current frame queue size
        /// </summary>
        /// <param name="writer">writer that was used</param>
        public void EndAddQueueItemToFrame(BitWriter writer, QueueHistoryFrame.QueueFrameType queueFrameType, NetworkUpdateManager.NetworkUpdateStage updateStage)
        {
            bool getNextFrame = false;
            if (NetworkingManager.Singleton.IsHost && queueFrameType == QueueHistoryFrame.QueueFrameType.Inbound)
            {
                getNextFrame = true;
            }

            QueueHistoryFrame queueHistoryItem = GetQueueHistoryFrame(queueFrameType, updateStage, getNextFrame);
            QueueHistoryFrame loopBackHistoryFrame = queueHistoryItem.loopbackHistoryFrame;


            PooledBitWriter pbWriter = (PooledBitWriter)writer;

            //Sanity check
            if (pbWriter != queueHistoryItem.queueWriter && !getNextFrame)
            {
                UnityEngine.Debug.LogError("RpcQueueContainer " + queueFrameType.ToString() + " passed writer is not the same as the current PooledBitWriter for the " +  queueFrameType.ToString() + "]!");
            }

            //The total size of the frame is the last known position of the stream
            queueHistoryItem.totalSize = (uint)queueHistoryItem.queueStream.Position;

            long CurrentPosition = queueHistoryItem.queueStream.Position;
            ulong BitPosition = queueHistoryItem.queueStream.BitPosition;

            //////////////////////////////////////////////////////////////
            //>>>> REPOSITIONING STREAM TO RPC MESSAGE SIZE LOCATION <<<<
            //////////////////////////////////////////////////////////////
            queueHistoryItem.queueStream.Position = queueHistoryItem.GetCurrentMarkedPosition();

            long MSGOffset = 8;
            if (getNextFrame && IsUsingBatching())
            {
                MSGOffset += 8;
            }
            //subtracting 8 byte to account for the value of the size of the RPC
            long MSGSize = (long)(queueHistoryItem.totalSize - (queueHistoryItem.GetCurrentMarkedPosition() + MSGOffset));

            if (MSGSize > 0)
            {
                //Write the actual size of the RPC message
                queueHistoryItem.queueWriter.WriteInt64(MSGSize);
            }
            else
            {
                UnityEngine.Debug.LogWarning("MSGSize of < zero detected!!  Setting message size to zero!");
                queueHistoryItem.queueWriter.WriteInt64(0);
            }

            if (loopBackHistoryFrame != null)
            {
                if (MSGSize > 0)
                {

                    //Point to where the size of the message is stored
                    loopBackHistoryFrame.queueStream.Position = loopBackHistoryFrame.GetCurrentMarkedPosition();

                    //Write the actual size of the RPC message
                    loopBackHistoryFrame.queueWriter.WriteInt64(MSGSize);

                    if (!IsUsingBatching())
                    {
                        //Write the offset for the header info copied
                        loopBackHistoryFrame.queueWriter.WriteInt64(1);
                    }
                    else
                    {
                        //Write the offset for the header info copied
                        loopBackHistoryFrame.queueWriter.WriteInt64(0);
                    }

                    //Write RPC data
                    loopBackHistoryFrame.queueWriter.WriteBytes(queueHistoryItem.queueStream.GetBuffer(), MSGSize,(int)queueHistoryItem.queueStream.Position);

                    //Set the total size for this stream
                    loopBackHistoryFrame.totalSize = (uint)loopBackHistoryFrame.queueStream.Position;

                    //Add the total size to the offsets for parsing over various entries
                    loopBackHistoryFrame.queueItemOffsets.Add((uint)loopBackHistoryFrame.queueStream.Position);

                }
                else
                {
                    UnityEngine.Debug.LogWarning("[LoopBack] MSGSize of < zero detected!!  Setting message size to zero!");
                    //Write the actual size of the RPC message
                    loopBackHistoryFrame.queueWriter.WriteInt64(0);
                }
                queueHistoryItem.loopbackHistoryFrame = null;
            }


            //////////////////////////////////////////////////////////////
            //<<<< REPOSITIONING STREAM BACK TO THE CURRENT TAIL >>>>
            //////////////////////////////////////////////////////////////
            queueHistoryItem.queueStream.Position = CurrentPosition;
            queueHistoryItem.queueStream.BitPosition = BitPosition;

            //Add the packed size to the offsets for parsing over various entries
            queueHistoryItem.queueItemOffsets.Add((uint)queueHistoryItem.queueStream.Position);
        }

        /// <summary>
        /// GetQueueHistoryFrame
        /// Gets the current queue history frame (inbound or outbound)
        /// </summary>
        /// <param name="frameType">inbound or outbound</param>
        /// <returns>QueueHistoryFrame or null</returns>
        public QueueHistoryFrame GetQueueHistoryFrame(QueueHistoryFrame.QueueFrameType frameType, NetworkUpdateManager.NetworkUpdateStage updateStage, bool getNextFrame = false)
        {
            int StreamBufferIndex = GetStreamBufferIndex(frameType);

            //We want to write into the future/next frame
            if (getNextFrame)
            {
                StreamBufferIndex++;
                //If we have hit our maximum history, roll back over to the first one
                if (StreamBufferIndex >= m_MaxFrameHistory)
                {
                    StreamBufferIndex = 0;
                }
            }

            if (!QueueHistory.ContainsKey(frameType))
            {
                UnityEngine.Debug.LogError("You must initialize the RPCQueueManager before using MLAPI!");
                return null;
            }

            if (!QueueHistory[frameType].ContainsKey(StreamBufferIndex))
            {
                UnityEngine.Debug.LogError("RPCQueueManager " + frameType + " queue stream buffer index out of range! [" + StreamBufferIndex + "]");
                return null;
            }

            if (!QueueHistory[frameType][StreamBufferIndex].ContainsKey(updateStage))
            {
                UnityEngine.Debug.LogError("RPCQueueManager " + updateStage.ToString() + " update type does not exist!");
                return null;
            }

            return QueueHistory[frameType][StreamBufferIndex][updateStage];
        }


        /// <summary>
        /// LoopbackSendFrame
        /// Will copy the contents of the current outbound QueueHistoryFrame to the current inbound QueueHistoryFrame
        /// [NSS]: Leaving this here in the event a portion of this code is useful for doing Batch testing
        /// </summary>
        public void LoopbackSendFrame()
        {
            //If we do not have loop back or testing mode enabled then ignore the call
            if (m_IsTestingEnabled)
            {
                QueueHistoryFrame queueHistoryItemOutbound = GetQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateManager.NetworkUpdateStage.LateUpdate);
                if (queueHistoryItemOutbound.queueItemOffsets.Count > 0)
                {
                    //Reset inbound queues based on update stage
                    foreach (NetworkUpdateManager.NetworkUpdateStage stage in System.Enum.GetValues(typeof(NetworkUpdateManager.NetworkUpdateStage)))
                    {
                        QueueHistoryFrame queueHistoryItemInbound = GetQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound, stage);
                        ResetQueueHistoryFrame(queueHistoryItemInbound);
                    }

                    PooledBitStream pooledBitStream = PooledBitStream.Get();
                    RpcFrameQueueItem rpcFrameQueueItem = queueHistoryItemOutbound.GetFirstQueueItem();

                    while (rpcFrameQueueItem.queueItemType != RpcQueueContainer.QueueItemType.None)
                    {
                        pooledBitStream.SetLength(rpcFrameQueueItem.streamSize);
                        pooledBitStream.Position = 0;
                        byte[] pooledBitStreamArray = pooledBitStream.GetBuffer();
                        Buffer.BlockCopy(rpcFrameQueueItem.messageData.Array ?? Array.Empty<byte>(), rpcFrameQueueItem.messageData.Offset, pooledBitStreamArray, 0, (int)rpcFrameQueueItem.streamSize);

                        if (!IsUsingBatching())
                        {
                            pooledBitStream.Position = 1;
                        }

                        AddQueueItemToInboundFrame(rpcFrameQueueItem.queueItemType, UnityEngine.Time.realtimeSinceStartup, rpcFrameQueueItem.networkId, pooledBitStream);
                        rpcFrameQueueItem = queueHistoryItemOutbound.GetNextQueueItem();
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

            m_RpcQueueProcessor = new RpcQueueProcessor();

            m_MaxFrameHistory = maxFrameHistory + k_MinQueueHistory;

            if (!QueueHistory.ContainsKey(QueueHistoryFrame.QueueFrameType.Inbound))
            {
                QueueHistory.Add(QueueHistoryFrame.QueueFrameType.Inbound, new Dictionary<int, Dictionary<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame>>());
            }

            if (!QueueHistory.ContainsKey(QueueHistoryFrame.QueueFrameType.Outbound))
            {
                QueueHistory.Add(QueueHistoryFrame.QueueFrameType.Outbound, new Dictionary<int, Dictionary<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame>>());
            }

            for (int i = 0; i < m_MaxFrameHistory; i++)
            {
                if (!QueueHistory[QueueHistoryFrame.QueueFrameType.Outbound].ContainsKey(i))
                {
                    QueueHistory[QueueHistoryFrame.QueueFrameType.Outbound].Add(i, new Dictionary<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame>());
                    QueueHistoryFrame queueHistoryFrame = new QueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateManager.NetworkUpdateStage.LateUpdate);
                    queueHistoryFrame.queueStream = PooledBitStream.Get();
                    queueHistoryFrame.queueStream.Position = 0;
                    queueHistoryFrame.queueWriter = PooledBitWriter.Get(queueHistoryFrame.queueStream);
                    queueHistoryFrame.queueReader = PooledBitReader.Get(queueHistoryFrame.queueStream);
                    queueHistoryFrame.queueItemOffsets = new List<uint>();

                    //For now all outbound, we will always have a single update in which they are processed (LATEUPDATE)
                    QueueHistory[QueueHistoryFrame.QueueFrameType.Outbound][i].Add(NetworkUpdateManager.NetworkUpdateStage.LateUpdate, queueHistoryFrame);
                }

                if (!QueueHistory[QueueHistoryFrame.QueueFrameType.Inbound].ContainsKey(i))
                {
                    QueueHistory[QueueHistoryFrame.QueueFrameType.Inbound].Add(i, new Dictionary<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame>());

                    //For inbound, we create a queue history frame per update stage
                    foreach (NetworkUpdateManager.NetworkUpdateStage stage in Enum.GetValues(typeof(NetworkUpdateManager.NetworkUpdateStage)))
                    {
                        QueueHistoryFrame queueHistoryFrame = new QueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound, stage);
                        queueHistoryFrame.queueStream = PooledBitStream.Get();
                        queueHistoryFrame.queueStream.Position = 0;
                        queueHistoryFrame.queueWriter = PooledBitWriter.Get(queueHistoryFrame.queueStream);
                        queueHistoryFrame.queueReader = PooledBitReader.Get(queueHistoryFrame.queueStream);
                        queueHistoryFrame.queueItemOffsets = new List<uint>();
                        QueueHistory[QueueHistoryFrame.QueueFrameType.Inbound][i].Add(stage, queueHistoryFrame);
                    }
                }
            }

            //As long as this instance is using the pre-defined update stages
            if (!m_ProcessUpdateStagesExternally)
            {
                //Register with the network update loop system
                RegisterUpdateLoopSystem();
            }
        }

        public void SetTestingState(bool enabled)
        {
            m_IsTestingEnabled = enabled;
        }

        public bool IsTesting()
        {
            return m_IsTestingEnabled;
        }

        /// <summary>
        /// Clears the stream indices and frames process properties
        /// </summary>
        private void ClearParameters()
        {
            m_InboundStreamBufferIndex = 0;
            m_OutBoundStreamBufferIndex = 0;
            m_OutboundFramesProcessed = 0;
            m_InboundFramesProcessed = 0;
        }

        /// <summary>
        /// Shutdown
        /// Flushes the internal messages
        /// Removes itself from the network update loop
        /// Disposes readers, writers, clears the queue history, and resets any parameters
        /// </summary>
        public void Shutdown()
        {
            //We need to make sure all internal messages (i.e. object destroy) are sent
            m_RpcQueueProcessor.InternalMessagesSendAndFlush();

            //As long as this instance is using the pre-defined update stages
            if (!m_ProcessUpdateStagesExternally)
            {
                //Remove ourself from the network loop update system
                OnNetworkLoopSystemRemove();
            }

            //Dispose of any readers and writers
            foreach (KeyValuePair<QueueHistoryFrame.QueueFrameType, Dictionary<int, Dictionary<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame>>> queueHistorySection in QueueHistory)
            {
                foreach (KeyValuePair<int, Dictionary<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame>> queueHistoryItemByStage in queueHistorySection.Value)
                {
                    foreach (KeyValuePair<NetworkUpdateManager.NetworkUpdateStage, QueueHistoryFrame> queueHistoryItem in queueHistoryItemByStage.Value)
                    {
                        queueHistoryItem.Value.queueWriter?.Dispose();
                        queueHistoryItem.Value.queueReader?.Dispose();
                        queueHistoryItem.Value.queueStream?.Dispose();
                    }
                }
            }

            //Clear history and parameters
            QueueHistory.Clear();

            ClearParameters();
        }

        /// <summary>
        /// RpcQueueContainer - Constructor
        /// </summary>
        /// <param name="processInternally">determines if it handles processing internally or if it will be done externally</param>
        /// <param name="isLoopBackEnabled">turns loopback on or off (primarily debugging purposes)</param>
        public RpcQueueContainer(bool processExternally)
        {
            m_ProcessUpdateStagesExternally = processExternally;
        }
    }
}
