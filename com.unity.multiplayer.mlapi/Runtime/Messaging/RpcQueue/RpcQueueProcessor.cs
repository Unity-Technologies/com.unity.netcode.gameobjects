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
    /// Handles processing of RpcQueues
    /// Inbound to invocation
    /// Outbound to send
    /// </summary>
    internal class RpcQueueProcessor
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        static ProfilerMarker s_RpcQueueProcess = new ProfilerMarker("RpcQueueProcess");
        static ProfilerMarker s_RpcQueueSend = new ProfilerMarker("RpcQueueSend");
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
            var rpcQueueContainer = NetworkManager.Singleton.rpcQueueContainer;
            if (rpcQueueContainer != null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_RpcQueueProcess.Begin();
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

                        NetworkManager.InvokeRpc(currentQueueItem);
                        ProfilerStatManager.rpcsQueueProc.Record();
                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCQueueProcessed);
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
            s_RpcQueueProcess.End();
#endif
        }

        /// <summary>
        /// ProcessSendQueue
        /// Called to send both performance RPC and internal messages and then flush the outbound queue
        /// </summary>
        public void ProcessSendQueue()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_RpcQueueSend.Begin();
#endif

            RpcQueueSendAndFlush();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_RpcQueueSend.End();
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

                if(NetworkManager.Singleton.IsListening)
                {
                    switch (queueItem.queueItemType)
                    {
                        case RpcQueueContainer.QueueItemType.CreateObject:
                        {
                            foreach (ulong clientId in queueItem.clientIds)
                            {
                                InternalMessageSender.Send(clientId, NetworkConstants.k_ADD_OBJECT, queueItem.networkChannel, PoolStream);
                            }

                            PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.clientIds.Length);
                            ProfilerStatManager.rpcsSent.Record(queueItem.clientIds.Length);
                            break;
                        }
                        case RpcQueueContainer.QueueItemType.DestroyObject:
                        {
                            foreach (ulong clientId in queueItem.clientIds)
                            {
                                InternalMessageSender.Send(clientId, NetworkConstants.k_DESTROY_OBJECT, queueItem.networkChannel, PoolStream);
                            }

                            PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.clientIds.Length);
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
        private void RpcQueueSendAndFlush()
        {
            var AdvanceFrameHistory = false;
            var rpcQueueContainer = NetworkManager.Singleton.rpcQueueContainer;
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

            NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, sendBuffer, sendStream.NetworkChannel);
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
                    NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(queueItem.networkId, queueItem.messageData, queueItem.networkChannel);

                    //For each packet sent, we want to record how much data we have sent

                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)queueItem.streamSize);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent);
                    ProfilerStatManager.bytesSent.Record((int)queueItem.streamSize);
                    ProfilerStatManager.rpcsSent.Record();
                    break;
                }
                case RpcQueueContainer.QueueItemType.ClientRpc:
                {
                    foreach (ulong clientid in queueItem.clientIds)
                    {
                        NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(clientid, queueItem.messageData, queueItem.networkChannel);

                        //For each packet sent, we want to record how much data we have sent
                        PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)queueItem.streamSize);
                        ProfilerStatManager.bytesSent.Record((int)queueItem.streamSize);
                    }

                    //For each client we send to, we want to record how many RPCs we have sent
                    PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.clientIds.Length);
                    ProfilerStatManager.rpcsSent.Record(queueItem.clientIds.Length);

                    break;
                }
            }
        }
    }
}
