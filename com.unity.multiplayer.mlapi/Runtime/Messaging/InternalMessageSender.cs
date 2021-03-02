using System;
using System.Collections.Generic;
using MLAPI.Configuration;
using MLAPI.Internal;
using MLAPI.Profiling;
using MLAPI.Transports;
using BitStream = MLAPI.Serialization.BitStream;

namespace MLAPI.Messaging
{
    internal static class InternalMessageSender
    {
        internal static void Send(ulong clientId, byte messageType, Channel channel, BitStream messageStream)
        {
            messageStream.PadStream();

            if (NetworkingManager.Singleton.IsServer && clientId == NetworkingManager.Singleton.ServerClientId) return;

            using (BitStream stream = MessagePacker.WrapMessage(messageType, messageStream))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channel, MLAPIConstants.MESSAGE_NAMES[messageType]);

                NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
                ProfilerStatManager.bytesSent.Record((int)stream.Length);
                PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)stream.Length);

                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, Channel channel, BitStream messageStream)
        {
            messageStream.PadStream();

            using (BitStream stream = MessagePacker.WrapMessage(messageType, messageStream))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channel, MLAPIConstants.MESSAGE_NAMES[messageType]);
                for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkingManager.Singleton.IsServer && NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.ServerClientId)
                        continue;

                    NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
                    ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)stream.Length);
                }

                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, Channel channel, List<ulong> clientIds, BitStream messageStream)
        {
            if (clientIds == null)
            {
                Send(messageType, channel, messageStream);
                return;
            }

            messageStream.PadStream();

            using (BitStream stream = MessagePacker.WrapMessage(messageType, messageStream))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channel, MLAPIConstants.MESSAGE_NAMES[messageType]);
                for (int i = 0; i < clientIds.Count; i++)
                {
                    if (NetworkingManager.Singleton.IsServer && clientIds[i] == NetworkingManager.Singleton.ServerClientId)
                        continue;

                    NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientIds[i], new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
                    ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)stream.Length);
                }

                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, Channel channel, ulong clientIdToIgnore, BitStream messageStream)
        {
            messageStream.PadStream();

            using (BitStream stream = MessagePacker.WrapMessage(messageType, messageStream))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channel, MLAPIConstants.MESSAGE_NAMES[messageType]);
                for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == clientIdToIgnore ||
                        (NetworkingManager.Singleton.IsServer && NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.ServerClientId))
                        continue;

                    NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
                    ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)stream.Length);
                }

                NetworkProfiler.EndEvent();
            }
        }
    }
}