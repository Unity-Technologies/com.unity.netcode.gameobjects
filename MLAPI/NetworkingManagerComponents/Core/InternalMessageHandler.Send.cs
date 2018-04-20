using System.Collections.Generic;
using MLAPI.Data;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.NetworkingManagerComponents.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static partial class InternalMessageHandler
    {
        internal static byte[] FinalMessageBuffer;
        internal static void PassthroughSend(uint targetId, uint sourceId, ushort messageType, int channelId, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            NetId targetNetId = new NetId(targetId);
            if (netManager.isHost && targetNetId.IsHost())
            {
                //Host trying to send data to it's own client
                Debug.LogWarning("MLAPI: Send method got message aimed at server from the server?");
                return;
            }

            using (BitWriter writer = new BitWriter())
            {
                writer.WriteUShort(messageType);
                writer.WriteBool(networkId != null);

                if (networkId != null)
                    writer.WriteUInt(networkId.Value);

                if (orderId != null)
                    writer.WriteUShort(orderId.Value);

                writer.WriteBool(true);
                writer.WriteUInt(sourceId);

                writer.WriteAlignBits();

                if (netManager.NetworkConfig.EncryptedChannelsHashSet.Contains(MessageManager.reverseChannels[channelId]))
                    writer.WriteByteArray(CryptographyHelper.Encrypt(data, netManager.connectedClients[targetId].AesKey));
                else
                    writer.WriteByteArray(data);

                writer.Finalize(ref FinalMessageBuffer);

                byte error;
                NetworkTransport.QueueMessageForSending(targetNetId.HostId, targetNetId.ConnectionId, channelId, FinalMessageBuffer, (int)writer.GetFinalizeSize(), out error);
            }
        }

        internal static void Send(uint clientId, string messageType, string channelName, byte[] data, uint? networkId = null, ushort? orderId = null, bool skipQueue = false)
        {
            NetId netId = new NetId(clientId);
            if (netManager.isHost && netId.IsHost())
            {
                //Don't invoke the message on our own machine. Instant stack overflow.
                Debug.LogWarning("MLAPI: Cannot send message to own client");
                return;
            }
            else if (netId.IsHost())
            {
                //Client trying to send data to host
                netId = NetId.ServerNetId;
            }

            bool isPassthrough = (!netManager.isServer && clientId != NetId.ServerNetId.GetClientId() && netManager.NetworkConfig.AllowPassthroughMessages);
            if (isPassthrough && !netManager.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType]))
            {
                Debug.LogWarning("MLAPI: The The MessageType " + messageType + " is not registered as an allowed passthrough message type.");
                return;
            }

            using (BitWriter writer = new BitWriter())
            {
                writer.WriteUShort(MessageManager.messageTypes[messageType]);
                writer.WriteBool(networkId != null);

                if (networkId != null)
                    writer.WriteUInt(networkId.Value);

                if (orderId != null)
                    writer.WriteUShort(orderId.Value);

                writer.WriteBool(isPassthrough);

                if (isPassthrough)
                    writer.WriteUInt(clientId);

                writer.WriteAlignBits();

                if (netManager.NetworkConfig.EncryptedChannelsHashSet.Contains(channelName))
                {
                    //This is an encrypted message.
                    byte[] encrypted;
                    if (netManager.isServer)
                        encrypted = CryptographyHelper.Encrypt(data, netManager.connectedClients[clientId].AesKey);
                    else
                        encrypted = CryptographyHelper.Encrypt(data, netManager.clientAesKey);

                    writer.WriteByteArray(encrypted);
                }
                else
                    writer.WriteByteArray(data);

                byte error;
                if (isPassthrough)
                    netId = NetId.ServerNetId;

                writer.Finalize(ref FinalMessageBuffer);

                if (skipQueue)
                    NetworkTransport.Send(netId.HostId, netId.ConnectionId, MessageManager.channels[channelName], FinalMessageBuffer, (int)writer.GetFinalizeSize(), out error);
                else
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, MessageManager.channels[channelName], FinalMessageBuffer, (int)writer.GetFinalizeSize(), out error);
            }
        }

        internal static void Send(uint[] clientIds, string messageType, string channelName, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            if (netManager.NetworkConfig.EncryptedChannelsHashSet.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return;
            }

            using (BitWriter writer = new BitWriter())
            {
                writer.WriteUShort(MessageManager.messageTypes[messageType]);
                writer.WriteBool(networkId != null);

                if (networkId != null)
                    writer.WriteUInt(networkId.Value);

                if (orderId != null)
                    writer.WriteUShort(orderId.Value);

                writer.WriteBool(false);

                writer.WriteAlignBits();

                writer.WriteByteArray(data);

                int channel = MessageManager.channels[channelName];
                for (int i = 0; i < clientIds.Length; i++)
                {
                    NetId netId = new NetId(clientIds[i]);
                    if (netManager.isHost && netId.IsHost())
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (netId.IsHost())
                    {
                        //Client trying to send data to host
                        netId = NetId.ServerNetId;
                    }

                    writer.Finalize(ref FinalMessageBuffer);

                    byte error;
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channel, FinalMessageBuffer, (int)writer.GetFinalizeSize(), out error);
                }
            }
        }

        internal static void Send(List<uint> clientIds, string messageType, string channelName, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            if (netManager.NetworkConfig.EncryptedChannelsHashSet.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return;
            }

            using (BitWriter writer = new BitWriter())
            {
                writer.WriteUShort(MessageManager.messageTypes[messageType]);
                writer.WriteBool(networkId != null);

                if (networkId != null)
                    writer.WriteUInt(networkId.Value);

                if (orderId != null)
                    writer.WriteUShort(orderId.Value);

                writer.WriteBool(false);

                writer.WriteAlignBits();

                writer.WriteByteArray(data);

                int channel = MessageManager.channels[channelName];
                for (int i = 0; i < clientIds.Count; i++)
                {
                    NetId netId = new NetId(clientIds[i]);
                    if (netManager.isHost && netId.IsHost())
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (netId.IsHost())
                    {
                        //Client trying to send data to host
                        netId = NetId.ServerNetId;
                    }

                    writer.Finalize(ref FinalMessageBuffer);

                    byte error;
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channel, FinalMessageBuffer, (int)writer.GetFinalizeSize(), out error);
                }
            }
        }

        internal static void Send(string messageType, string channelName, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            if (netManager.connectedClients.Count == 0)
                return;
            if (netManager.NetworkConfig.EncryptedChannels.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return;
            }
            using (BitWriter writer = new BitWriter())
            {
                writer.WriteUShort(MessageManager.messageTypes[messageType]);
                writer.WriteBool(networkId != null);

                if (networkId != null)
                    writer.WriteUInt(networkId.Value);

                if (orderId != null)
                    writer.WriteUShort(orderId.Value);

                writer.WriteBool(false);

                writer.WriteAlignBits();

                writer.WriteByteArray(data);

                int channel = MessageManager.channels[channelName];
                foreach (KeyValuePair<uint, NetworkedClient> pair in netManager.connectedClients)
                {
                    NetId netId = new NetId(pair.Key);
                    if (netManager.isHost && netId.IsHost())
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (netId.IsHost())
                    {
                        //Client trying to send data to host
                        netId = NetId.ServerNetId;
                    }

                    writer.Finalize(ref FinalMessageBuffer);

                    byte error;
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channel, FinalMessageBuffer, (int)writer.GetFinalizeSize(), out error);
                }
            }
        }

        internal static void Send(string messageType, string channelName, byte[] data, uint clientIdToIgnore, uint? networkId = null, ushort? orderId = null)
        {
            if (netManager.NetworkConfig.EncryptedChannels.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return;
            }

            using (BitWriter writer = new BitWriter())
            {
                writer.WriteUShort(MessageManager.messageTypes[messageType]);
                writer.WriteBool(networkId != null);

                if (networkId != null)
                    writer.WriteUInt(networkId.Value);

                if (orderId != null)
                    writer.WriteUShort(orderId.Value);

                writer.WriteBool(false);

                writer.WriteAlignBits();

                writer.WriteByteArray(data);

                int channel = MessageManager.channels[channelName];
                foreach (KeyValuePair<uint, NetworkedClient> pair in netManager.connectedClients)
                {
                    if (pair.Key == clientIdToIgnore)
                        continue;

                    NetId netId = new NetId(pair.Key);
                    if (netManager.isHost && netId.IsHost())
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (netId.IsHost())
                    {
                        //Client trying to send data to host
                        netId = NetId.ServerNetId;
                    }

                    writer.Finalize(ref FinalMessageBuffer);

                    byte error;
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channel, FinalMessageBuffer, (int)writer.GetFinalizeSize(), out error);
                }
            }
        }
    }
}
