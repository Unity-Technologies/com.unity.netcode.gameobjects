using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Profiling;
using MLAPI.Configuration;
using MLAPI.Messaging;
using MLAPI.Profiling;
using MLAPI.Serialization.Pooled;

namespace MLAPI
{
    internal class SendStream
    {
        public FrameQueueItem Item;
        public PooledBitWriter Writer;
        public PooledBitStream Stream = new PooledBitStream();
    }

    /// <summary>
    /// RPCQueueProcessing
    /// Handles processing of RPCQueues
    /// Inbound to invocation
    /// Outbound to send
    /// </summary>
    internal class RPCQueueProcessing
    {

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        static ProfilerMarker s_MLAPIRPCQueueProcess = new ProfilerMarker("MLAPIRPCQueueProcess");
        static ProfilerMarker s_MLAPIRPCQueueSend = new ProfilerMarker("MLAPIRPCQueueSend");
#endif

        //NSS-TODO:  Need to determine how we want to handle all other MLAPI send types
        //Temporary place to keep internal MLAPI messages
        readonly List<FrameQueueItem> InternalMLAPISendQueue = new List<FrameQueueItem>();

        RPCQueueManager rpcQueueManager;

        // Stores the stream of batched RPC to send to each client, by ClientId
        private Dictionary<ulong, SendStream> SendDict = new Dictionary<ulong, SendStream>();
        private BatchUtil batcher = new BatchUtil();

        private int BatchThreshold = 1000;

        /// <summary>
        /// ProcessReceiveQueue
        /// Public facing interface method to start processing all RPCs in the current inbound frame
        /// </summary>
        public void ProcessReceiveQueue()
        {
            RPCReceiveQueueProcessFlush();
        }

        /// <summary>
        /// RCPQueueReeiveAndFlush
        /// Parses through all incoming RPCs in the active RPC History Frame (RPCQueueManager)
        /// </summary>
        private void RPCReceiveQueueProcessFlush()
        {
            bool AdvanceFrameHistory = false;
            RPCQueueManager rpcQueueManager = NetworkingManager.Singleton.GetRPCQueueManager();
            if(rpcQueueManager != null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_MLAPIRPCQueueProcess.Begin();
#endif
                try
                {

                    QueueHistoryFrame CurrentFrame = rpcQueueManager.GetCurrentFrame(QueueHistoryFrame.QueueFrameType.Inbound);
                    if(CurrentFrame != null)
                    {
                        FrameQueueItem currentQueueItem = CurrentFrame.GetFirstQueueItem();
                        while(currentQueueItem.QueueItemType != RPCQueueManager.QueueItemType.NONE)
                        {
                            AdvanceFrameHistory = true;
                            if(rpcQueueManager.IsLoopBack())
                            {
                                currentQueueItem.ItemStream.Position = 1;
                            }

                            NetworkingManager.Singleton.InvokeRPC(currentQueueItem);
                            ProfilerStatManager.rpcsQueueProc.Record();
                            currentQueueItem = CurrentFrame.GetNextQueueItem();
                        }
                        //We call this to dispose of the shared stream writer and stream
                        CurrentFrame.CloseQueue();
                    }

                }
                catch(Exception ex)
                {
                    Debug.LogError(ex);
                }

                if(AdvanceFrameHistory)
                {
                    rpcQueueManager.AdvanceFrameHistory(QueueHistoryFrame.QueueFrameType.Inbound);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_MLAPIRPCQueueProcess.End();
#endif
        }

        /// <summary>
        /// ProcessSendQueue
        /// Called to send both performance RPC and internal messages and then flush the outbound queue
        /// </summary>
        public void ProcessSendQueue()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
           s_MLAPIRPCQueueSend.Begin();
#endif

            RPCQueueSendAndFlush();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_MLAPIRPCQueueSend.End();
#endif
            InternalMessagesSendAndFlush();
        }

        /// <summary>
        ///  QueueInternalMLAPICommand
        ///  Added this as an example of how to add internal messages to the outbound send queue
        /// </summary>
        /// <param name="queueItem">message queue item to add<</param>
        public void QueueInternalMLAPICommand(FrameQueueItem queueItem)
        {
            InternalMLAPISendQueue.Add(queueItem);
        }

        /// <summary>
        /// Generic Sending Method for Internal Messages
        /// TODO: Will need to open this up for discussion, but we will want to determine if this is how we want internal MLAPI command
        /// messages to be sent.  We might want specific commands to occur during specific network update regions (see NetworkUpdate
        /// </summary>
        public void InternalMessagesSendAndFlush()
        {
            foreach (FrameQueueItem queueItem in InternalMLAPISendQueue)
            {
                PooledBitStream PoolStream = (PooledBitStream)queueItem.ItemStream;
                switch(queueItem.QueueItemType)
                {
                    case RPCQueueManager.QueueItemType.CreateObject:
                        {
                            foreach(ulong clientId in queueItem.ClientIds)
                            {
                                InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_ADD_OBJECT,queueItem.Channel,PoolStream, queueItem.SendFlags);
                            }
                            ProfilerStatManager.rpcsSent.Record(queueItem.ClientIds?.Count ?? NetworkingManager.Singleton.ConnectedClientsList.Count);
                            break;
                        }
                    case RPCQueueManager.QueueItemType.DestroyObject:
                        {
                            foreach(ulong clientId in queueItem.ClientIds)
                            {
                                InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_DESTROY_OBJECT, queueItem.Channel, PoolStream, queueItem.SendFlags);
                            }
                            ProfilerStatManager.rpcsSent.Record(queueItem.ClientIds?.Count ?? NetworkingManager.Singleton.ConnectedClientsList.Count);
                            break;
                        }
                }
                PoolStream.Dispose();
            }
            InternalMLAPISendQueue.Clear();
        }

        /// <summary>
        /// RPCQueueSendAndFlush
        /// Sends all RPC queue items in the current outbound frame
        /// </summary>
        private void RPCQueueSendAndFlush()
        {
            bool AdvanceFrameHistory = false;
            RPCQueueManager rpcQueueManager = NetworkingManager.Singleton.GetRPCQueueManager();
            if(rpcQueueManager != null)
            {
                try
                {
                    QueueHistoryFrame CurrentFrame = rpcQueueManager.GetCurrentFrame(QueueHistoryFrame.QueueFrameType.Outbound);
                    //If loopback is enabled
                    if(rpcQueueManager.IsLoopBack())
                    {
                        //Migrate the outbound buffer to the inbound buffer
                        rpcQueueManager.LoopbackSendFrame();
                        AdvanceFrameHistory = true;
                    }
                    else
                    {
                        if(CurrentFrame != null)
                        {
                            FrameQueueItem currentQueueItem = CurrentFrame.GetFirstQueueItem();
                            while(currentQueueItem.QueueItemType != RPCQueueManager.QueueItemType.NONE)
                            {
                                AdvanceFrameHistory = true;
                                QueueItem(currentQueueItem);
                                currentQueueItem = CurrentFrame.GetNextQueueItem();

                                SendItems(BatchThreshold); // send anything already above the batching threshold
                            }
                            SendItems(0); // send the remaining  batches
                        }
                    }
                }
                catch(Exception ex)
                {
                    Debug.LogError(ex);
                }

                if(AdvanceFrameHistory)
                {
                    rpcQueueManager.AdvanceFrameHistory(QueueHistoryFrame.QueueFrameType.Outbound);
                }
            }
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
        /// SendItems
        /// Send any batch of RPC that are of length above threshold
        /// </summary>
        /// <param name="threshold">the threshold in bytes</param>
        private void SendItems(int threshold)
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

        /// <summary>
        /// QueueItem
        /// Add a FrameQueueItem to be sent
        /// </summary>queueItem
        /// <param name="item">the threshold in bytes</param>
        private void QueueItem(FrameQueueItem item)
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
                batcher.PushLength(item.MessageData.Count, ref SendDict[clientId].Writer);

                // write the message to send
                // todo: is there a faster alternative to .ToArray()
                SendDict[clientId].Writer.WriteBytes(item.MessageData.ToArray(), item.MessageData.Count);

                ProfilerStatManager.bytesSent.Record((int)item.MessageData.Count);
                ProfilerStatManager.rpcsSent.Record();
            }
        }

        public RPCQueueProcessing(RPCQueueManager rpcqueuemanager)
        {
            rpcQueueManager = rpcqueuemanager;
        }
    }
}
