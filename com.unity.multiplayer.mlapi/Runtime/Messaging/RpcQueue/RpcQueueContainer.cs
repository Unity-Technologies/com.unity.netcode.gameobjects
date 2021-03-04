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
    internal class RpcQueueContainer : INetworkUpdateSystem
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

        // Inbound and Outbound QueueHistoryFrames
        private readonly Dictionary<RpcQueueHistoryFrame.QueueFrameType, Dictionary<int, Dictionary<NetworkUpdateStage, RpcQueueHistoryFrame>>> QueueHistory =
            new Dictionary<RpcQueueHistoryFrame.QueueFrameType, Dictionary<int, Dictionary<NetworkUpdateStage, RpcQueueHistoryFrame>>>();

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

        // INetworkUpdateSystem
        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            ProcessAndFlushRpcQueue(RpcQueueProcessingTypes.Receive, updateStage);

            if (updateStage == NetworkUpdateStage.PostLateUpdate)
            {
                ProcessAndFlushRpcQueue(RpcQueueProcessingTypes.Send, updateStage);
            }
        }

        /// <summary>
        /// GetStreamBufferFrameCount
        /// Returns how many frames have been processed (Inbound/Outbound)
        /// </summary>
        /// <param name="queueType"></param>
        /// <returns>number of frames procssed</returns>
        public uint GetStreamBufferFrameCount(RpcQueueHistoryFrame.QueueFrameType queueType)
        {
            return queueType == RpcQueueHistoryFrame.QueueFrameType.Inbound ? m_InboundFramesProcessed : m_OutboundFramesProcessed;
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
        public void ProcessAndFlushRpcQueue(RpcQueueProcessingTypes queueType, NetworkUpdateStage currentUpdateStage)
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
        public RpcQueueHistoryFrame GetCurrentFrame(RpcQueueHistoryFrame.QueueFrameType qType, NetworkUpdateStage currentUpdateStage)
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
        private int GetStreamBufferIndex(RpcQueueHistoryFrame.QueueFrameType queueType)
        {
            return queueType == RpcQueueHistoryFrame.QueueFrameType.Inbound ? m_InboundStreamBufferIndex : m_OutBoundStreamBufferIndex;
        }

        /// <summary>
        /// AdvanceFrameHistory
        /// Progresses the current frame to the next QueueHistoryFrame for the QueueHistoryFrame.QueueFrameType.
        /// All other frames other than the current frame is considered the live rollback history
        /// </summary>
        /// <param name="queueType"></param>
        public void AdvanceFrameHistory(RpcQueueHistoryFrame.QueueFrameType queueType)
        {
            int StreamBufferIndex = GetStreamBufferIndex(queueType);

            if (!QueueHistory.ContainsKey(queueType))
            {
                UnityEngine.Debug.LogError($"You must initialize the {nameof(RpcQueueContainer)} before using MLAPI!");
                return;
            }

            if (!QueueHistory[queueType].ContainsKey(StreamBufferIndex))
            {
                UnityEngine.Debug.LogError($"{nameof(RpcQueueContainer)} {queueType} queue stream buffer index out of range! [{StreamBufferIndex}]");
                return;
            }


            foreach (KeyValuePair<NetworkUpdateStage, RpcQueueHistoryFrame> queueHistoryByUpdates in QueueHistory[queueType][StreamBufferIndex])
            {
                var rpcQueueHistoryItem = queueHistoryByUpdates.Value;

                //This only gets reset when we advanced to next frame (do not reset this in the ResetQueueHistoryFrame)
                rpcQueueHistoryItem.HasLoopbackData = false;

                if (rpcQueueHistoryItem.QueueItemOffsets.Count > 0)
                {
                    if (queueType == RpcQueueHistoryFrame.QueueFrameType.Inbound)
                    {
                        ProfilerStatManager.RpcInQueueSize.Record((int)rpcQueueHistoryItem.TotalSize);
                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsInQueueSize, (int)rpcQueueHistoryItem.TotalSize);
                    }
                    else
                    {
                        ProfilerStatManager.RpcOutQueueSize.Record((int)rpcQueueHistoryItem.TotalSize);
                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsOutQueueSize, (int)rpcQueueHistoryItem.TotalSize);
                    }
                }

                ResetQueueHistoryFrame(rpcQueueHistoryItem);
                IncrementAndSetQueueHistoryFrame(rpcQueueHistoryItem);
            }

            //Roll to the next stream buffer
            StreamBufferIndex++;

            //If we have hit our maximum history, roll back over to the first one
            if (StreamBufferIndex >= m_MaxFrameHistory)
            {
                StreamBufferIndex = 0;
            }

            if (queueType == RpcQueueHistoryFrame.QueueFrameType.Inbound)
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
        /// <param name="rpcQueueFrame">QueueHistoryFrame to be reset</param>
        private void IncrementAndSetQueueHistoryFrame(RpcQueueHistoryFrame rpcQueueFrame)
        {
            if (rpcQueueFrame.GetQueueFrameType() == RpcQueueHistoryFrame.QueueFrameType.Inbound)
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
        /// <param name="rpcQueueFrame">QueueHistoryFrame to be reset</param>
        private static void ResetQueueHistoryFrame(RpcQueueHistoryFrame rpcQueueFrame)
        {
            //If we are dirt and have loopback data then don't clear this frame
            if (rpcQueueFrame.IsDirty && !rpcQueueFrame.HasLoopbackData)
            {
                rpcQueueFrame.TotalSize = 0;
                rpcQueueFrame.QueueItemOffsets.Clear();
                rpcQueueFrame.QueueBuffer.Position = 0;
                rpcQueueFrame.MarkCurrentStreamPosition();
                rpcQueueFrame.IsDirty = false;
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
        internal void AddQueueItemToInboundFrame(QueueItemType qItemType, float timeStamp, ulong sourceNetworkId, NetworkBuffer message)
        {
            long originalPosition = message.Position;

            NetworkUpdateStage updateStage;

            using (var reader = PooledNetworkReader.Get(message))
            {
                var longValue = reader.ReadUInt64Packed(); // NetworkObjectId (temporary, we reset position just below)
                var shortValue = reader.ReadUInt16Packed(); // NetworkBehaviourId (temporary, we reset position just below)
                updateStage = (NetworkUpdateStage)reader.ReadByteDirect();
            }

            message.Position = originalPosition;
            var rpcQueueHistoryItem = GetQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Inbound, updateStage);
            rpcQueueHistoryItem.IsDirty = true;

            long StartPosition = rpcQueueHistoryItem.QueueBuffer.Position;

            //Write the packed version of the queueItem to our current queue history buffer
            rpcQueueHistoryItem.QueueWriter.WriteUInt16((ushort)qItemType);
            rpcQueueHistoryItem.QueueWriter.WriteSingle(timeStamp);
            rpcQueueHistoryItem.QueueWriter.WriteUInt64(sourceNetworkId);

            //Inbound we copy the entire packet and store the position offset
            long streamSize = message.Length;
            rpcQueueHistoryItem.QueueWriter.WriteInt64(streamSize);
            rpcQueueHistoryItem.QueueWriter.WriteInt64(message.Position);
            rpcQueueHistoryItem.QueueWriter.WriteBytes(message.GetBuffer(), streamSize);

            //Add the packed size to the offsets for parsing over various entries
            rpcQueueHistoryItem.QueueItemOffsets.Add((uint)rpcQueueHistoryItem.QueueBuffer.Position);

            //Calculate the packed size based on stream progression
            rpcQueueHistoryItem.TotalSize += (uint)(rpcQueueHistoryItem.QueueBuffer.Position - StartPosition);
        }

        /// <summary>
        /// SetLoopBackFrameItem
        /// ***Temporary fix for host mode loopback RPC writer work-around
        /// Sets the next frame inbond buffer as the loopback queue history frame in the current frame's outbound buffer
        /// </summary>
        /// <param name="updateStage"></param>
        public void SetLoopBackFrameItem(NetworkUpdateStage updateStage)
        {
            //Get the next frame's inbound queue history frame
            var loopbackHistoryframe = GetQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Inbound, updateStage, true);

            //Get the current frame's outbound queue history frame
            var rpcQueueHistoryItem = GetQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate, false);

            if (rpcQueueHistoryItem != null)
            {
                rpcQueueHistoryItem.LoopbackHistoryFrame = loopbackHistoryframe;
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
        public RpcQueueHistoryFrame GetLoopBackHistoryFrame(RpcQueueHistoryFrame.QueueFrameType queueFrameType, NetworkUpdateStage updateStage)
        {
            return GetQueueHistoryFrame(queueFrameType, updateStage, false);
        }

        /// <summary>
        /// BeginAddQueueItemToOutboundFrame
        /// Adds a queue item to the outbound queue frame
        /// </summary>
        /// <param name="qItemType">type of rpc (client or server)</param>
        /// <param name="timeStamp">when it was scheduled to be sent</param>
        /// <param name="networkChannel">the channel to send it on</param>
        /// <param name="sourceNetworkId">who is sending the rpc</param>
        /// <param name="targetNetworkIds">who the rpc is being sent to</param>
        /// <returns></returns>
        public PooledNetworkWriter BeginAddQueueItemToFrame(QueueItemType qItemType, float timeStamp, NetworkChannel networkChannel, ulong sourceNetworkId, ulong[] targetNetworkIds,
            RpcQueueHistoryFrame.QueueFrameType queueFrameType, NetworkUpdateStage updateStage)
        {
            bool getNextFrame = NetworkManager.Singleton.IsHost && queueFrameType == RpcQueueHistoryFrame.QueueFrameType.Inbound;

            var rpcQueueHistoryItem = GetQueueHistoryFrame(queueFrameType, updateStage, getNextFrame);
            rpcQueueHistoryItem.IsDirty = true;

            //Write the packed version of the queueItem to our current queue history buffer
            rpcQueueHistoryItem.QueueWriter.WriteUInt16((ushort)qItemType);
            rpcQueueHistoryItem.QueueWriter.WriteSingle(timeStamp);
            rpcQueueHistoryItem.QueueWriter.WriteUInt64(sourceNetworkId);

            if (queueFrameType != RpcQueueHistoryFrame.QueueFrameType.Inbound)
            {
                rpcQueueHistoryItem.QueueWriter.WriteByte((byte)networkChannel);

                if (targetNetworkIds != null && targetNetworkIds.Length != 0)
                {
                    //In the event the host is one of the networkIds, for outbound we want to ignore it (at this spot only!!)
                    //Get a count of clients we are going to send to (and write into the buffer)
                    var numberOfClients = 0;
                    for (int i = 0; i < targetNetworkIds.Length; i++)
                    {
                        if (NetworkManager.Singleton.IsHost && targetNetworkIds[i] == NetworkManager.Singleton.ServerClientId)
                        {
                            continue;
                        }

                        numberOfClients++;
                    }

                    //Write our total number of clients
                    rpcQueueHistoryItem.QueueWriter.WriteInt32(numberOfClients);

                    //Now write the cliend ids
                    for (int i = 0; i < targetNetworkIds.Length; i++)
                    {
                        if (NetworkManager.Singleton.IsHost && targetNetworkIds[i] == NetworkManager.Singleton.ServerClientId)
                        {
                            continue;
                        }

                        rpcQueueHistoryItem.QueueWriter.WriteUInt64(targetNetworkIds[i]);
                    }
                }
                else
                {
                    rpcQueueHistoryItem.QueueWriter.WriteInt32(0);
                }
            }

            //Mark where we started in the stream to later determine the actual RPC message size (position before writing RPC message vs position after write has completed)
            rpcQueueHistoryItem.MarkCurrentStreamPosition();

            //Write a filler dummy size of 0 to hold this position in order to write to it once the RPC is done writing.
            rpcQueueHistoryItem.QueueWriter.WriteInt64(0);

            if (NetworkManager.Singleton.IsHost && queueFrameType == RpcQueueHistoryFrame.QueueFrameType.Inbound)
            {
                if (!IsUsingBatching())
                {
                    rpcQueueHistoryItem.QueueWriter.WriteInt64(1);
                }
                else
                {
                    rpcQueueHistoryItem.QueueWriter.WriteInt64(0);
                }

                rpcQueueHistoryItem.HasLoopbackData = true; //The only case for this is when it is the Host
            }

            //Return the writer to the invoking method.
            return rpcQueueHistoryItem.QueueWriter;
        }

        /// <summary>
        /// EndAddQueueItemToOutboundFrame
        /// Signifies the end of this outbound RPC.
        /// We store final MSG size and track the total current frame queue size
        /// </summary>
        /// <param name="writer">writer that was used</param>
        public void EndAddQueueItemToFrame(NetworkWriter writer, RpcQueueHistoryFrame.QueueFrameType queueFrameType, NetworkUpdateStage updateStage)
        {
            bool getNextFrame = NetworkManager.Singleton.IsHost && queueFrameType == RpcQueueHistoryFrame.QueueFrameType.Inbound;

            var rpcQueueHistoryItem = GetQueueHistoryFrame(queueFrameType, updateStage, getNextFrame);
            var loopBackHistoryFrame = rpcQueueHistoryItem.LoopbackHistoryFrame;

            var pbWriter = (PooledNetworkWriter)writer;
            if (pbWriter != rpcQueueHistoryItem.QueueWriter && !getNextFrame)
            {
                UnityEngine.Debug.LogError($"{nameof(RpcQueueContainer)} {queueFrameType} passed writer is not the same as the current {nameof(PooledNetworkWriter)} for the {queueFrameType}!");
            }

            //The total size of the frame is the last known position of the stream
            rpcQueueHistoryItem.TotalSize = (uint)rpcQueueHistoryItem.QueueBuffer.Position;

            long CurrentPosition = rpcQueueHistoryItem.QueueBuffer.Position;
            ulong BitPosition = rpcQueueHistoryItem.QueueBuffer.BitPosition;

            //////////////////////////////////////////////////////////////
            //>>>> REPOSITIONING STREAM TO RPC MESSAGE SIZE LOCATION <<<<
            //////////////////////////////////////////////////////////////
            rpcQueueHistoryItem.QueueBuffer.Position = rpcQueueHistoryItem.GetCurrentMarkedPosition();

            long MSGOffset = 8;
            if (getNextFrame && IsUsingBatching())
            {
                MSGOffset += 8;
            }

            //subtracting 8 byte to account for the value of the size of the RPC
            long MSGSize = (long)(rpcQueueHistoryItem.TotalSize - (rpcQueueHistoryItem.GetCurrentMarkedPosition() + MSGOffset));

            if (MSGSize > 0)
            {
                //Write the actual size of the RPC message
                rpcQueueHistoryItem.QueueWriter.WriteInt64(MSGSize);
            }
            else
            {
                UnityEngine.Debug.LogWarning("MSGSize of < zero detected!!  Setting message size to zero!");
                rpcQueueHistoryItem.QueueWriter.WriteInt64(0);
            }

            if (loopBackHistoryFrame != null)
            {
                if (MSGSize > 0)
                {
                    //Point to where the size of the message is stored
                    loopBackHistoryFrame.QueueBuffer.Position = loopBackHistoryFrame.GetCurrentMarkedPosition();

                    //Write the actual size of the RPC message
                    loopBackHistoryFrame.QueueWriter.WriteInt64(MSGSize);

                    if (!IsUsingBatching())
                    {
                        //Write the offset for the header info copied
                        loopBackHistoryFrame.QueueWriter.WriteInt64(1);
                    }
                    else
                    {
                        //Write the offset for the header info copied
                        loopBackHistoryFrame.QueueWriter.WriteInt64(0);
                    }

                    //Write RPC data
                    loopBackHistoryFrame.QueueWriter.WriteBytes(rpcQueueHistoryItem.QueueBuffer.GetBuffer(), MSGSize, (int)rpcQueueHistoryItem.QueueBuffer.Position);

                    //Set the total size for this stream
                    loopBackHistoryFrame.TotalSize = (uint)loopBackHistoryFrame.QueueBuffer.Position;

                    //Add the total size to the offsets for parsing over various entries
                    loopBackHistoryFrame.QueueItemOffsets.Add((uint)loopBackHistoryFrame.QueueBuffer.Position);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[LoopBack] MSGSize of < zero detected!!  Setting message size to zero!");
                    //Write the actual size of the RPC message
                    loopBackHistoryFrame.QueueWriter.WriteInt64(0);
                }

                rpcQueueHistoryItem.LoopbackHistoryFrame = null;
            }


            //////////////////////////////////////////////////////////////
            //<<<< REPOSITIONING STREAM BACK TO THE CURRENT TAIL >>>>
            //////////////////////////////////////////////////////////////
            rpcQueueHistoryItem.QueueBuffer.Position = CurrentPosition;
            rpcQueueHistoryItem.QueueBuffer.BitPosition = BitPosition;

            //Add the packed size to the offsets for parsing over various entries
            rpcQueueHistoryItem.QueueItemOffsets.Add((uint)rpcQueueHistoryItem.QueueBuffer.Position);
        }

        /// <summary>
        /// GetQueueHistoryFrame
        /// Gets the current queue history frame (inbound or outbound)
        /// </summary>
        /// <param name="frameType">inbound or outbound</param>
        /// <returns>QueueHistoryFrame or null</returns>
        public RpcQueueHistoryFrame GetQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType frameType, NetworkUpdateStage updateStage, bool getNextFrame = false)
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
                UnityEngine.Debug.LogError($"{nameof(RpcQueueContainer)} {frameType} queue stream buffer index out of range! [{StreamBufferIndex}]");
                return null;
            }

            if (!QueueHistory[frameType][StreamBufferIndex].ContainsKey(updateStage))
            {
                UnityEngine.Debug.LogError($"{nameof(RpcQueueContainer)} {updateStage} update type does not exist!");
                return null;
            }

            return QueueHistory[frameType][StreamBufferIndex][updateStage];
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
                var rpcQueueHistoryItemOutbound = GetQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
                if (rpcQueueHistoryItemOutbound.QueueItemOffsets.Count > 0)
                {
                    //Reset inbound queues based on update stage
                    foreach (NetworkUpdateStage netUpdateStage in Enum.GetValues(typeof(NetworkUpdateStage)))
                    {
                        var rpcQueueHistoryItemInbound = GetQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Inbound, netUpdateStage);
                        ResetQueueHistoryFrame(rpcQueueHistoryItemInbound);
                    }

                    var pooledNetworkBuffer = PooledNetworkBuffer.Get();
                    var rpcFrameQueueItem = rpcQueueHistoryItemOutbound.GetFirstQueueItem();

                    while (rpcFrameQueueItem.QueueItemType != QueueItemType.None)
                    {
                        pooledNetworkBuffer.SetLength(rpcFrameQueueItem.StreamSize);
                        pooledNetworkBuffer.Position = 0;
                        byte[] pooledNetworkStreamArray = pooledNetworkBuffer.GetBuffer();
                        Buffer.BlockCopy(rpcFrameQueueItem.MessageData.Array ?? Array.Empty<byte>(), rpcFrameQueueItem.MessageData.Offset, pooledNetworkStreamArray, 0, (int)rpcFrameQueueItem.StreamSize);

                        if (!IsUsingBatching())
                        {
                            pooledNetworkBuffer.Position = 1;
                        }

                        AddQueueItemToInboundFrame(rpcFrameQueueItem.QueueItemType, UnityEngine.Time.realtimeSinceStartup, rpcFrameQueueItem.NetworkId, pooledNetworkBuffer);
                        rpcFrameQueueItem = rpcQueueHistoryItemOutbound.GetNextQueueItem();
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Initialize
        /// This should be called during primary initialization period (typically during NetworkManager's Start method)
        /// This will allocate [maxFrameHistory] + [1 currentFrame] number of PooledNetworkBuffers and keep them open until the session ends
        /// Note: For zero frame history set maxFrameHistory to zero
        /// </summary>
        /// <param name="maxFrameHistory"></param>
        public void Initialize(uint maxFrameHistory)
        {
            ClearParameters();

            m_RpcQueueProcessor = new RpcQueueProcessor();
            m_MaxFrameHistory = maxFrameHistory + k_MinQueueHistory;

            if (!QueueHistory.ContainsKey(RpcQueueHistoryFrame.QueueFrameType.Inbound))
            {
                QueueHistory.Add(RpcQueueHistoryFrame.QueueFrameType.Inbound, new Dictionary<int, Dictionary<NetworkUpdateStage, RpcQueueHistoryFrame>>());
            }

            if (!QueueHistory.ContainsKey(RpcQueueHistoryFrame.QueueFrameType.Outbound))
            {
                QueueHistory.Add(RpcQueueHistoryFrame.QueueFrameType.Outbound, new Dictionary<int, Dictionary<NetworkUpdateStage, RpcQueueHistoryFrame>>());
            }

            for (int i = 0; i < m_MaxFrameHistory; i++)
            {
                if (!QueueHistory[RpcQueueHistoryFrame.QueueFrameType.Outbound].ContainsKey(i))
                {
                    QueueHistory[RpcQueueHistoryFrame.QueueFrameType.Outbound].Add(i, new Dictionary<NetworkUpdateStage, RpcQueueHistoryFrame>());
                    var queueHistoryFrame = new RpcQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
                    queueHistoryFrame.QueueBuffer = PooledNetworkBuffer.Get();
                    queueHistoryFrame.QueueBuffer.Position = 0;
                    queueHistoryFrame.QueueWriter = PooledNetworkWriter.Get(queueHistoryFrame.QueueBuffer);
                    queueHistoryFrame.QueueReader = PooledNetworkReader.Get(queueHistoryFrame.QueueBuffer);
                    queueHistoryFrame.QueueItemOffsets = new List<uint>();

                    //For now all outbound, we will always have a single update in which they are processed (LATEUPDATE)
                    QueueHistory[RpcQueueHistoryFrame.QueueFrameType.Outbound][i].Add(NetworkUpdateStage.PostLateUpdate, queueHistoryFrame);
                }

                if (!QueueHistory[RpcQueueHistoryFrame.QueueFrameType.Inbound].ContainsKey(i))
                {
                    QueueHistory[RpcQueueHistoryFrame.QueueFrameType.Inbound].Add(i, new Dictionary<NetworkUpdateStage, RpcQueueHistoryFrame>());

                    //For inbound, we create a queue history frame per update stage
                    foreach (NetworkUpdateStage netUpdateStage in Enum.GetValues(typeof(NetworkUpdateStage)))
                    {
                        var rpcQueueHistoryFrame = new RpcQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Inbound, netUpdateStage);
                        rpcQueueHistoryFrame.QueueBuffer = PooledNetworkBuffer.Get();
                        rpcQueueHistoryFrame.QueueBuffer.Position = 0;
                        rpcQueueHistoryFrame.QueueWriter = PooledNetworkWriter.Get(rpcQueueHistoryFrame.QueueBuffer);
                        rpcQueueHistoryFrame.QueueReader = PooledNetworkReader.Get(rpcQueueHistoryFrame.QueueBuffer);
                        rpcQueueHistoryFrame.QueueItemOffsets = new List<uint>();
                        QueueHistory[RpcQueueHistoryFrame.QueueFrameType.Inbound][i].Add(netUpdateStage, rpcQueueHistoryFrame);
                    }
                }
            }

            //As long as this instance is using the pre-defined update stages
            if (!m_ProcessUpdateStagesExternally)
            {
                //Register with the network update loop system
                this.RegisterAllNetworkUpdates();
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
            //As long as this instance is using the pre-defined update stages
            if (!m_ProcessUpdateStagesExternally)
            {
                //Remove ourself from the network loop update system
                this.UnregisterAllNetworkUpdates();
            }

            //We need to make sure all internal messages (i.e. object destroy) are sent
            m_RpcQueueProcessor.InternalMessagesSendAndFlush();

            //Dispose of any readers and writers
            foreach (var queueHistorySection in QueueHistory)
            {
                foreach (var queueHistoryItemByStage in queueHistorySection.Value)
                {
                    foreach (var queueHistoryItem in queueHistoryItemByStage.Value)
                    {
                        queueHistoryItem.Value.QueueWriter?.Dispose();
                        queueHistoryItem.Value.QueueReader?.Dispose();
                        queueHistoryItem.Value.QueueBuffer?.Dispose();
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