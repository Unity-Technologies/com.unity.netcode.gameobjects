using System.Collections.Generic;
using System.IO;
using MLAPI.Data;
using MLAPI.Data.NetworkProfiler;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.NetworkingManagerComponents.Cryptography;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static partial class InternalMessageHandler
    {
        internal static void Send(uint clientId, string messageType, string channelName, Stream messageStream, bool skipQueue = false)
        {
            uint targetClientId = clientId;
            if (netManager.isHost && targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
            {
                //Don't invoke the message on our own machine. Instant stack overflow.
                return;
            }
            else if (targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
            {
                //Client trying to send data to host
                targetClientId = netManager.NetworkConfig.NetworkTransport.ServerNetId;
            }

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteUInt16Packed(MessageManager.messageTypes[messageType]);
                stream.CopyFrom(messageStream);

                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, messageType);
                byte error;
                if (skipQueue)
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], true, out error);
                else
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(string messageType, string channelName, Stream messageStream)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteUInt16Packed(MessageManager.messageTypes[messageType]);
                stream.CopyFrom(messageStream);

                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, messageType);
                for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                {
                    uint targetClientId = netManager.ConnectedClientsList[i].ClientId;
                    if (netManager.isHost && targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        return;
                    }
                    else if (targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Client trying to send data to host
                        targetClientId = netManager.NetworkConfig.NetworkTransport.ServerNetId;
                    }
                    
                    byte error;
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                }
                NetworkProfiler.EndEvent();
            }
        }
        
        internal static void Send(string messageType, string channelName, uint clientIdToIgnore, Stream messageStream)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteUInt16Packed(MessageManager.messageTypes[messageType]);
                stream.CopyFrom(messageStream);

                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, messageType);
                for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                {
                    uint targetClientId = netManager.ConnectedClientsList[i].ClientId;
                    if (targetClientId == clientIdToIgnore)
                        continue;
                    
                    if (netManager.isHost && targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        return;
                    }
                    else if (targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Client trying to send data to host
                        targetClientId = netManager.NetworkConfig.NetworkTransport.ServerNetId;
                    }
                    
                    byte error;
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                }
                NetworkProfiler.EndEvent();
            }
        }
    }
}
