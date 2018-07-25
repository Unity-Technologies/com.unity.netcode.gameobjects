using System.IO;
using MLAPI.Collections;
using MLAPI.Profiler;
using MLAPI.Serialization;

namespace MLAPI.Internal
{
    internal static partial class InternalMessageHandler
    {
        internal static void Send(uint clientId, string messageType, string channelName, Stream messageStream, bool skipQueue = false)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteUInt16Packed(MessageManager.messageTypes[messageType]);
                stream.CopyFrom(messageStream);

                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, messageType);
                byte error;
                if (skipQueue)
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], true, out error);
                else
					netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
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
                    byte error;
					netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(netManager.ConnectedClientsList[i].ClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
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
					if (netManager.ConnectedClientsList[i].ClientId == clientIdToIgnore)
                        continue;
                    
                    byte error;
					netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(netManager.ConnectedClientsList[i].ClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                }
                NetworkProfiler.EndEvent();
            }
        }
    }
}
