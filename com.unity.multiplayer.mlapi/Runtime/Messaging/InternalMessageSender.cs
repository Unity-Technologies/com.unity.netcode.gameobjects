using System;
using System.Collections.Generic;
using MLAPI.Configuration;
using MLAPI.Internal;
using MLAPI.Profiling;
using MLAPI.Serialization;
using MLAPI.Transports;

namespace MLAPI.Messaging
{
    internal static class InternalMessageSender
    {
        internal static void Send(ulong clientId, byte messageType, NetworkChannel networkChannel, NetworkStream messageStream)
        {
            messageStream.PadStream();

            if (NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.ServerClientId) return;

            using (NetworkStream stream = MessagePacker.WrapMessage(messageType, messageStream))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, networkChannel, MLAPIConstants.MESSAGE_NAMES[messageType]);

                NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), networkChannel);
                ProfilerStatManager.bytesSent.Record((int)stream.Length);
                PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)stream.Length);

                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, NetworkChannel networkChannel, NetworkStream messageStream)
        {
            messageStream.PadStream();

            using (NetworkStream stream = MessagePacker.WrapMessage(messageType, messageStream))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, networkChannel, MLAPIConstants.MESSAGE_NAMES[messageType]);
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsList[i].ClientId == NetworkManager.Singleton.ServerClientId)
                        continue;

                    NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), networkChannel);
                    ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)stream.Length);
                }

                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, NetworkChannel networkChannel, List<ulong> clientIds, NetworkStream messageStream)
        {
            if (clientIds == null)
            {
                Send(messageType, networkChannel, messageStream);
                return;
            }

            messageStream.PadStream();

            using (NetworkStream stream = MessagePacker.WrapMessage(messageType, messageStream))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, networkChannel, MLAPIConstants.MESSAGE_NAMES[messageType]);
                for (int i = 0; i < clientIds.Count; i++)
                {
                    if (NetworkManager.Singleton.IsServer && clientIds[i] == NetworkManager.Singleton.ServerClientId)
                        continue;

                    NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(clientIds[i], new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), networkChannel);
                    ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)stream.Length);
                }

                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, NetworkChannel networkChannel, ulong clientIdToIgnore, NetworkStream messageStream)
        {
            messageStream.PadStream();

            using (NetworkStream stream = MessagePacker.WrapMessage(messageType, messageStream))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, networkChannel, MLAPIConstants.MESSAGE_NAMES[messageType]);
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkManager.Singleton.ConnectedClientsList[i].ClientId == clientIdToIgnore ||
                        (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsList[i].ClientId == NetworkManager.Singleton.ServerClientId))
                        continue;

                    NetworkManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), networkChannel);
                    ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    PerformanceDataManager.Increment(ProfilerConstants.NumberBytesSent, (int)stream.Length);
                }

                NetworkProfiler.EndEvent();
            }
        }
    }
}