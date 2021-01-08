using System;
using System.Collections.Generic;
using Unity.Profiling;
using MLAPI.Configuration;
using MLAPI.Messaging;
using MLAPI.Profiling;


namespace MLAPI
{
    /// <summary>
    /// RpcQueueProcessing
    /// Handles processing of RPCQueues
    /// Inbound to invocation
    /// Outbound to send
    /// </summary>
    internal class RpcQueueProcessing
    {

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        static ProfilerMarker s_MLAPIRPCQueueProcess = new ProfilerMarker("MLAPIRPCQueueProcess");
        static ProfilerMarker s_MLAPIRPCQueueSend = new ProfilerMarker("MLAPIRPCQueueSend");
#endif
        // Batcher object used to manage the RPC batching on the send side
        private MessageBatcher m_batcher = new MessageBatcher();
        private int m_BatchThreshold = 10;

        //NSS-TODO: Need to determine how we want to handle all other MLAPI send types
        //Temporary place to keep internal MLAPI messages
        private readonly List<FrameQueueItem> m_InternalMLAPISendQueue = new List<FrameQueueItem>();

        /// <summary>
        /// ProcessReceiveQueue
        /// Public facing interface method to start processing all RPCs in the current inbound frame
        /// </summary>
        public void ProcessReceiveQueue(NetworkUpdateManager.NetworkUpdateStages currentStage)
        {
            bool AdvanceFrameHistory = false;
            var rpcQueueManager = NetworkingManager.Singleton.RpcQueueManager;
            if (rpcQueueManager != null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_MLAPIRPCQueueProcess.Begin();
#endif
                var CurrentFrame = rpcQueueManager.GetCurrentFrame(QueueHistoryFrame.QueueFrameType.Inbound,currentStage);
                if (CurrentFrame != null)
                {
                    var currentQueueItem = CurrentFrame.GetFirstQueueItem();
                    while (currentQueueItem.queueItemType != RpcQueueContainer.QueueItemType.None)
                    {
                        AdvanceFrameHistory = true;
                        if (rpcQueueManager.IsLoopBack())
                        {
                            currentQueueItem.itemStream.Position = 1;
                        }
                        if(rpcQueueManager.IsTesting())
                        {
                            Debug.Log("RPC invoked during the " + currentStage.ToString() + " update stage.");
                        }
                        NetworkingManager.InvokeRpc(currentQueueItem);
                        ProfilerStatManager.rpcsQueueProc.Record();
                        currentQueueItem = CurrentFrame.GetNextQueueItem();
                    }

                    //We call this to dispose of the shared stream writer and stream
                    CurrentFrame.CloseQueue();
                }

                if (AdvanceFrameHistory)
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
            InternalMessagesSendAndFlush();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_MLAPIRPCQueueSend.Begin();
#endif
            RPCQueueSendAndFlush();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_MLAPIRPCQueueSend.End();
#endif

        }

        /// <summary>
        ///  QueueInternalMLAPICommand
        ///  Added this as an example of how to add internal messages to the outbound send queue
        /// </summary>
        /// <param name="queueItem">message queue item to add<</param>
        public void QueueInternalMLAPICommand(FrameQueueItem queueItem)
        {
            m_InternalMLAPISendQueue.Add(queueItem);
        }

        /// <summary>
        /// Generic Sending Method for Internal Messages
        /// TODO: Will need to open this up for discussion, but we will want to determine if this is how we want internal MLAPI command
        /// messages to be sent.  We might want specific commands to occur during specific network update regions (see NetworkUpdate
        /// </summary>
        public void InternalMessagesSendAndFlush()
        {
            foreach (FrameQueueItem queueItem in m_InternalMLAPISendQueue)
            {
                var PoolStream = queueItem.itemStream;
                switch (queueItem.queueItemType)
                {
                    case RpcQueueContainer.QueueItemType.CreateObject:
                    {
                        foreach (ulong clientId in queueItem.clientIds)
                        {
                            InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_ADD_OBJECT, queueItem.channel, PoolStream, queueItem.sendFlags);
                        }

                        ProfilerStatManager.rpcsSent.Record(queueItem.clientIds.Length);
                        break;
                    }
                    case RpcQueueContainer.QueueItemType.DestroyObject:
                    {
                        foreach (ulong clientId in queueItem.clientIds)
                        {
                            InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_DESTROY_OBJECT, queueItem.channel, PoolStream, queueItem.sendFlags);
                        }

                        ProfilerStatManager.rpcsSent.Record(queueItem.clientIds.Length);
                        break;
                    }
                }

                PoolStream.Dispose();
            }

            m_InternalMLAPISendQueue.Clear();
        }

        /// <summary>
        /// RPCQueueSendAndFlush
        /// Sends all RPC queue items in the current outbound frame
        /// </summary>
        private void RPCQueueSendAndFlush()
        {
            bool AdvanceFrameHistory = false;
            var rpcQueueManager = NetworkingManager.Singleton.RpcQueueManager;
            if (rpcQueueManager != null)
            {
                var CurrentFrame = rpcQueueManager.GetCurrentFrame(QueueHistoryFrame.QueueFrameType.Outbound,NetworkUpdateManager.NetworkUpdateStages.LATEUPDATE);
                //If loopback is enabled
                if (rpcQueueManager.IsLoopBack())
                {
                    //Migrate the outbound buffer to the inbound buffer
                    rpcQueueManager.LoopbackSendFrame();
                    AdvanceFrameHistory = true;
                }
                else
                {
                    if (CurrentFrame != null)
                    {
                        var currentQueueItem = CurrentFrame.GetFirstQueueItem();
                        while (currentQueueItem.queueItemType != RpcQueueContainer.QueueItemType.None)
                        {
                            AdvanceFrameHistory = true;
                            m_batcher.QueueItem(currentQueueItem);
                            currentQueueItem = CurrentFrame.GetNextQueueItem();

                            m_batcher.SendItems(m_BatchThreshold, SendCallback); // send anything already above the batching threshold
                        }
                        m_batcher.SendItems(0, SendCallback); // send the remaining  batches
                    }
                }

                if (AdvanceFrameHistory)
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
        private static void SendCallback(ulong clientId, MLAPI.MessageBatcher.SendStream sendStream)
        {
            var length = (int)sendStream.Stream.Length;
            var bytes = sendStream.Stream.GetBuffer();
            ArraySegment<byte> sendBuffer = new ArraySegment<byte>(bytes, 0, length);

            NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, sendBuffer, string.IsNullOrEmpty(sendStream.Item.channel) ? "MLAPI_DEFAULT_MESSAGE" : sendStream.Item.channel);
        }
    }
}
