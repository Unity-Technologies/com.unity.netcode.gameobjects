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

            if (NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.ServerClientId) return;

            using (BitStream stream = MessagePacker.WrapMessage(messageType, messageStream))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channel, MLAPIConstants.MESSAGE_NAMES[messageType]);

                NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
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
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsList[i].ClientId == NetworkManager.Singleton.ServerClientId)
                        continue;

                    NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
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
                    if (NetworkManager.Singleton.IsServer && clientIds[i] == NetworkManager.Singleton.ServerClientId)
                        continue;

                    NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(clientIds[i], new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
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
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkManager.Singleton.ConnectedClientsList[i].ClientId == clientIdToIgnore ||
                        (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsList[i].ClientId == NetworkManager.Singleton.ServerClientId))
                        continue;

                    NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
                    ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)stream.Length);
                }

                NetworkProfiler.EndEvent();
            }
        }
    }
}