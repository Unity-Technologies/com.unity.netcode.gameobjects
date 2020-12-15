using MLAPI.Serialization.Pooled;
using MLAPI.Serialization;
using MLAPI.Configuration;
using MLAPI.Profiling;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MLAPI
{
    class BatchUtil
    {
        private Dictionary<ulong, SendStream> SendDict = new Dictionary<ulong, SendStream>();

        public void PushLength(int length, ref PooledBitWriter writer)
        {
            // supports lengths up to 16-bit wide
            if (length < 255)
            {
                writer.WriteByte((byte)length); // write the amounts of bytes that are coming up
            }
            else
            {
                writer.WriteByte(255); // mark larger size
                writer.WriteByte((byte)(length % 256)); // write the length modulo 256
                writer.WriteByte((byte)(length / 256)); // write the length divided by 256
            }
        }

        public int PopLength(in BitStream messageStream)
        {
            int length = 0;
            byte read = (byte)messageStream.ReadByte();
            if (read != 255)
            {
                return read;
            }
            length += messageStream.ReadByte();
            length += messageStream.ReadByte() * 256;

            return length;
        }

        /// <summary>
        /// GetTargetList
        /// Returns the list of ClientId an item is targeted to
        /// </summary>
        /// <param name="item">the FrameQueueItem we want targets for</param>
        private static List<ulong> GetTargetList(FrameQueueItem item)
        {
            List<ulong> ret = new List<ulong>();
            switch (item.QueueItemType)
            {
                case RPCQueueManager.QueueItemType.ServerRPC:
                    ret.Add(item.NetworkId);
                    break;
                case RPCQueueManager.QueueItemType.ClientRPC:
                    ret = item.ClientIds;
                    break;
                default:
                    break;
            }
            return ret;
        }

        /// <summary>
        /// QueueItem
        /// Add a FrameQueueItem to be sent
        /// </summary>queueItem
        /// <param name="item">the threshold in bytes</param>
        public void QueueItem(FrameQueueItem item)
        {
            foreach (ulong clientId in GetTargetList(item))
            {
                // todo: actually queue and buffer. For now, the dict contains just one entry !!!

                if (!SendDict.ContainsKey(clientId))
                {
                    SendDict[clientId] = new SendStream();
                    SendDict[clientId].Item = item;
                    SendDict[clientId].Writer = new PooledBitWriter(SendDict[clientId].Stream);

                    SendDict[clientId].Writer.WriteBit(false); // Encrypted
                    SendDict[clientId].Writer.WriteBit(false); // Authenticated

                    switch (item.QueueItemType)
                    {
                        case RPCQueueManager.QueueItemType.ServerRPC:
                            SendDict[clientId].Writer.WriteBits(MLAPIConstants.MLAPI_STD_SERVER_RPC, 6); // MessageType
                            break;
                        case RPCQueueManager.QueueItemType.ClientRPC:
                            SendDict[clientId].Writer.WriteBits(MLAPIConstants.MLAPI_STD_CLIENT_RPC, 6); // MessageType
                            break;
                    }
                }

                // write the amounts of bytes that are coming up
                PushLength(item.MessageData.Count, ref SendDict[clientId].Writer);

                // write the message to send
                // todo: is there a faster alternative to .ToArray()
                SendDict[clientId].Writer.WriteBytes(item.MessageData.ToArray(), item.MessageData.Count);

                ProfilerStatManager.bytesSent.Record((int)item.MessageData.Count);
                ProfilerStatManager.rpcsSent.Record();
            }
        }

        /// <summary>
        /// SendItems
        /// Send any batch of RPC that are of length above threshold
        /// </summary>
        /// <param name="threshold">the threshold in bytes</param>
        public void SendItems(int threshold)
        {
            List<ulong> sent = new List<ulong>();

            foreach (KeyValuePair<ulong, SendStream> entry in SendDict)
            {
                // read the queued message
                using PooledBitWriter writer = SendDict[entry.Key].Writer;
                int length = (int)writer.GetStream().Length;

                if (length >= threshold)
                {
                    byte[] byteBuffer = new byte[length];

                    Byte[] bytes = ((MLAPI.Serialization.BitStream)writer.GetStream()).GetBuffer();
                    System.Buffer.BlockCopy(bytes, 0, byteBuffer, 0, length);

                    ArraySegment<byte> sendBuffer = new ArraySegment<byte>(byteBuffer, 0, length);

                    NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(entry.Key, sendBuffer,
                        string.IsNullOrEmpty(entry.Value.Item.Channel) ? "MLAPI_DEFAULT_MESSAGE" : entry.Value.Item.Channel);

                    ProfilerStatManager.rpcBatchesSent.Record();

                    // mark the client for which a batch was sent
                    sent.Add(entry.Key);
                }
            }

            // clear the batch that were sent from the SendDict
            foreach(ulong clientid in sent)
            {
                SendDict.Remove(clientid);
            }
        }
    }



}
