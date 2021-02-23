using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MLAPI.Serialization.Pooled;
using MLAPI.Serialization;
using MLAPI.Configuration;
using MLAPI.Profiling;
using MLAPI.Transports;

namespace MLAPI.Messaging
{
    internal class RpcBatcher
    {
        public class SendStream
        {
            public Channel Channel;
            public PooledBitStream Stream;
            public PooledBitWriter Writer;
            public bool IsEmpty = true;

            public SendStream()
            {
                Stream = PooledBitStream.Get();
                Writer = PooledBitWriter.Get(Stream);
            }
        }

        // Stores the stream of batched RPC to send to each client, by ClientId
        private readonly Dictionary<ulong, SendStream> SendDict = new Dictionary<ulong, SendStream>();

        // Used to store targets, internally
        private ulong[] TargetList = new ulong[0];

        // Used to mark longer lengths. Works because we can't have zero-sized messages
        private const byte k_LongLenMarker = 0;

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
                writer.WriteByte(k_LongLenMarker); // mark larger size
                writer.WriteByte((byte)(length % 256)); // write the length modulo 256
                writer.WriteByte((byte)(length / 256)); // write the length divided by 256
            }
        }

        private int PopLength(in BitStream messageStream)
        {
            int read = messageStream.ReadByte();
            // if we read a non-zero value, we have a single byte length
            // or a -1 error we can return
            if (read != k_LongLenMarker)
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
        /// <param name="queueItem">the FrameQueueItem we want targets for</param>
        /// <param name="networkIdList">the list to fill</param>
        private static void FillTargetList(in RpcFrameQueueItem queueItem, ref ulong[] networkIdList)
        {
            switch (queueItem.queueItemType)
            {
                // todo: revisit .resize() and .ToArry() usage, for performance
                case RpcQueueContainer.QueueItemType.ServerRpc:
                    Array.Resize(ref networkIdList, 1);
                    networkIdList[0] = queueItem.networkId;
                    break;
                default:
                    // todo: consider the implications of default usage of queueItem.clientIds
                case RpcQueueContainer.QueueItemType.ClientRpc:
                    // copy the list
                    networkIdList = queueItem.clientIds.ToArray();
                    break;
            }
        }

        /// <summary>
        /// QueueItem
        /// Add a FrameQueueItem to be sent
        /// </summary>queueItem
        /// <param name="queueItem">the threshold in bytes</param>
        public void QueueItem(in RpcFrameQueueItem queueItem)
        {
            FillTargetList(queueItem, ref TargetList);

            foreach (ulong clientId in TargetList)
            {
                if (!SendDict.ContainsKey(clientId))
                {
                    // todo: consider what happens if many clients join and leave the game consecutively
                    // we probably need a cleanup mechanism at some point
                    SendDict[clientId] = new SendStream();
                }

                if (SendDict[clientId].IsEmpty)
                {
                    SendDict[clientId].IsEmpty = false;
                    SendDict[clientId].Channel = queueItem.channel;

                    SendDict[clientId].Writer.WriteBit(false); // Encrypted
                    SendDict[clientId].Writer.WriteBit(false); // Authenticated

                    switch (queueItem.queueItemType)
                    {
                        // 6 bits are used for the message type, which is an MLAPIConstants
                        case RpcQueueContainer.QueueItemType.ServerRpc:
                            SendDict[clientId].Writer.WriteBits(MLAPIConstants.MLAPI_SERVER_RPC, 6); // MessageType
                            break;
                        case RpcQueueContainer.QueueItemType.ClientRpc:
                            SendDict[clientId].Writer.WriteBits(MLAPIConstants.MLAPI_CLIENT_RPC, 6); // MessageType
                            break;
                    }
                }

                // write the amounts of bytes that are coming up
                PushLength(queueItem.messageData.Count, ref SendDict[clientId].Writer);

                // write the message to send
                SendDict[clientId].Writer.WriteBytes(queueItem.messageData.Array, queueItem.messageData.Count, queueItem.messageData.Offset);

                ProfilerStatManager.bytesSent.Record(queueItem.messageData.Count);
                ProfilerStatManager.rpcsSent.Record();
                PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent.ToString(), queueItem.messageData.Count);
                PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent.ToString());
            }
        }

        public delegate void SendCallbackType(ulong clientId, SendStream messageStream);
        public delegate void ReceiveCallbackType(BitStream messageStream, RpcQueueContainer.QueueItemType messageType, ulong clientId, float receiveTime);

        /// <summary>
        /// SendItems
        /// Send any batch of RPC that are of length above threshold
        /// </summary>
        /// <param name="thresholdBytes"> the threshold in bytes</param>
        /// <param name="sendCallback"> the function to call for sending the batch</param>
        public void SendItems(int thresholdBytes, SendCallbackType sendCallback)
        {
            foreach (KeyValuePair<ulong, SendStream> entry in SendDict)
            {
                if (!entry.Value.IsEmpty)
                {
                    // read the queued message
                    int length = (int)SendDict[entry.Key].Stream.Length;

                    if (length >= thresholdBytes)
                    {
                        sendCallback(entry.Key, entry.Value);
                        // clear the batch that was sent from the SendDict
                        entry.Value.Stream.SetLength(0);
                        entry.Value.Stream.Position = 0;
                        entry.Value.IsEmpty = true;
                        ProfilerStatManager.rpcBatchesSent.Record();
                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCBatchesSent.ToString());
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
        public void ReceiveItems(in BitStream messageStream, ReceiveCallbackType receiveCallback, RpcQueueContainer.QueueItemType messageType, ulong clientId, float receiveTime)
        {
            using (var copy = PooledBitStream.Get())
            {
                do
                {
                    // read the length of the next RPC
                    int rpcSize = PopLength(messageStream);
                    if (rpcSize < 0)
                    {
                        // abort if there's an error reading lengths
                        return;
                    }

                    // copy what comes after current stream position
                    long position = messageStream.Position;
                    copy.SetLength(rpcSize);
                    copy.Position = 0;
                    Buffer.BlockCopy(messageStream.GetBuffer(), (int)position, copy.GetBuffer(), 0, rpcSize);

                    receiveCallback(copy, messageType, clientId, receiveTime);

                    // seek over the RPC
                    // RPCReceiveQueueItem peeks at content, it doesn't advance
                    messageStream.Seek(rpcSize, SeekOrigin.Current);
                } while (messageStream.Position < messageStream.Length);
            }
        }
    }
}
