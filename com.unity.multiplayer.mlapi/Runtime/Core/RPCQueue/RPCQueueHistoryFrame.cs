using System.Collections.Generic;
using MLAPI.Serialization.Pooled;

namespace MLAPI
{
    /// <summary>
    /// QueueHistoryFrame
    /// All queued RPCs end up in a PooledBitStream within a QueueHistoryFrame instance.
    /// </summary>
    public class QueueHistoryFrame
    {
        public enum QueueFrameType
        {
            Inbound,
            Outbound,
        }

        public uint TotalSize;

        public PooledBitStream QueueStream;
        public PooledBitWriter QueueWriter;
        public PooledBitReader QueueReader;

        public List<uint> QueueItemOffsets;

        private int QueueItemOffsetIndex;
        private FrameQueueItem CurrentQueueItem;
        private readonly QueueFrameType queueFrameType;

        long CurrentStreamSizeMark;

        /// <summary>
        /// GetQueueFrameType
        /// Returns whether this is an inbound or outbound frame
        /// </summary>
        /// <returns></returns>
        public QueueFrameType GetQueueFrameType()
        {
            return queueFrameType;
        }

        /// <summary>
        /// MarkCurrentStreamSize
        /// Marks the current size of the stream  (used primarily for sanity checks)
        /// </summary>
        public void MarkCurrentStreamPosition()
        {
            if (QueueStream != null)
            {
                CurrentStreamSizeMark = QueueStream.Position;
            }
            else
            {
                CurrentStreamSizeMark = 0;
            }
        }

        public long GetCurrentMarkedPosition()
        {
            return CurrentStreamSizeMark;
        }


        /// <summary>
        /// GetCurrentQueueItem
        /// Internal method to get the current Queue Item from the stream at its current position
        /// </summary>
        /// <returns>FrameQueueItem</returns>
        private FrameQueueItem GetCurrentQueueItem()
        {
            //Write the packed version of the queueItem to our current queue history buffer
            CurrentQueueItem.QueueItemType = (RPCQueueManager.QueueItemType)QueueReader.ReadUInt16();
            CurrentQueueItem.SendFlags = (Security.SecuritySendFlags)QueueReader.ReadUInt16();
            QueueReader.ReadSingle();
            CurrentQueueItem.NetworkId = QueueReader.ReadUInt64();

            //NSS: For now, with inbound we are punting on reading the channel and clients for these reasons:
            //1.) For right now, channels are not pertinent as the message is already received
            //2.) Clients are not pertinent as a received RPC is a "single target" destination
            if (queueFrameType == QueueFrameType.Inbound)
            {
                QueueReader.ReadByte();
                QueueReader.ReadInt32();
                CurrentQueueItem.ClientIds = new ulong[0];
            }
            else
            {
                //NSS: Outbound we care about both channel and clients
                CurrentQueueItem.Channel = QueueReader.ReadString().ToString();
                int NumClients = QueueReader.ReadInt32();
                if (NumClients > 0 && NumClients < 16)
                {
                    ulong[] clientIdArray = new ulong[NumClients];
                    for (int i = 0; i < NumClients; i++)
                    {
                        clientIdArray[i] = QueueReader.ReadUInt64();
                    }

                    if (CurrentQueueItem.ClientIds == null)
                    {
                        CurrentQueueItem.ClientIds = clientIdArray;
                    }
                    else
                    {
                        CurrentQueueItem.ClientIds = clientIdArray;
                    }
                }
                else
                {
                    CurrentQueueItem.ClientIds = new ulong[0];
                }
            }

            //Get the stream size
            CurrentQueueItem.StreamSize = QueueReader.ReadInt64();

            if(CurrentQueueItem.StreamSize < 131072 && CurrentQueueItem.StreamSize > 0)
            {

                //Inbound and Outbound message streams are handled differently
                if (queueFrameType == QueueFrameType.Inbound)
                {
                    //Get our offset
                    long Position = QueueReader.ReadInt64();
                    //Always make sure we are positioned at the start of the stream before we write
                    CurrentQueueItem.ItemStream.Position = 0;

                    //Write the entire message to the CurrentQueueItem stream (1 stream is re-used for all incoming RPCs)
                    CurrentQueueItem.StreamWriter.ReadAndWrite(QueueReader, CurrentQueueItem.StreamSize);

                    //Reset the position back to the offset so std rpc API can process the message properly
                    //(i.e. minus the already processed header)
                    CurrentQueueItem.ItemStream.Position = Position;
                }
                else
                {
                    //Create a byte array segment for outbound sending
                    CurrentQueueItem.MessageData = QueueReader.CreateArraySegment((int)CurrentQueueItem.StreamSize, (int)QueueStream.Position);
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("CurrentQueueItem.StreamSize exceeds allowed size (128KB vs " +CurrentQueueItem.StreamSize.ToString() + ")! Exiting Current RPC Queue Enumeration Loop! ");
                CurrentQueueItem.QueueItemType = RPCQueueManager.QueueItemType.None;
            }
            return CurrentQueueItem;
        }

        /// <summary>
        /// GetNextQueueItem
        /// Handles getting the next queue item from this frame
        /// If none are remaining, then it returns a queue item type of NONE
        /// </summary>
        /// <returns>FrameQueueItem</returns>
        public FrameQueueItem GetNextQueueItem()
        {
            QueueStream.Position = QueueItemOffsets[QueueItemOffsetIndex];
            QueueItemOffsetIndex++;
            if (QueueItemOffsetIndex >= QueueItemOffsets.Count)
            {
                CurrentQueueItem.QueueItemType = RPCQueueManager.QueueItemType.None;
                return CurrentQueueItem;
            }

            return GetCurrentQueueItem();
        }

        /// <summary>
        /// GetFirstQueueItem
        /// Should be called the first time a queue item is pulled from a queue history frame.
        /// This will reset the frame's stream indices and add a new stream and stream writer to the CurrentQueueItem instance.
        /// </summary>
        /// <returns>FrameQueueItem</returns>
        public FrameQueueItem GetFirstQueueItem()
        {
            if (QueueStream.Position > 0)
            {
                QueueItemOffsetIndex = 0;
                QueueStream.Position = 0;

                if (queueFrameType == QueueFrameType.Inbound)
                {
                    CurrentQueueItem.ItemStream = PooledBitStream.Get();
                    CurrentQueueItem.StreamWriter = PooledBitWriter.Get(CurrentQueueItem.ItemStream);
                    CurrentQueueItem.StreamReader = PooledBitReader.Get(CurrentQueueItem.ItemStream);
                }

                return GetCurrentQueueItem();
            }
            else
            {
                CurrentQueueItem.QueueItemType = RPCQueueManager.QueueItemType.None;
                return CurrentQueueItem;
            }
        }

        /// <summary>
        /// CloseQueue
        /// Should be called once all processing of the current frame is complete.
        /// This only closes the CurrentQueueItem's stream which is used as a "middle-man" (currently)
        /// for delivering the RPC message to the method requesting a queue item from a frame.
        /// </summary>
        public void CloseQueue()
        {
            if (CurrentQueueItem.StreamWriter != null)
            {
                CurrentQueueItem.StreamWriter.Dispose();
                CurrentQueueItem.StreamWriter = null;
            }

            if (CurrentQueueItem.StreamReader != null)
            {
                CurrentQueueItem.StreamReader.Dispose();
                CurrentQueueItem.StreamReader = null;
            }

            if (CurrentQueueItem.ItemStream != null)
            {
                CurrentQueueItem.ItemStream.Dispose();
                CurrentQueueItem.ItemStream = null;
            }
        }

        /// <summary>
        /// QueueHistoryFrame Constructor
        /// </summary>
        /// <param name="queueType">type of queue history frame (Inbound/Outbound)</param>
        public QueueHistoryFrame(QueueFrameType queueType)
        {
            queueFrameType = queueType;
            CurrentQueueItem = new FrameQueueItem();
        }
    }
}
