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

        // Batcher object used to manage the RPC batching on the send side
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
                                batcher.QueueItem(currentQueueItem);
                                currentQueueItem = CurrentFrame.GetNextQueueItem();

                                batcher.SendItems(BatchThreshold, SendCallback); // send anything already above the batching threshold
                            }
                            batcher.SendItems(0, SendCallback); // send the remaining  batches
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
        /// SendCallback
        /// This is the callback from the batcher when it need to send a batch
        ///
        /// </summary>
        /// <param name="clientId"> clientId to send to</param>
        /// <param name="sendStream"> the stream to send</param>
        private static void SendCallback(ulong clientId, MLAPI.BatchUtil.SendStream sendStream)
        {
            using PooledBitWriter writer = sendStream.Writer;
            int length = (int)writer.GetStream().Length;
            Byte[] bytes = ((MLAPI.Serialization.BitStream)writer.GetStream()).GetBuffer();

            ArraySegment<byte> sendBuffer = new ArraySegment<byte>(bytes, 0, length);

            NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, sendBuffer,
                string.IsNullOrEmpty(sendStream.Item.Channel) ? "MLAPI_DEFAULT_MESSAGE" : sendStream.Item.Channel);
        }

        public RPCQueueProcessing(RPCQueueManager rpcqueuemanager)
        {
            rpcQueueManager = rpcqueuemanager;
        }
    }
}
