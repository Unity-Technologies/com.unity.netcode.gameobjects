using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// MessageQueueProcessing
    /// Handles processing of MessageQueues
    /// Inbound to invocation
    /// Outbound to send
    /// </summary>
    internal class MessageQueueProcessor
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static ProfilerMarker s_ProcessReceiveQueue = new ProfilerMarker($"{nameof(MessageQueueProcessor)}.{nameof(ProcessReceiveQueue)}");
        private static ProfilerMarker s_ProcessSendQueue = new ProfilerMarker($"{nameof(MessageQueueProcessor)}.{nameof(ProcessSendQueue)}");
#endif

        // Batcher object used to manage the message batching on the send side
        private readonly MessageBatcher m_MessageBatcher = new MessageBatcher();
        private const int k_BatchThreshold = 512;
        // Selected mostly arbitrarily... Better solution to come soon.
        private const int k_FragmentationThreshold = 1024;

        private bool m_AdvanceInboundFrameHistory = false;

        //The MessageQueueContainer that is associated with this MessageQueueProcessor
        private MessageQueueContainer m_MessageQueueContainer;

        private readonly NetworkManager m_NetworkManager;
        private readonly List<ulong> m_TargetIdBuffer = new List<ulong>();

        public void Shutdown()
        {
            m_MessageBatcher.Shutdown();
        }

        public void ProcessMessage(in MessageFrameItem item)
        {
            try
            {
                switch (item.MessageType)
                {
                    case MessageQueueContainer.MessageType.ClientRpc:
                    case MessageQueueContainer.MessageType.ServerRpc:
                        // Can rely on currentStage == the original updateStage in the buffer
                        // After all, that's the whole point of it being in the buffer.
                        m_NetworkManager.InvokeRpc(item, item.UpdateStage);
                        break;
                    case MessageQueueContainer.MessageType.UnnamedMessage:
                        m_NetworkManager.MessageHandler.HandleUnnamedMessage(item.NetworkId, item.NetworkBuffer);
                        break;
                    case MessageQueueContainer.MessageType.NamedMessage:
                        m_NetworkManager.MessageHandler.HandleNamedMessage(item.NetworkId, item.NetworkBuffer);
                        break;
                    case MessageQueueContainer.MessageType.SceneEvent:
                        m_NetworkManager.MessageHandler.HandleSceneEvent(item.NetworkId, item.NetworkBuffer);
                        break;

                    default:
                        NetworkLog.LogWarning($"Received unknown message {((int)item.MessageType).ToString()}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"A {item.MessageType} threw an exception while executing! Please check Unity logs for more information.");
                }
            }
        }

        /// <summary>
        /// ProcessReceiveQueue
        /// Public facing interface method to start processing all messages in the current inbound frame
        /// </summary>
        public void ProcessReceiveQueue(NetworkUpdateStage currentStage, bool isTesting)
        {
            if (!ReferenceEquals(m_MessageQueueContainer, null))
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_ProcessReceiveQueue.Begin();
#endif
                var currentFrame = m_MessageQueueContainer.GetQueueHistoryFrame(MessageQueueHistoryFrame.QueueFrameType.Inbound, currentStage);
                var nextFrame = m_MessageQueueContainer.GetQueueHistoryFrame(MessageQueueHistoryFrame.QueueFrameType.Inbound, currentStage, true);
                if (nextFrame.IsDirty && nextFrame.HasLoopbackData)
                {
                    m_AdvanceInboundFrameHistory = true;
                }

                if (currentFrame != null && currentFrame.IsDirty)
                {
                    var currentQueueItem = currentFrame.GetFirstQueueItem();
                    while (currentQueueItem.MessageType != MessageQueueContainer.MessageType.None)
                    {
                        m_AdvanceInboundFrameHistory = true;

                        if (!isTesting)
                        {
                            currentQueueItem.UpdateStage = currentStage;
                            ProcessMessage(currentQueueItem);
                        }

                        currentQueueItem = currentFrame.GetNextQueueItem();
                    }

                    //We call this to dispose of the shared stream writer and stream
                    currentFrame.CloseQueue();
                }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                s_ProcessReceiveQueue.End();
#endif
            }
        }

        public void AdvanceFrameHistoryIfNeeded()
        {
            if (m_AdvanceInboundFrameHistory)
            {
                m_MessageQueueContainer.AdvanceFrameHistory(MessageQueueHistoryFrame.QueueFrameType.Inbound);
                m_AdvanceInboundFrameHistory = false;
            }
        }

        /// <summary>
        /// ProcessSendQueue
        /// Called to send both performance RPC and internal messages and then flush the outbound queue
        /// </summary>
        internal void ProcessSendQueue(bool isListening)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_ProcessSendQueue.Begin();
#endif

            MessageQueueSendAndFlush(isListening);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_ProcessSendQueue.End();
#endif
        }

        /// <summary>
        /// FillTargetList
        /// Fills a list with the ClientId's an item is targeted to
        /// </summary>
        /// <param name="item">the MessageQueueItem we want targets for</param>
        /// <param name="targetList">the list to fill</param>
        private static void FillTargetList(in MessageFrameItem item, List<ulong> targetList)
        {
            switch (item.MessageType)
            {
                case MessageQueueContainer.MessageType.ServerRpc:
                    targetList.Add(item.NetworkId);
                    break;
                default:
                // todo: consider the implications of default usage of queueItem.clientIds
                case MessageQueueContainer.MessageType.ClientRpc:
                    // copy the list
                    targetList.AddRange(item.ClientNetworkIds);
                    break;
            }
        }

        /// <summary>
        /// Sends all message queue items in the current outbound frame
        /// </summary>
        /// <param name="isListening">if flase it will just process through the queue items but attempt to send</param>
        private void MessageQueueSendAndFlush(bool isListening)
        {
            var advanceFrameHistory = false;
            if (!ReferenceEquals(m_MessageQueueContainer, null))
            {
                var currentFrame = m_MessageQueueContainer.GetCurrentFrame(MessageQueueHistoryFrame.QueueFrameType.Outbound, NetworkUpdateStage.PostLateUpdate);
                if (currentFrame != null)
                {
                    var currentQueueItem = currentFrame.GetFirstQueueItem();
                    while (currentQueueItem.MessageType != MessageQueueContainer.MessageType.None)
                    {
                        advanceFrameHistory = true;
                        if (isListening)
                        {
                            m_TargetIdBuffer.Clear();
                            FillTargetList(currentQueueItem, m_TargetIdBuffer);

                            if (m_MessageQueueContainer.IsUsingBatching())
                            {
                                m_MessageBatcher.QueueItem(m_TargetIdBuffer, currentQueueItem, k_BatchThreshold, SendCallback);
                            }
                            else
                            {
                                SendFrameQueueItem(m_TargetIdBuffer, currentQueueItem);
                            }

                            foreach (var target in m_TargetIdBuffer)
                            {
                                m_NetworkManager.NetworkMetrics.TrackNetworkMessageSent(target, MessageQueueContainer.GetMessageTypeName(currentQueueItem.MessageType), currentQueueItem.MessageData.Count);
                            }
                        }

                        currentQueueItem = currentFrame.GetNextQueueItem();
                    }

                    //If the size is < m_BatchThreshold then just send the messages
                    if (isListening && advanceFrameHistory && m_MessageQueueContainer.IsUsingBatching())
                    {
                        m_MessageBatcher.SendItems(0, SendCallback);
                    }
                }

                //If we processed any messages, then advance to the next frame
                if (advanceFrameHistory)
                {
                    m_MessageQueueContainer.AdvanceFrameHistory(MessageQueueHistoryFrame.QueueFrameType.Outbound);
                }
            }
        }

        /// <summary>
        /// This is the callback from the batcher when it need to send a batch
        /// </summary>
        /// <param name="clientId"> clientId to send to</param>
        /// <param name="sendStream"> the stream to send</param>
        private void SendCallback(ulong clientId, MessageBatcher.SendStream sendStream)
        {
            var length = (int)sendStream.Buffer.Length;
            var bytes = sendStream.Buffer.GetBuffer();
            var sendBuffer = new ArraySegment<byte>(bytes, 0, length);

            var networkDelivery = sendStream.Delivery;
            // If the length is greater than the fragmented threshold, switch to a fragmented channel.
            // This is kind of a hack to get around issues with certain usages patterns on fragmentation with UNet.
            // We send everything unfragmented to avoid those issues, and only switch to the fragmented channel
            // if we have no other choice.
            if (length > k_FragmentationThreshold)
            {
                networkDelivery = NetworkDelivery.ReliableFragmentedSequenced;
            }

            m_MessageQueueContainer.NetworkManager.NetworkMetrics.TrackTransportBytesSent(length);
            m_MessageQueueContainer.NetworkManager.NetworkConfig.NetworkTransport.Send(clientId, sendBuffer, networkDelivery);
        }

        /// <summary>
        /// SendFrameQueueItem
        /// Sends the Message Queue Item to the specified destination
        /// </summary>
        /// <param name="item">Information on what to send</param>
        private void SendFrameQueueItem(IReadOnlyCollection<ulong> targetIds, in MessageFrameItem item)
        {
            var networkDelivery = item.Delivery;
            // If the length is greater than the fragmented threshold, switch to a fragmented channel.
            // This is kind of a hack to get around issues with certain usages patterns on fragmentation with UNet.
            // We send everything unfragmented to avoid those issues, and only switch to the fragmented channel
            // if we have no other choice.
            if (item.MessageData.Count > k_FragmentationThreshold)
            {
                networkDelivery = NetworkDelivery.ReliableFragmentedSequenced;
            }

            foreach (var clientId in targetIds)
            {
                m_MessageQueueContainer.NetworkManager.NetworkMetrics.TrackTransportBytesSent(item.MessageData.Count);
                m_MessageQueueContainer.NetworkManager.NetworkConfig.NetworkTransport.Send(clientId, item.MessageData, networkDelivery);
            }
        }

        internal MessageQueueProcessor(MessageQueueContainer messageQueueContainer, NetworkManager networkManager)
        {
            m_MessageQueueContainer = messageQueueContainer;
            m_NetworkManager = networkManager;
        }
    }
}
