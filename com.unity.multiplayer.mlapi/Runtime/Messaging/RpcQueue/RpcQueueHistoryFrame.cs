using System.Collections.Generic;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports;

namespace MLAPI.Messaging
{
    /// <summary>
    /// QueueHistoryFrame
    /// Used by the RpcQueueContainer to hold queued RPCs
    /// All queued Rpcs end up in a PooledNetworkBuffer within a QueueHistoryFrame instance.
    /// </summary>
    public class RpcQueueHistoryFrame
    {
        public enum QueueFrameType
        {
            Inbound,
            Outbound,
        }

        public bool IsDirty; //Used to determine if this queue history frame has been reset (cleaned) yet
        public bool HasLoopbackData; //Used to determine if a dirt frame is dirty because rpcs are being looped back betwen HostClient and HostServer
        public uint TotalSize;
        public List<uint> QueueItemOffsets;

        public PooledNetworkBuffer QueueBuffer;
        public PooledNetworkWriter QueueWriter;
        public RpcQueueHistoryFrame LoopbackHistoryFrame; //Temporary fix for Host mode loopback work around.


        public PooledNetworkReader QueueReader;

        private int m_QueueItemOffsetIndex;
        private RpcFrameQueueItem m_CurrentQueueItem;
        private readonly QueueFrameType m_QueueFrameType;
        private int m_MaximumClients;
        private long m_CurrentStreamSizeMark;
        private NetworkUpdateStage m_StreamUpdateStage; //Update stage specific to RPCs (typically inbound has most potential for variation)
        private const int k_MaxStreamBounds = 131072;
        private const int k_MinStreamBounds = 0;

        /// <summary>
        /// GetQueueFrameType
        /// Returns whether this is an inbound or outbound frame
        /// </summary>
        /// <returns></returns>
        public QueueFrameType GetQueueFrameType()
        {
            return m_QueueFrameType;
        }

        /// <summary>
        /// MarkCurrentStreamSize
        /// Marks the current size of the stream  (used primarily for sanity checks)
        /// </summary>
        public void MarkCurrentStreamPosition()
        {
            if (QueueBuffer != null)
            {
                m_CurrentStreamSizeMark = QueueBuffer.Position;
            }
            else
            {
                m_CurrentStreamSizeMark = 0;
            }
        }

        /// <summary>
        /// Returns the current position that was marked (to track size of RPC msg)
        /// </summary>
        /// <returns>m_CurrentStreamSizeMark</returns>
        public long GetCurrentMarkedPosition()
        {
            return m_CurrentStreamSizeMark;
        }

        /// <summary>
        /// GetCurrentQueueItem
        /// Internal method to get the current Queue Item from the stream at its current position
        /// </summary>
        /// <returns>FrameQueueItem</returns>
        private RpcFrameQueueItem GetCurrentQueueItem()
        {
            //Write the packed version of the queueItem to our current queue history buffer
            m_CurrentQueueItem.QueueItemType = (RpcQueueContainer.QueueItemType)QueueReader.ReadUInt16();
            m_CurrentQueueItem.Timestamp = QueueReader.ReadSingle();
            m_CurrentQueueItem.NetworkId = QueueReader.ReadUInt64();

            //Clear out any current value for the client ids
            m_CurrentQueueItem.ClientNetworkIds = new ulong[0];

            //If outbound, determine if any client ids needs to be added
            if (m_QueueFrameType == QueueFrameType.Outbound)
            {
                //Outbound we care about both channel and clients
                m_CurrentQueueItem.NetworkChannel = (NetworkChannel)QueueReader.ReadByteDirect();
                int NumClients = QueueReader.ReadInt32();
                if (NumClients > 0 && NumClients < m_MaximumClients)
                {
                    ulong[] clientIdArray = new ulong[NumClients];
                    for (int i = 0; i < NumClients; i++)
                    {
                        clientIdArray[i] = QueueReader.ReadUInt64();
                    }

                    if (m_CurrentQueueItem.ClientNetworkIds == null)
                    {
                        m_CurrentQueueItem.ClientNetworkIds = clientIdArray;
                    }
                    else
                    {
                        m_CurrentQueueItem.ClientNetworkIds = clientIdArray;
                    }
                }
            }

            m_CurrentQueueItem.UpdateStage = m_StreamUpdateStage;

            //Get the stream size
            m_CurrentQueueItem.StreamSize = QueueReader.ReadInt64();

            //Sanity checking for boundaries
            if (m_CurrentQueueItem.StreamSize < k_MaxStreamBounds && m_CurrentQueueItem.StreamSize > k_MinStreamBounds)
            {
                //Inbound and Outbound message streams are handled differently
                if (m_QueueFrameType == QueueFrameType.Inbound)
                {
                    //Get our offset
                    long Position = QueueReader.ReadInt64();

                    //Always make sure we are positioned at the start of the stream before we write
                    m_CurrentQueueItem.NetworkBuffer.Position = 0;

                    //Write the entire message to the m_CurrentQueueItem stream (1 stream is re-used for all incoming RPCs)
                    m_CurrentQueueItem.NetworkWriter.ReadAndWrite(QueueReader, m_CurrentQueueItem.StreamSize);

                    //Reset the position back to the offset so std rpc API can process the message properly
                    //(i.e. minus the already processed header)
                    m_CurrentQueueItem.NetworkBuffer.Position = Position;
                }
                else
                {
                    //Create a byte array segment for outbound sending
                    m_CurrentQueueItem.MessageData = QueueReader.CreateArraySegment((int)m_CurrentQueueItem.StreamSize, (int)QueueBuffer.Position);
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning($"{nameof(m_CurrentQueueItem)}.{nameof(RpcFrameQueueItem.StreamSize)} exceeds allowed size ({k_MaxStreamBounds} vs {m_CurrentQueueItem.StreamSize})! Exiting from the current RpcQueue enumeration loop!");
                m_CurrentQueueItem.QueueItemType = RpcQueueContainer.QueueItemType.None;
            }

            return m_CurrentQueueItem;
        }

        /// <summary>
        /// GetNextQueueItem
        /// Handles getting the next queue item from this frame
        /// If none are remaining, then it returns a queue item type of NONE
        /// </summary>
        /// <returns>FrameQueueItem</returns>
        internal RpcFrameQueueItem GetNextQueueItem()
        {
            QueueBuffer.Position = QueueItemOffsets[m_QueueItemOffsetIndex];
            m_QueueItemOffsetIndex++;
            if (m_QueueItemOffsetIndex >= QueueItemOffsets.Count)
            {
                m_CurrentQueueItem.QueueItemType = RpcQueueContainer.QueueItemType.None;
                return m_CurrentQueueItem;
            }

            return GetCurrentQueueItem();
        }

        /// <summary>
        /// GetFirstQueueItem
        /// Should be called the first time a queue item is pulled from a queue history frame.
        /// This will reset the frame's stream indices and add a new stream and stream writer to the m_CurrentQueueItem instance.
        /// </summary>
        /// <returns>FrameQueueItem</returns>
        internal RpcFrameQueueItem GetFirstQueueItem()
        {
            if (QueueBuffer.Position > 0)
            {
                m_QueueItemOffsetIndex = 0;
                QueueBuffer.Position = 0;

                if (m_QueueFrameType == QueueFrameType.Inbound)
                {
                    if (m_CurrentQueueItem.NetworkBuffer == null)
                    {
                        m_CurrentQueueItem.NetworkBuffer = PooledNetworkBuffer.Get();
                    }

                    if (m_CurrentQueueItem.NetworkWriter == null)
                    {
                        m_CurrentQueueItem.NetworkWriter = PooledNetworkWriter.Get(m_CurrentQueueItem.NetworkBuffer);
                    }

                    if (m_CurrentQueueItem.NetworkReader == null)
                    {
                        m_CurrentQueueItem.NetworkReader = PooledNetworkReader.Get(m_CurrentQueueItem.NetworkBuffer);
                    }
                }

                return GetCurrentQueueItem();
            }

            m_CurrentQueueItem.QueueItemType = RpcQueueContainer.QueueItemType.None;
            return m_CurrentQueueItem;
        }

        /// <summary>
        /// CloseQueue
        /// Should be called once all processing of the current frame is complete.
        /// This only closes the m_CurrentQueueItem's stream which is used as a "middle-man" (currently)
        /// for delivering the RPC message to the method requesting a queue item from a frame.
        /// </summary>
        public void CloseQueue()
        {
            if (m_CurrentQueueItem.NetworkWriter != null)
            {
                m_CurrentQueueItem.NetworkWriter.Dispose();
                m_CurrentQueueItem.NetworkWriter = null;
            }

            if (m_CurrentQueueItem.NetworkReader != null)
            {
                m_CurrentQueueItem.NetworkReader.Dispose();
                m_CurrentQueueItem.NetworkReader = null;
            }

            if (m_CurrentQueueItem.NetworkBuffer != null)
            {
                m_CurrentQueueItem.NetworkBuffer.Dispose();
                m_CurrentQueueItem.NetworkBuffer = null;
            }
        }

        /// <summary>
        /// QueueHistoryFrame Constructor
        /// </summary>
        /// <param name="queueType">type of queue history frame (Inbound/Outbound)</param>
        public RpcQueueHistoryFrame(QueueFrameType queueType, NetworkUpdateStage updateStage, int maxClients = 512)
        {
            m_MaximumClients = maxClients;
            m_QueueFrameType = queueType;
            m_CurrentQueueItem = new RpcFrameQueueItem();
            m_StreamUpdateStage = updateStage;
        }
    }
}