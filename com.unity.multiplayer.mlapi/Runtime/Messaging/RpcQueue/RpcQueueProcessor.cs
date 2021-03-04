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
        static ProfilerMarker s_RpcQueueProcess = new ProfilerMarker("RpcQueueProcess");
        static ProfilerMarker s_RpcQueueSend = new ProfilerMarker("RpcQueueSend");
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
        internal void ProcessSendQueue()
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
                    List<ulong> clientIds = new List<ulong>(queueItem.clientIds);
                    var PoolStream = queueItem.itemBuffer;
                    for(int i = 0; i < queueItem.clientIds.Length; i++)
                    {
                        ulong clientId = queueItem.clientIds[i];

                        //Clients always can send internal messages, but servers need to make sure clients are loaded.
                        if( NetworkManager.Singleton.IsClient || NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                        {
                            if(NetworkManager.Singleton.IsClient || NetworkManager.Singleton.ConnectedClients[clientId].IsClientDoneLoadingScene )
                            {
                                switch (queueItem.queueItemType)
                                {
                                    case RpcQueueContainer.QueueItemType.CreateObject:
                                    {
                                        InternalMessageSender.Send(clientId, NetworkConstants.ADD_OBJECT, queueItem.networkChannel, PoolStream);
                                        clientIds.Remove(clientId);
                                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.clientIds.Length);
                                        ProfilerStatManager.rpcsSent.Record(queueItem.clientIds.Length);
                                        break;
                                    }
                                    case RpcQueueContainer.QueueItemType.DestroyObject:
                                    {
                                        InternalMessageSender.Send(clientId, NetworkConstants.DESTROY_OBJECT, queueItem.networkChannel, PoolStream);
                                        clientIds.Remove(clientId);
                                        PerformanceDataManager.Increment(ProfilerConstants.NumberOfRPCsSent, queueItem.clientIds.Length);
                                        ProfilerStatManager.rpcsSent.Record(queueItem.clientIds.Length);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            clientIds.Remove(clientId);
                        }
                    }

                    //If there are no more clients to send this item to =or= a queue item has been around for a minute then remove it from the outbound internal message queue
                    if(clientIds.Count == 0 || ((queueItem.queueCreationTime + k_MaximumInternalQueueItemLifeTime) < Time.realtimeSinceStartup) )
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
        /// The send callback is assigned to the batcher and is invoked when an RPC batch reaches its maximum size
        /// </summary>
        /// <param name="clientId"> clientId to send to</param>
        /// <param name="sendStream"> the stream to send</param>
        private static void SendCallback(ulong clientId, RpcBatcher.SendStream sendStream)
        {
            var length = (int)sendStream.Buffer.Length;
            var bytes = sendStream.Buffer.GetBuffer();
            ArraySegment<byte> sendBuffer = new ArraySegment<byte>(bytes, 0, length);

            NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, sendBuffer, sendStream.NetworkChannel);
        }

        /// <summary>
        /// If batching is disabled, then this is what is invoked to send each RPC individually
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
