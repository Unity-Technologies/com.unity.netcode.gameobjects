using System.IO;
using MLAPI.Collections;
using MLAPI.Data;
using MLAPI.Profiler;
using MLAPI.Serialization;

namespace MLAPI.Internal
{
    internal static partial class InternalMessageHandler
    {
        internal static void Send(uint clientId, byte messageType, string channelName, Stream messageStream, bool skipQueue = false)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteByte(messageType);
                stream.CopyFrom(messageStream);

                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                byte error;
                if (skipQueue)
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], true, out error);
                else
					netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, string channelName, Stream messageStream)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteByte(messageType);
                stream.CopyFrom(messageStream);

                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                {
                    byte error;
					netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(netManager.ConnectedClientsList[i].ClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                }
                NetworkProfiler.EndEvent();
            }
        }
        
        internal static void Send(byte messageType, string channelName, uint clientIdToIgnore, Stream messageStream)
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                BitWriter writer = new BitWriter(stream);
                writer.WriteByte(messageType);
                stream.CopyFrom(messageStream);

                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
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
