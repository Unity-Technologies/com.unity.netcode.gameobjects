using System;
using System.Collections.Generic;
using MLAPI.Configuration;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.Profiling;
using MLAPI.Security;
using MLAPI.Transports;
using BitStream = MLAPI.Serialization.BitStream;

namespace MLAPI.Messaging
{
    internal class InternalMessageSender
    {
        private NetworkingManager networkingManager;

        internal InternalMessageSender(NetworkingManager manager )
        {
            networkingManager = manager;
        }

        internal void Send(ulong clientId, byte messageType, byte channel, BitStream messageStream, SecuritySendFlags flags)
        {
            messageStream.PadStream();

            if (networkingManager.IsServer && clientId == networkingManager.ServerClientId)
                return;

            using (BitStream stream = MessagePacker.WrapMessage(networkingManager, messageType, clientId, messageStream, flags))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint) stream.Length, channel,
                    MLAPIConstants.MESSAGE_NAMES[messageType]);

                networkingManager.NetworkConfig.NetworkTransport.Send(clientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
                ProfilerStatManager.bytesSent.Record((int)stream.Length);

                NetworkProfiler.EndEvent();
            }
        }

        internal void Send(byte messageType, byte channel, BitStream messageStream, SecuritySendFlags flags)
        {
            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && networkingManager.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && networkingManager.NetworkConfig.EnableEncryption;

            if (authenticated || encrypted)
            {
                for (int i = 0; i < networkingManager.ConnectedClientsList.Count; i++)
                {
                    Send(networkingManager.ConnectedClientsList[i].ClientId, messageType, channel, messageStream, flags);
                }
            }
            else
            {
                messageStream.PadStream();

                using (BitStream stream = MessagePacker.WrapMessage(networkingManager, messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channel, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < networkingManager.ConnectedClientsList.Count; i++)
                    {
                        if (networkingManager.IsServer && networkingManager.ConnectedClientsList[i].ClientId == networkingManager.ServerClientId)
                            continue;

                        networkingManager.NetworkConfig.NetworkTransport.Send(networkingManager.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
                        ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }

        internal void Send(byte messageType, byte channel, List<ulong> clientIds, BitStream messageStream, SecuritySendFlags flags)
        {
            if (clientIds == null)
            {
                Send(messageType, channel, messageStream, flags);
                return;
            }

            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && networkingManager.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && networkingManager.NetworkConfig.EnableEncryption;

            if (authenticated || encrypted)
            {
                for (int i = 0; i < clientIds.Count; i++)
                {
                    Send(clientIds[i], messageType, channel, messageStream, flags);
                }
            }
            else
            {
                messageStream.PadStream();

                using (BitStream stream = MessagePacker.WrapMessage(networkingManager, messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channel, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < clientIds.Count; i++)
                    {
                        if (networkingManager.IsServer && clientIds[i] == networkingManager.ServerClientId)
                            continue;

                        networkingManager.NetworkConfig.NetworkTransport.Send(clientIds[i], new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
                        ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }

        internal void Send(byte messageType, byte channel, ulong clientIdToIgnore, BitStream messageStream, SecuritySendFlags flags)
        {
            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && networkingManager.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && networkingManager.NetworkConfig.EnableEncryption;

            if (encrypted || authenticated)
            {
                for (int i = 0; i < networkingManager.ConnectedClientsList.Count; i++)
                {
                    if (networkingManager.ConnectedClientsList[i].ClientId == clientIdToIgnore)
                        continue;

                    Send(networkingManager.ConnectedClientsList[i].ClientId, messageType, channel, messageStream, flags);
                }
            }
            else
            {
                messageStream.PadStream();

                using (BitStream stream = MessagePacker.WrapMessage(networkingManager, messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channel, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < networkingManager.ConnectedClientsList.Count; i++)
                    {
                        if (networkingManager.ConnectedClientsList[i].ClientId == clientIdToIgnore ||
                            (networkingManager.IsServer && networkingManager.ConnectedClientsList[i].ClientId == networkingManager.ServerClientId))
                            continue;

                        networkingManager.NetworkConfig.NetworkTransport.Send(networkingManager.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channel);
                        ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }
    }
}
