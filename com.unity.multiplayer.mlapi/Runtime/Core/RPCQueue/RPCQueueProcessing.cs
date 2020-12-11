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
        public FrameQueueItem item;
        public PooledBitWriter writer;
        public PooledBitStream stream = new PooledBitStream();
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

        private Dictionary<ulong, SendStream> SendDict = new Dictionary<ulong, SendStream>();

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
                            }
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
            }
            return ret;
        }

        private void QueueItem(FrameQueueItem queueItem)
        {
            foreach (ulong clientid in GetTargetList(queueItem))
            {
                // todo: actually queue and buffer. For now, the dict contains just one entry ...
                SendDict[clientid] = new SendStream();
                SendDict[clientid].item = queueItem;
                SendDict[clientid].writer = new PooledBitWriter(SendDict[clientid].stream);

                SendDict[clientid].writer.WriteBit(false); // Encrypted
                SendDict[clientid].writer.WriteBit(false); // Authenticated
                switch (queueItem.QueueItemType)
                {
                    case RPCQueueManager.QueueItemType.ServerRPC:
                        SendDict[clientid].writer.WriteBits(MLAPIConstants.MLAPI_STD_SERVER_RPC, 6); // MessageType
                        break;
                    case RPCQueueManager.QueueItemType.ClientRPC:
                        SendDict[clientid].writer.WriteBits(MLAPIConstants.MLAPI_STD_CLIENT_RPC, 6); // MessageType
                        break;
                }

                SendDict[clientid].writer.WriteByte((byte)queueItem.MessageData.Count); // write the amounts of bytes that are coming up

                // write the message to send
                // todo: is there a faster alternative to .ToArray()
                SendDict[clientid].writer.WriteBytes(queueItem.MessageData.ToArray(), queueItem.MessageData.Count);

                SendDict[clientid].writer.WriteByte(42); // extra for testing
            }

            foreach (KeyValuePair<ulong, SendStream> entry in SendDict)
            {
                // read the queued message
                byte[] byteBuffer = new byte[1000];

                using PooledBitWriter writer = SendDict[entry.Key].writer;
                int length = (int)writer.GetStream().Length;

                Byte[] bytes = ((MLAPI.Serialization.BitStream)writer.GetStream()).GetBuffer();
                System.Buffer.BlockCopy(bytes, 0, byteBuffer, 0, length);

                ArraySegment<byte> sendBuffer = new ArraySegment<byte>(byteBuffer, 0, length);

                // todo: ... that gets sent right away
                NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(entry.Key, sendBuffer,
                    string.IsNullOrEmpty(entry.Value.item.Channel) ? "MLAPI_DEFAULT_MESSAGE" : entry.Value.item.Channel);

                ProfilerStatManager.bytesSent.Record((int)entry.Value.item.StreamSize);
                ProfilerStatManager.rpcsSent.Record();
            }

            SendDict.Clear();
        }

            /// <summary>
        /// SendFrameQueueItem
        /// Sends the RPC Queue Item to the specified destination
        /// </summary>
        /// <param name="queueItem">Information on what to send</param>
        private void SendFrameQueueItem(FrameQueueItem queueItem)
        {
            switch(queueItem.QueueItemType)
            {
                case RPCQueueManager.QueueItemType.ServerRPC:
                    {

                        NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(queueItem.NetworkId, queueItem.MessageData,
                            string.IsNullOrEmpty(queueItem.Channel) ? "MLAPI_DEFAULT_MESSAGE" : queueItem.Channel);

                        //For each packet sent, we want to record how much data we have sent
                        ProfilerStatManager.bytesSent.Record((int)queueItem.StreamSize);
                        ProfilerStatManager.rpcsSent.Record();
                        break;
                    }
                case RPCQueueManager.QueueItemType.ClientRPC:
                    {
                        foreach(ulong clientid in queueItem.ClientIds)
                        {
                            NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientid, queueItem.MessageData, string.IsNullOrEmpty(queueItem.Channel) ? "MLAPI_DEFAULT_MESSAGE" : queueItem.Channel);

                            //For each packet sent, we want to record how much data we have sent
                            ProfilerStatManager.bytesSent.Record((int)queueItem.StreamSize);
                        }
                        //For each client we send to, we want to record how many RPCs we have sent
                        ProfilerStatManager.rpcsSent.Record(queueItem.ClientIds?.Count ?? NetworkingManager.Singleton.ConnectedClientsList.Count);

                        break;
                    }
            }
        }

        public RPCQueueProcessing(RPCQueueManager rpcqueuemanager)
        {
            rpcQueueManager = rpcqueuemanager;
        }
    }
}
