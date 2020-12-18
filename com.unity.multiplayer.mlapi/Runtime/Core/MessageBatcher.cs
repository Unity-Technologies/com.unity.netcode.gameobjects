using MLAPI.Serialization.Pooled;
using MLAPI.Serialization;
using MLAPI.Configuration;
using MLAPI.Profiling;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace MLAPI
{
    class MessageBatcher
    {
        public class SendStream
        {
            public FrameQueueItem Item;
            public PooledBitStream Stream = new PooledBitStream();
        }

        // Stores the stream of batched RPC to send to each client, by ClientId
        private Dictionary<ulong, SendStream> SendDict = new Dictionary<ulong, SendStream>();
        private PooledBitWriter Writer = new PooledBitWriter(new PooledBitStream());

        // Used to store targets, internally
        private List<ulong> TargetList = new List<ulong>();

        public void PushLength(int length, ref PooledBitWriter writer)
        {
            // If length is single byte we write it
            if (length < 256)
            {
                writer.WriteByte((byte)length); // write the amounts of bytes that are coming up
            }
            else
            {
                // otherwise we write a 0, and a two-byte length
                writer.WriteByte(0); // mark larger size
                writer.WriteByte((byte)(length % 256)); // write the length modulo 256
                writer.WriteByte((byte)(length / 256)); // write the length divided by 256
            }
        }

        public int PopLength(in BitStream messageStream)
        {
            byte read = (byte)messageStream.ReadByte();
            // if we read a non-zero value, we have a single byte length
            if (read != 0)
            {
                return read;
            }
            // otherwise, a two-byte length follows
            int length = messageStream.ReadByte();
            length += messageStream.ReadByte() * 256;

            return length;
        }

        /// <summary>
        /// FillTargetList
        /// Fills a list with the ClientId's an item is targeted to
        /// </summary>
        /// <param name="item">the FrameQueueItem we want targets for</param>
        /// <param name="list">the list to fill</param>
        private static void FillTargetList(in FrameQueueItem item, ref List<ulong> list)
        {
            list.Clear();
            switch (item.QueueItemType)
            {
                case RPCQueueManager.QueueItemType.ServerRPC:
                    list.Add(item.NetworkId);
                    break;
                case RPCQueueManager.QueueItemType.ClientRPC:
                    foreach (ulong clientId in item.ClientIds)
                    {
                        list.Add(clientId);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// QueueItem
        /// Add a FrameQueueItem to be sent
        /// </summary>queueItem
        /// <param name="item">the threshold in bytes</param>
        public void QueueItem(in FrameQueueItem item)
        {
            FillTargetList(item, ref TargetList);

            foreach (ulong clientId in TargetList)
            {
                // todo: actually queue and buffer. For now, the dict contains just one entry !!!

                if (!SendDict.ContainsKey(clientId))
                {
                    SendDict[clientId] = new SendStream();
                    SendDict[clientId].Item = item;
                    Writer.SetStream(SendDict[clientId].Stream);

                    Writer.WriteBit(false); // Encrypted
                    Writer.WriteBit(false); // Authenticated

                    switch (item.QueueItemType)
                    {
                        // 6 bits are used for the message type, which is an MLAPIConstants
                        case RPCQueueManager.QueueItemType.ServerRPC:
                            Writer.WriteBits(MLAPIConstants.MLAPI_STD_SERVER_RPC, 6); // MessageType
                            break;
                        case RPCQueueManager.QueueItemType.ClientRPC:
                            Writer.WriteBits(MLAPIConstants.MLAPI_STD_CLIENT_RPC, 6); // MessageType
                            break;
                    }
                }

                // write the amounts of bytes that are coming up
                PushLength(item.MessageData.Count, ref Writer);

                // write the message to send
                // todo: is there a faster alternative to .ToArray()
                Writer.WriteBytes(item.MessageData.ToArray(), item.MessageData.Count);

                ProfilerStatManager.bytesSent.Record((int)item.MessageData.Count);
                ProfilerStatManager.rpcsSent.Record();
            }
        }

        public delegate void SendCallbackType(ulong clientId, SendStream messageStream);
        public delegate void ReceiveCallbackType(BitStream messageStream, MLAPI.RPCQueueManager.QueueItemType messageType, ulong clientId, float time);

        /// <summary>
        /// SendItems
        /// Send any batch of RPC that are of length above threshold
        /// </summary>
        /// <param name="threshold"> the threshold in bytes</param>
        /// <param name="sendCallback"> the function to call for sending the batch</param>
        public void SendItems(int threshold, SendCallbackType sendCallback)
        {
            List<ulong> sent = new List<ulong>();

            foreach (KeyValuePair<ulong, SendStream> entry in SendDict)
            {
                // read the queued message
                int length = (int)SendDict[entry.Key].Stream.Length;

                if (length >= threshold)
                {
                    sendCallback(entry.Key, entry.Value);
                    ProfilerStatManager.rpcBatchesSent.Record();

                    // mark the client for which a batch was sent
                    sent.Add(entry.Key);
                }
            }

            // clear the batches that were sent from the SendDict
            // this cannot be done above acuring the Dictionary iteration, so we do it here
            foreach(ulong clientid in sent)
            {
                SendDict.Remove(clientid);
            }
        }

        /// <summary>
        /// ReceiveItems
        /// Process the messageStream and call the callback with individual RPC messages
        /// </summary>
        /// <param name="messageStream"> the messageStream containing the batched RPC</param>
        /// <param name="receiveCallback"> the callback to call has type int f(message, type, clientId, time) </param>
        /// <param name="messageType"> the message type to pass back to callback</param>
        /// <param name="clientId"> the clientId to pass back to callback</param>
        /// <param name="receiveTime"> the packet receive time to pass back to callback</param>
        public int ReceiveItems(in BitStream messageStream, ReceiveCallbackType receiveCallback, MLAPI.RPCQueueManager.QueueItemType messageType, ulong clientId, float receiveTime)
        {
            do
            {
                // read the length of the next RPC
                int rpcSize = PopLength(messageStream);

                // copy what comes after current stream position
                long pos = messageStream.Position;
                BitStream copy = new BitStream();
                copy.SetLength(rpcSize);
                copy.Position = 0;
                Buffer.BlockCopy(messageStream.GetBuffer(), (int)pos, copy.GetBuffer(), 0, rpcSize);

                receiveCallback(copy, messageType, clientId, receiveTime);

                // seek over the RPC
                // RPCReceiveQueueItem peeks at content, it doesn't advance
                messageStream.Seek(rpcSize, SeekOrigin.Current);
            } while (messageStream.Position < messageStream.Length);
            return 0;
        }

    }
}
