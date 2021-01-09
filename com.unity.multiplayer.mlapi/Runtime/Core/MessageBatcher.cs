using MLAPI.Serialization.Pooled;
using MLAPI.Serialization;
using MLAPI.Configuration;
using MLAPI.Profiling;
using MLAPI.Messaging;
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
            public PooledBitStream Stream = PooledBitStream.Get();
            public bool Empty = true;
        }

        // Stores the stream of batched RPC to send to each client, by ClientId
        private Dictionary<ulong, SendStream> SendDict = new Dictionary<ulong, SendStream>();
        private PooledBitWriter Writer = new PooledBitWriter(PooledBitStream.Get());

        // Used to store targets, internally
        private ulong[] TargetList = new ulong[0];

        // Used to mark longer lengths. Works because we can't have zero-sized messages
        private const byte LongLenMarker = 0;

        private void PushLength(int length, ref PooledBitWriter writer)
        {
            // If length is single byte we write it
            if (length < 256)
            {
                writer.WriteByte((byte)length); // write the amounts of bytes that are coming up
            }
            else
            {
                // otherwise we write a two-byte length
                writer.WriteByte(LongLenMarker); // mark larger size
                writer.WriteByte((byte)(length % 256)); // write the length modulo 256
                writer.WriteByte((byte)(length / 256)); // write the length divided by 256
            }
        }

        private int PopLength(in BitStream messageStream)
        {
            int read = messageStream.ReadByte();
            // if we read a non-zero value, we have a single byte length
            // or a -1 error we can return
            if (read != LongLenMarker)
            {
                return read;
            }
            // otherwise, a two-byte length follows. We'll read in len1, len2
            int len1 = messageStream.ReadByte();
            if (len1 < 0)
            {
                // pass errors back to caller
                return len1;
            }
            int len2 = messageStream.ReadByte();
            if (len2 < 0)
            {
                // pass errors back to caller
                return len2;
            }

            return len1 + len2 * 256;
        }

        /// <summary>
        /// FillTargetList
        /// Fills a list with the ClientId's an item is targeted to
        /// </summary>
        /// <param name="item">the FrameQueueItem we want targets for</param>
        /// <param name="list">the list to fill</param>
        private static void FillTargetList(in FrameQueueItem item, ref ulong[] list)
        {
            switch (item.queueItemType)
            {
                case RpcQueueContainer.QueueItemType.ServerRpc:
                    Array.Resize(ref list, 1);
                    list[0] = item.networkId;
                    break;
                case RpcQueueContainer.QueueItemType.ClientRpc:
                    // copy the list
                    list = item.clientIds.ToArray();
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
                if (!SendDict.ContainsKey(clientId))
                {
                    // todo: consider what happens if many clients join and leave the game consecutively
                    // we probably need a cleanup mechanism at some point
                    SendDict[clientId] = new SendStream();
                }

                if (SendDict[clientId].Empty)
                {
                    SendDict[clientId].Empty = false;
                    SendDict[clientId].Item = item;
                    Writer.SetStream(SendDict[clientId].Stream);

                    Writer.WriteBit(false); // Encrypted
                    Writer.WriteBit(false); // Authenticated

                    switch (item.queueItemType)
                    {
                        // 6 bits are used for the message type, which is an MLAPIConstants
                        case RpcQueueContainer.QueueItemType.ServerRpc:
                            Writer.WriteBits(MLAPIConstants.MLAPI_SERVER_RPC, 6); // MessageType
                            break;
                        case RpcQueueContainer.QueueItemType.ClientRpc:
                            Writer.WriteBits(MLAPIConstants.MLAPI_CLIENT_RPC, 6); // MessageType
                            break;
                    }
                }

                // write the amounts of bytes that are coming up
                PushLength(item.messageData.Count, ref Writer);

                // write the message to send
                // todo: is there a faster alternative to .ToArray()
                Writer.WriteBytes(item.messageData.ToArray(), item.messageData.Count);

                ProfilerStatManager.bytesSent.Record((int)item.messageData.Count);
                ProfilerStatManager.rpcsSent.Record();
            }
        }

        public delegate void SendCallbackType(ulong clientId, SendStream messageStream);
        public delegate void ReceiveCallbackType(BitStream messageStream, RpcQueueContainer.QueueItemType messageType, ulong clientId, float time);

        /// <summary>
        /// SendItems
        /// Send any batch of RPC that are of length above threshold
        /// </summary>
        /// <param name="threshold"> the threshold in bytes</param>
        /// <param name="sendCallback"> the function to call for sending the batch</param>
        public void SendItems(int threshold, SendCallbackType sendCallback)
        {
            foreach (KeyValuePair<ulong, SendStream> entry in SendDict)
            {
                if (!entry.Value.Empty)
                {
                    // read the queued message
                    int length = (int)SendDict[entry.Key].Stream.Length;

                    if (length >= threshold)
                    {
                        sendCallback(entry.Key, entry.Value);
                        // clear the batch that was sent from the SendDict
                        entry.Value.Stream.SetLength(0);
                        entry.Value.Stream.Position = 0;
                        entry.Value.Empty = true;
                        ProfilerStatManager.rpcBatchesSent.Record();
                    }
                }
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
        public int ReceiveItems(in BitStream messageStream, ReceiveCallbackType receiveCallback, RpcQueueContainer.QueueItemType messageType, ulong clientId, float receiveTime)
        {
            do
            {
                // read the length of the next RPC
                int rpcSize = PopLength(messageStream);

                if (rpcSize < 0)
                {
                    // abort if there's an error reading lengths
                    return 0;
                }

                // copy what comes after current stream position
                long pos = messageStream.Position;
                BitStream copy = PooledBitStream.Get();
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
