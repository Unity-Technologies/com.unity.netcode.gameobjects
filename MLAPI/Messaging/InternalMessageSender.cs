using System;
using MLAPI.Configuration;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.Profiling;
using MLAPI.Security;
using BitStream = MLAPI.Serialization.BitStream;

namespace MLAPI.Messaging
{
    internal static class InternalMessageSender
    {
        internal static void Send(ulong clientId, byte messageType, string channelName, BitStream messageStream, SecuritySendFlags flags, NetworkedObject targetObject)
        {
            messageStream.PadStream();

            if (NetworkingManager.Singleton.IsServer && clientId == NetworkingManager.Singleton.ServerClientId) 
                return;

            if (targetObject != null && !targetObject.observers.Contains(clientId))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Developer) NetworkLog.LogWarning("Silently suppressed send call because it was directed to an object without visibility");
                return;
            }
            
            using (BitStream stream = MessagePacker.WrapMessage(messageType, clientId, messageStream, flags))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                
                NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channelName);

                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, string channelName, BitStream messageStream, SecuritySendFlags flags, NetworkedObject targetObject)
        {
            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;

            if (authenticated || encrypted)
            {
                for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, messageType, channelName, messageStream, flags, targetObject);
                }
            }
            else
            {
                messageStream.PadStream();

                using (BitStream stream = MessagePacker.WrapMessage(messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        if (NetworkingManager.Singleton.IsServer && NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.ServerClientId) 
                            continue;

                        if (targetObject != null && !targetObject.observers.Contains(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer) NetworkLog.LogWarning("Silently suppressed send(all) call because it was directed to an object without visibility");
                            continue;
                        }
                        
                        NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channelName);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }

        internal static void Send(byte messageType, string channelName, ulong clientIdToIgnore, BitStream messageStream, SecuritySendFlags flags, NetworkedObject targetObject)
        {
            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;

            if (encrypted || authenticated)
            {
                for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == clientIdToIgnore)
                        continue;

                    Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, messageType, channelName, messageStream, flags, targetObject);
                }
            }
            else
            {
                messageStream.PadStream();

                using (BitStream stream = MessagePacker.WrapMessage(messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        if (NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == clientIdToIgnore ||
                            (NetworkingManager.Singleton.IsServer && NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.ServerClientId))
                            continue;

                        if (targetObject != null && !targetObject.observers.Contains(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId))
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Developer) NetworkLog.LogWarning("Silently suppressed send(ignore) call because it was directed to an object without visibility");
                            continue;
                        }

                        NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channelName);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }
    }
}
