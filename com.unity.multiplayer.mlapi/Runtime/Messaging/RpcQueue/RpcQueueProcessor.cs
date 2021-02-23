using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using MLAPI.Configuration;
using MLAPI.Profiling;

namespace MLAPI.Messaging
{
    /// <summary>
    /// RpcQueueProcessing
    /// Handles processing of RPCQueues
    /// Inbound to invocation
    /// Outbound to send
    /// </summary>
    internal class RpcQueueProcessor
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        static ProfilerMarker s_MLAPIRPCQueueProcess = new ProfilerMarker("MLAPIRPCQueueProcess");
        static ProfilerMarker s_MLAPIRPCQueueSend = new ProfilerMarker("MLAPIRPCQueueSend");
#endif

        // Batcher object used to manage the RPC batching on the send side
        private readonly RpcBatcher m_RpcBatcher = new RpcBatcher();
        private const int k_BatchThreshold = 512;

        //NSS-TODO: Need to determine how we want to handle all other MLAPI send types
        //Temporary place to keep internal MLAPI messages
        private readonly List<RpcFrameQueueItem> m_InternalMLAPISendQueue = new List<RpcFrameQueueItem>();

        /// <summary>
        /// ProcessReceiveQueue
        /// Public facing interface method to start processing all RPCs in the current inbound frame
        /// </summary>
        public void ProcessReceiveQueue(NetworkUpdateStage currentStage)
        {
            bool AdvanceFrameHistory = false;
            var rpcQueueContainer = NetworkingManager.Singleton.rpcQueueContainer;
            if (rpcQueueContainer != null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_MLAPIRPCQueueProcess.Begin();
#endif
                var CurrentFrame = rpcQueueContainer.GetQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound, currentStage);
                var NextFrame = rpcQueueContainer.GetQueueHistoryFrame(QueueHistoryFrame.QueueFrameType.Inbound, currentStage, true);
                if (NextFrame.isDirty && NextFrame.hasLoopbackData)
                {
                    AdvanceFrameHistory = true;
                }

                if (CurrentFrame != null && CurrentFrame.isDirty)
                {
                    var currentQueueItem = CurrentFrame.GetFirstQueueItem();
                    while (currentQueueItem.queueItemType != RpcQueueContainer.QueueItemType.None)
                    {
                        AdvanceFrameHistory = true;

                        if (rpcQueueContainer.IsTesting())
                        {
                            Debug.Log("RPC invoked during the " + currentStage.ToString() + " update stage.");
                        }

                        NetworkingManager.InvokeRpc(currentQueueItem);
                        ProfilerStatManager.rpcsQueueProc.Record();
                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCQueueProcessed.ToString());
                        currentQueueItem = CurrentFrame.GetNextQueueItem();
                    }

                    //We call this to dispose of the shared stream writer and stream
                    CurrentFrame.CloseQueue();
                }

                if (AdvanceFrameHistory)
                {
                    rpcQueueContainer.AdvanceFrameHistory(QueueHistoryFrame.QueueFrameType.Inbound);
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
        public void QueueInternalMLAPICommand(RpcFrameQueueItem queueItem)
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
            foreach (RpcFrameQueueItem queueItem in m_InternalMLAPISendQueue)
            {

                var PoolStream = queueItem.itemStream;

                if(NetworkingManager.Singleton.IsListening)
                {
                    switch (queueItem.queueItemType)
                    {
                        case RpcQueueContainer.QueueItemType.CreateObject:
                        {
                            foreach (ulong clientId in queueItem.clientIds)
                            {
                                InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_ADD_OBJECT, queueItem.channel, PoolStream, queueItem.sendFlags);
                            }

                            PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent.ToString(), queueItem.clientIds.Length);
                            ProfilerStatManager.rpcsSent.Record(queueItem.clientIds.Length);
                            break;
                        }
                        case RpcQueueContainer.QueueItemType.DestroyObject:
                        {
                            foreach (ulong clientId in queueItem.clientIds)
                            {
                                InternalMessageSender.Send(clientId, MLAPIConstants.MLAPI_DESTROY_OBJECT, queueItem.channel, PoolStream, queueItem.sendFlags);
                            }

                            PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent.ToString(), queueItem.clientIds.Length);
                            ProfilerStatManager.rpcsSent.Record(queueItem.clientIds.Length);
                            break;
                        }
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
            var AdvanceFrameHistory = false;
            var rpcQueueContainer = NetworkingManager.Singleton.rpcQueueContainer;
            if (rpcQueueContainer != null)
            {
                var CurrentFrame = rpcQueueContainer.GetCurrentFrame(QueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);

                if (CurrentFrame != null)
                {
                    var currentQueueItem = CurrentFrame.GetFirstQueueItem();
                    while (currentQueueItem.queueItemType != RpcQueueContainer.QueueItemType.None)
                    {
                        AdvanceFrameHistory = true;
                        if (rpcQueueContainer.IsUsingBatching())
                        {
                            m_RpcBatcher.QueueItem(currentQueueItem);

                            m_RpcBatcher.SendItems(k_BatchThreshold, SendCallback);
                        }
                        else
                        {
                            SendFrameQueueItem(currentQueueItem);
                        }
                        currentQueueItem = CurrentFrame.GetNextQueueItem();
                    }

                    //If the size is < m_BatchThreshold then just send the messages
                    if (AdvanceFrameHistory && rpcQueueContainer.IsUsingBatching())
                    {
                        m_RpcBatcher.SendItems(0, SendCallback);
                    }
                }

                //If we processed any RPCs, then advance to the next frame
                if (AdvanceFrameHistory)
                {
                    rpcQueueContainer.AdvanceFrameHistory(QueueHistoryFrame.QueueFrameType.Outbound);
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
        private static void SendCallback(ulong clientId, RpcBatcher.SendStream sendStream)
        {
            var length = (int)sendStream.Stream.Length;
            var bytes = sendStream.Stream.GetBuffer();
            ArraySegment<byte> sendBuffer = new ArraySegment<byte>(bytes, 0, length);

            NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, sendBuffer, sendStream.Channel);
        }

        /// <summary>
        /// SendFrameQueueItem
        /// Sends the RPC Queue Item to the specified destination
        /// </summary>
        /// <param name="queueItem">Information on what to send</param>
        private void SendFrameQueueItem(RpcFrameQueueItem queueItem)
        {
            switch (queueItem.queueItemType)
            {
                case RpcQueueContainer.QueueItemType.ServerRpc:
                {
                    NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(queueItem.networkId, queueItem.messageData, queueItem.channel);

                    //For each packet sent, we want to record how much data we have sent

                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent.ToString(), (int)queueItem.streamSize);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent.ToString());
                    ProfilerStatManager.bytesSent.Record((int)queueItem.streamSize);
                    ProfilerStatManager.rpcsSent.Record();
                    break;
                }
                case RpcQueueContainer.QueueItemType.ClientRpc:
                {
                    foreach (ulong clientid in queueItem.clientIds)
                    {
                        NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientid, queueItem.messageData, queueItem.channel);

                        //For each packet sent, we want to record how much data we have sent
                        PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent.ToString(), (int)queueItem.streamSize);
                        ProfilerStatManager.bytesSent.Record((int)queueItem.streamSize);
                    }

                    //For each client we send to, we want to record how many RPCs we have sent
                    PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent.ToString(), queueItem.clientIds.Length);
                    ProfilerStatManager.rpcsSent.Record(queueItem.clientIds.Length);

                    break;
                }
            }
        }
    }
}
