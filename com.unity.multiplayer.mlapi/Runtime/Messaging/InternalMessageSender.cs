using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using MLAPI.Configuration;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.Profiling;
using MLAPI.Security;
using MLAPI.Serialization.Pooled;
using BitStream = MLAPI.Serialization.BitStream;

namespace MLAPI.Messaging
{
    public static class InternalMessageSender
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR                
        static ProfilerMarker s_MLAPIRPCSendQueued = new ProfilerMarker("MLAPIRPCSendQueued");
#endif

        private const string MLAPI_STD_RPC_CHANNEL = "STD_RPC_CH";
        
        /// <summary>
        /// GetAllClientIdsExcluding
        /// Gets all client network ids excluding the one clientId and the server client id 
        /// </summary>
        /// <param name="clientId">client id to exclude</param>
        /// <returns>remaining list of client ids</returns>
        public static List<ulong> GetAllClientIdsExcluding(ulong clientId)
        {
            List<ulong> ClientIds = GetAllClientIds();
            if(ClientIds != null)
            {
                ClientIds.Remove(clientId);
            }
            return ClientIds;
        }

        /// <summary>
        /// GetAllClientIds
        /// Gets all client network ids excluding the server client id 
        /// </summary>
        /// <returns>remaining list of client ids</returns>
        public static List<ulong> GetAllClientIds()
        {
            List<ulong> ClientIds = new List<ulong>(NetworkingManager.Singleton.ConnectedClients.Keys);
            ClientIds.Remove(NetworkingManager.Singleton.ServerClientId);
            return ClientIds.Count == 0 ? new List<ulong>():ClientIds;
        }    

        internal static void Send(ulong clientId, byte messageType, string channelName, BitStream messageStream, SecuritySendFlags flags)
        {
            messageStream.PadStream();
            
            if (NetworkingManager.Singleton.IsServer && clientId == NetworkingManager.Singleton.ServerClientId )
                return;

            using (BitStream stream = MessagePacker.WrapMessage(messageType, clientId, messageStream, flags))
            {
                NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);

                NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channelName);

                ProfilerStatManager.bytesSent.Record((int)stream.Length);

                NetworkProfiler.EndEvent();
            }
        }

        internal static void Send(byte messageType, string channelName, BitStream messageStream, SecuritySendFlags flags)
        {
            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;

            if (authenticated || encrypted)
            {
                for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, messageType, channelName, messageStream, flags);
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

                        NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channelName);
                        ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    }

                    NetworkProfiler.EndEvent();
                }
            }
        }

        internal static void Send(byte messageType, string channelName, List<ulong> clientIds, BitStream messageStream, SecuritySendFlags flags)
        {
            if (clientIds == null)
            {
                Send(messageType, channelName, messageStream, flags);
                return;
            }

            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;

            if (authenticated || encrypted)
            {
                for (int i = 0; i < clientIds.Count; i++)
                {
                    Send(clientIds[i], messageType, channelName, messageStream, flags);
                }
            }
            else
            {
                messageStream.PadStream();

                using (BitStream stream = MessagePacker.WrapMessage(messageType, 0, messageStream, flags))
                {
                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < clientIds.Count; i++)
                    {
                        if (NetworkingManager.Singleton.IsServer && clientIds[i] == NetworkingManager.Singleton.ServerClientId)
                            continue;

                        NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(clientIds[i], new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channelName);
                        ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    }

                    NetworkProfiler.EndEvent();
                }
            }
        }

        internal static void Send(byte messageType, string channelName, ulong clientIdToIgnore, BitStream messageStream, SecuritySendFlags flags)
        {
            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;
            bool authenticated = ((flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;

            if (encrypted || authenticated)
            {
                for (int i = 0; i < NetworkingManager.Singleton.ConnectedClientsList.Count; i++)
                {
                    if (NetworkingManager.Singleton.ConnectedClientsList[i].ClientId == clientIdToIgnore)
                        continue;

                    Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, messageType, channelName, messageStream, flags);
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

                        NetworkingManager.Singleton.NetworkConfig.NetworkTransport.Send(NetworkingManager.Singleton.ConnectedClientsList[i].ClientId, new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), channelName);
                        ProfilerStatManager.bytesSent.Record((int)stream.Length);
                    }

                    NetworkProfiler.EndEvent();
                }
            }
        }        
    }
}
