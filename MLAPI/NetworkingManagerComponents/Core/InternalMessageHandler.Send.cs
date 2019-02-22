using MLAPI.Data;
using MLAPI.Logging;
using MLAPI.Profiling;
using MLAPI.Serialization;

namespace MLAPI.Internal
{
    internal static partial class InternalMessageHandler
    {
        internal static void Send(uint clientId, byte messageType, string channelName, BitStream messageStream, SecuritySendFlags flags, NetworkedObject targetObject, bool skipQueue = false)
        {
            messageStream.PadStream();

            if (NetworkingManager.Singleton.IsServer && clientId == NetworkingManager.Singleton.ServerClientId) 
                return;

            if (targetObject != null && !targetObject.observers.Contains(clientId))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogWarning("Silently suppressed send call because it was directed to an object without visibility");
                return;
            }
            
            using (BitStream stream = MessageManager.WrapMessage(messageType, clientId, messageStream, flags))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                byte error;
                if (skipQueue)
                    NetworkingManager.Singleton.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], true, out error);
                else
                    NetworkingManager.Singleton.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
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

                using (BitStream stream = MessageManager.WrapMessage(messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        if (NetworkingManager.Singleton.IsServer && NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.ServerClientId) 
                            continue;

                        if (targetObject != null && !targetObject.observers.Contains(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId))
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogWarning("Silently suppressed send(all) call because it was directed to an object without visibility");
                            continue;
                        }
                        
                        byte error;
                        NetworkingManager.Singleton.NetworkConfig.NetworkTransport.QueueMessageForSending(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }

        internal static void Send(byte messageType, string channelName, uint clientIdToIgnore, BitStream messageStream, SecuritySendFlags flags, NetworkedObject targetObject)
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

                using (BitStream stream = MessageManager.WrapMessage(messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                    {
                        if (NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == clientIdToIgnore ||
                            (NetworkingManager.Singleton.IsServer && NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.ServerClientId))
                            continue;

                        if (targetObject != null && !targetObject.observers.Contains(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId))
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogWarning("Silently suppressed send(ignore) call because it was directed to an object without visibility");
                            continue;
                        }

                        byte error;
                        NetworkingManager.Singleton.NetworkConfig.NetworkTransport.QueueMessageForSending(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }
    }
}
