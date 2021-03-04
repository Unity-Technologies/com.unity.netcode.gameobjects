using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using MLAPI.Configuration;
using MLAPI.Profiling;

namespace MLAPI.Messaging
{
    /// <summary>
    /// The inbound and outbound RPC queue processing and sending class
    /// Inbound: RPCs will be invoked based on their assigned network update stage
    /// Outbound: RPCs will be batched and sent or just sent depending upon whether batching is enabled or not
    /// Internal: Queues the add object and destroy object lower layer MLAPI messages
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

        //If a queue item lingers for longer than 60 seconds, then it can be removed (i.e. player failed to connect or the like)
        private const float k_MaximumInternalQueueItemLifeTime = 60;

        //NSS-TODO: Need to determine how we want to handle all other MLAPI send types
        //Temporary place to keep internal MLAPI messages
        private readonly List<RpcFrameQueueItem> m_InternalMLAPISendQueue = new List<RpcFrameQueueItem>();

        /// <summary>
        /// ProcessReceiveQueue
        /// Public facing interface method to start processing all RPCs in the current inbound frame
        /// </summary>
        internal void ProcessReceiveQueue(NetworkUpdateStage currentStage)
        {
            //In case there is no Singleton, then just exit
            if(!NetworkManager.Singleton)
            {
                return;
            }

            bool AdvanceFrameHistory = false;
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
                    AdvanceFrameHistory = true;
                }

                if (currentFrame != null && currentFrame.IsDirty)
                {
                    var currentQueueItem = currentFrame.GetFirstQueueItem();
                    while (currentQueueItem.QueueItemType != RpcQueueContainer.QueueItemType.None)
                    {
                        AdvanceFrameHistory = true;

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

                if (AdvanceFrameHistory)
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
        internal void ProcessSendQueue()
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
        ///  Used to queue lower layer MLAPI commands
        ///  Currently only used for spawning and destroying objects
        /// </summary>
        /// <param name="queueItem">message queue item to add<</param>
        internal void QueueInternalMLAPICommand(RpcFrameQueueItem queueItem)
        {
            m_InternalMLAPISendQueue.Add(queueItem);
        }

        /// <summary>
        /// Generic Sending Method for Internal Messages
        /// If a client is not finished loading the current scene, then the messages will be kept until
        /// the clients scene loading status is marked as completed.
        /// </summary>
        internal void InternalMessagesSendAndFlush()
        {
            if(NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
            {
                List<RpcFrameQueueItem> CompletedRpcQueueItems = new List<RpcFrameQueueItem>();
                foreach (RpcFrameQueueItem queueItem in m_InternalMLAPISendQueue)
                {
                    var clientIds = new List<ulong>(queueItem.ClientNetworkIds);
                    var PoolStream = queueItem.NetworkBuffer;
                    for(int i = 0; i < queueItem.ClientNetworkIds.Length; i++)
                    {
                        ulong clientId = queueItem.ClientNetworkIds[i];

                        //Clients always can send internal messages, but servers need to make sure clients are loaded.
                        if( NetworkManager.Singleton.IsClient || NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                        {
                            if(NetworkManager.Singleton.IsClient || NetworkManager.Singleton.ConnectedClients[clientId].IsClientDoneLoadingScene )
                            {
                                switch (queueItem.QueueItemType)
                                {
                                    case RpcQueueContainer.QueueItemType.CreateObject:
                                    {
                                        InternalMessageSender.Send(clientId, NetworkConstants.ADD_OBJECT, queueItem.NetworkChannel, PoolStream);
                                        clientIds.Remove(clientId);
                                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.ClientNetworkIds.Length);
                                        ProfilerStatManager.RpcsSent.Record(queueItem.ClientNetworkIds.Length);
                                        break;
                                    }
                                    case RpcQueueContainer.QueueItemType.DestroyObject:
                                    {
                                        InternalMessageSender.Send(clientId, NetworkConstants.DESTROY_OBJECT, queueItem.NetworkChannel, PoolStream);
                                        clientIds.Remove(clientId);
                                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.ClientNetworkIds.Length);
                                        ProfilerStatManager.RpcsSent.Record(queueItem.ClientNetworkIds.Length);
                                        break;
                                    }
                                }
                            }
                        }
                        //else
                        //{
                        //    clientIds.Remove(clientId);
                        //}
                    }

                    //If there are no more clients to send this item to =or= a queue item has been around for a minute then remove it from the outbound internal message queue
                    if(clientIds.Count == 0 || ((queueItem.QueueCreationTime + k_MaximumInternalQueueItemLifeTime) < Time.realtimeSinceStartup) )
                    {
                        CompletedRpcQueueItems.Add(queueItem);
                        PoolStream.Dispose();
                    }
                }
                foreach(RpcFrameQueueItem item in CompletedRpcQueueItems)
                {
                    m_InternalMLAPISendQueue.Remove(item);
                }
            }
        }

        /// <summary>
        /// If batching is disabled, then it will sends all RPC queue items in the current outbound frame
        /// If batching is enabled, then it will batch RPCs together based on their targets and send
        /// </summary>
        private void RpcQueueSendAndFlush()
        {
            //In case there is no Singleton, then just exit
            if(!NetworkManager.Singleton)
            {
                return;
            }

            var AdvanceFrameHistory = false;
            var rpcQueueContainer = NetworkManager.Singleton.RpcQueueContainer;
            if (rpcQueueContainer != null)
            {
                var currentFrame = rpcQueueContainer.GetCurrentFrame(RpcQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
                if (currentFrame != null)
                {
                    var currentQueueItem = currentFrame.GetFirstQueueItem();
                    while (currentQueueItem.QueueItemType != RpcQueueContainer.QueueItemType.None)
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

                        currentQueueItem = currentFrame.GetNextQueueItem();
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
                    rpcQueueContainer.AdvanceFrameHistory(RpcQueueHistoryFrame.QueueFrameType.Outbound);
                }
            }
        }


        /// <summary>
        /// The send callback is assigned to the batcher and is invoked when an RPC batch reaches its maximum size
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
        /// If batching is disabled, then this is what is invoked to send each RPC individually
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
