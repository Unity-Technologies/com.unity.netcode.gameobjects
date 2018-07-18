using System.Collections.Generic;
using MLAPI.Data;
using MLAPI.Data.NetworkProfiler;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.NetworkingManagerComponents.Cryptography;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static partial class InternalMessageHandler
    {
        internal static byte[] FinalMessageBuffer;

        internal static void Send(uint clientId, string messageType, string channelName, BitWriter messageWriter, bool skipQueue = false)
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

            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteUShort(MessageManager.messageTypes[messageType]);
                writer.WriteWriter(messageWriter);

                writer.Finalize(ref FinalMessageBuffer);

                NetworkProfiler.StartEvent(TickType.Send, (uint)messageWriter.GetFinalizeSize(), channelName, messageType);
                byte error;
                if (skipQueue)
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), MessageManager.channels[channelName], true, out error);
                else
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), MessageManager.channels[channelName], false, out error);
                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(string messageType, string channelName, BitWriter messageWriter)
        {
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteUShort(MessageManager.messageTypes[messageType]);
                writer.WriteWriter(messageWriter);

                writer.Finalize(ref FinalMessageBuffer);

                NetworkProfiler.StartEvent(TickType.Send, (uint)messageWriter.GetFinalizeSize(), channelName, messageType);
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
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), MessageManager.channels[channelName], false, out error);
                }
                NetworkProfiler.EndEvent();
            }
        }
        
        internal static void Send(string messageType, string channelName, uint clientIdToIgnore, BitWriter messageWriter)
        {
            using (BitWriter writer = BitWriter.Get())
            {
                writer.WriteUShort(MessageManager.messageTypes[messageType]);
                writer.WriteWriter(messageWriter);

                writer.Finalize(ref FinalMessageBuffer);

                NetworkProfiler.StartEvent(TickType.Send, (uint)messageWriter.GetFinalizeSize(), channelName, messageType);
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
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), MessageManager.channels[channelName], false, out error);
                }
                NetworkProfiler.EndEvent();
            }
        }
    }
}
