using MLAPI.Data;
using MLAPI.Profiling;
using MLAPI.Serialization;

namespace MLAPI.Internal
{
    internal static partial class InternalMessageHandler
    {
        internal static void Send(uint clientId, byte messageType, string channelName, BitStream messageStream, SecuritySendFlags flags, bool skipQueue = false)
        {
            messageStream.PadStream();

            if (NetworkingManager.Singleton.IsServer && clientId == NetworkingManager.Singleton.ServerClientId) return;

            using (BitStream stream = MessageManager.WrapMessage(messageType, clientId, messageStream, flags))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                byte error;
                if (skipQueue)
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], true, out error);
                else
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, string channelName, BitStream messageStream, SecuritySendFlags flags)
        {
            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && netManager.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && netManager.NetworkConfig.EnableEncryption;

            if (authenticated || encrypted)
            {
                for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                {
                    Send(netManager.ConnectedClientsList[i].ClientId, messageType, channelName, messageStream, flags);
                }
            }
            else
            {
                messageStream.PadStream();

                using (BitStream stream = MessageManager.WrapMessage(messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                    {
                        if (NetworkingManager.Singleton.IsServer && netManager.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.ServerClientId) continue;
                        byte error;
                        netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(netManager.ConnectedClientsList[i].ClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }

        internal static void Send(byte messageType, string channelName, uint clientIdToIgnore, BitStream messageStream, SecuritySendFlags flags)
        {
            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && netManager.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && netManager.NetworkConfig.EnableEncryption;

            if (encrypted || authenticated)
            {
                for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                {
                    if (netManager.ConnectedClientsList[i].ClientId == clientIdToIgnore)
                        continue;

                    Send(netManager.ConnectedClientsList[i].ClientId, messageType, channelName, messageStream, flags);
                }
            }
            else
            {
                messageStream.PadStream();

                using (BitStream stream = MessageManager.WrapMessage(messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                    {
                        if (netManager.ConnectedClientsList[i].ClientId == clientIdToIgnore ||
                            (NetworkingManager.Singleton.IsServer && netManager.ConnectedClientsList[i].ClientId == NetworkingManager.Singleton.ServerClientId))
                            continue;

                        byte error;
                        netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(netManager.ConnectedClientsList[i].ClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }
    }
}
