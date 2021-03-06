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
        private static ProfilerMarker s_ProcessReceiveQueue = new ProfilerMarker($"{nameof(RpcQueueProcessor)}.{nameof(ProcessReceiveQueue)}");
        private static ProfilerMarker s_ProcessSendQueue = new ProfilerMarker($"{nameof(RpcQueueProcessor)}.{nameof(ProcessSendQueue)}");
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
            bool advanceFrameHistory = false;
            var rpcQueueContainer = NetworkManager.Singleton.RpcQueueContainer;
            if (rpcQueueContainer != null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_ProcessReceiveQueue.Begin();
#endif
                var currentFrame = rpcQueueContainer.GetQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Inbound, currentStage);
                var nextFrame = rpcQueueContainer.GetQueueHistoryFrame(RpcQueueHistoryFrame.QueueFrameType.Inbound, currentStage, true);
                if (nextFrame.IsDirty && nextFrame.HasLoopbackData)
                {
                    advanceFrameHistory = true;
                }

                if (currentFrame != null && currentFrame.IsDirty)
                {
                    var currentQueueItem = currentFrame.GetFirstQueueItem();
                    while (currentQueueItem.QueueItemType != RpcQueueContainer.QueueItemType.None)
                    {
                        advanceFrameHistory = true;

                        if (rpcQueueContainer.IsTesting())
                        {
                            Debug.Log($"RPC invoked during the {currentStage} update stage.");
                        }

                        NetworkManager.InvokeRpc(currentQueueItem);
                        ProfilerStatManager.RpcsQueueProc.Record();
                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCQueueProcessed);
                        currentQueueItem = currentFrame.GetNextQueueItem();
                    }

                    //We call this to dispose of the shared stream writer and stream
                    currentFrame.CloseQueue();
                }

                if (advanceFrameHistory)
                {
                    rpcQueueContainer.AdvanceFrameHistory(RpcQueueHistoryFrame.QueueFrameType.Inbound);
                }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_ProcessReceiveQueue.End();
#endif
            }
        }

        /// <summary>
        /// ProcessSendQueue
        /// Called to send both performance RPC and internal messages and then flush the outbound queue
        /// </summary>
        public void ProcessSendQueue()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_ProcessSendQueue.Begin();
#endif

            RpcQueueSendAndFlush();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_ProcessSendQueue.End();
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
                var PoolStream = queueItem.NetworkBuffer;
                if (NetworkManager.Singleton.IsListening)
                {
                    switch (queueItem.QueueItemType)
                    {
                        case RpcQueueContainer.QueueItemType.CreateObject:
                        {
                            foreach (ulong clientId in queueItem.ClientNetworkIds)
                            {
                                InternalMessageSender.Send(clientId, NetworkConstants.ADD_OBJECT, queueItem.NetworkChannel, PoolStream);
                            }

                            PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.ClientNetworkIds.Length);
                            ProfilerStatManager.RpcsSent.Record(queueItem.ClientNetworkIds.Length);
                            break;
                        }
                        case RpcQueueContainer.QueueItemType.DestroyObject:
                        {
                            foreach (ulong clientId in queueItem.ClientNetworkIds)
                            {
                                InternalMessageSender.Send(clientId, NetworkConstants.DESTROY_OBJECT, queueItem.NetworkChannel, PoolStream);
                            }

                            PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.ClientNetworkIds.Length);
                            ProfilerStatManager.RpcsSent.Record(queueItem.ClientNetworkIds.Length);
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
            var advanceFrameHistory = false;
            var rpcQueueContainer = NetworkManager.Singleton.RpcQueueContainer;
            if (rpcQueueContainer != null)
            {
                var currentFrame = rpcQueueContainer.GetCurrentFrame(RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
                if (currentFrame != null)
                {
                    var currentQueueItem = currentFrame.GetFirstQueueItem();
                    while (currentQueueItem.QueueItemType != RpcQueueContainer.QueueItemType.None)
                    {
                        advanceFrameHistory = true;
                        if (rpcQueueContainer.IsUsingBatching())
                        {
                            m_RpcBatcher.QueueItem(currentQueueItem);

                            m_RpcBatcher.SendItems(k_BatchThreshold, SendCallback);
                        }
                        else
                        {
                            SendFrameQueueItem(currentQueueItem);
                        }

                        currentQueueItem = currentFrame.GetNextQueueItem();
                    }

                    //If the size is < m_BatchThreshold then just send the messages
                    if (advanceFrameHistory && rpcQueueContainer.IsUsingBatching())
                    {
                        m_RpcBatcher.SendItems(0, SendCallback);
                    }
                }

                //If we processed any RPCs, then advance to the next frame
                if (advanceFrameHistory)
                {
                    rpcQueueContainer.AdvanceFrameHistory(RpcQueueHistoryFrame.QueueFrameType.Outbound);
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
            var length = (int)sendStream.Buffer.Length;
            var bytes = sendStream.Buffer.GetBuffer();
            var sendBuffer = new ArraySegment<byte>(bytes, 0, length);

            NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, sendBuffer, sendStream.NetworkChannel);
        }

        /// <summary>
        /// SendFrameQueueItem
        /// Sends the RPC Queue Item to the specified destination
        /// </summary>
        /// <param name="queueItem">Information on what to send</param>
        private void SendFrameQueueItem(RpcFrameQueueItem queueItem)
        {
            switch (queueItem.QueueItemType)
            {
                case RpcQueueContainer.QueueItemType.ServerRpc:
                {
                    NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(queueItem.NetworkId, queueItem.MessageData, queueItem.NetworkChannel);

                    //For each packet sent, we want to record how much data we have sent

                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)queueItem.StreamSize);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent);
                    ProfilerStatManager.BytesSent.Record((int)queueItem.StreamSize);
                    ProfilerStatManager.RpcsSent.Record();
                    break;
                }
                case RpcQueueContainer.QueueItemType.ClientRpc:
                {
                    foreach (ulong clientid in queueItem.ClientNetworkIds)
                    {
                        NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(clientid, queueItem.MessageData, queueItem.NetworkChannel);

                        //For each packet sent, we want to record how much data we have sent
                        PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)queueItem.StreamSize);
                        ProfilerStatManager.BytesSent.Record((int)queueItem.StreamSize);
                    }

                    //For each client we send to, we want to record how many RPCs we have sent
                    PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.ClientNetworkIds.Length);
                    ProfilerStatManager.RpcsSent.Record(queueItem.ClientNetworkIds.Length);

                    break;
                }
            }
        }
    }
}