using System.Collections.Generic;
using System.IO;
using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static partial class InternalMessageHandler
    {
        internal static void PassthroughSend(uint targetId, uint sourceId, ushort messageType, int channelId, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            NetId targetNetId = new NetId(targetId);
            if (netManager.isHost && targetNetId.IsHost())
            {
                //Host trying to send data to it's own client
                Debug.LogWarning("MLAPI: Send method got message aimed at server from the server?");
                return;
            }

            int sizeOfStream = 10;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(messageType);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(true);
                    writer.Write(sourceId);
                    if (netManager.NetworkConfig.EncryptedChannelsHashSet.Contains(MessageManager.reverseChannels[channelId]))
                    {
                        //Encrypted message
                        byte[] encrypted = CryptographyHelper.Encrypt(data, netManager.connectedClients[targetId].AesKey);
                        writer.Write((ushort)encrypted.Length);
                        writer.Write(encrypted);
                    }
                    else
                    {
                        writer.Write((ushort)data.Length);
                        writer.Write(data);
                    }
                }

                byte error;
                NetworkTransport.QueueMessageForSending(targetNetId.HostId, targetNetId.ConnectionId, channelId, stream.GetBuffer(), sizeOfStream, out error);
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

            int sizeOfStream = 6;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            if (isPassthrough)
                sizeOfStream += 4;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(isPassthrough);
                    if (isPassthrough)
                        writer.Write(clientId);

                    if (netManager.NetworkConfig.EncryptedChannelsHashSet.Contains(channelName))
                    {
                        //This is an encrypted message.
                        byte[] encrypted;
                        if (netManager.isServer)
                            encrypted = CryptographyHelper.Encrypt(data, netManager.connectedClients[clientId].AesKey);
                        else
                            encrypted = CryptographyHelper.Encrypt(data, netManager.clientAesKey);

                        writer.Write((ushort)encrypted.Length);
                        writer.Write(encrypted);
                    }
                    else
                    {
                        //Send in plaintext.
                        writer.Write((ushort)data.Length);
                        writer.Write(data);
                    }
                }
                byte error;
                if (isPassthrough)
                    netId = NetId.ServerNetId;
                if (skipQueue)
                    NetworkTransport.Send(netId.HostId, netId.ConnectionId, MessageManager.channels[channelName], stream.GetBuffer(), sizeOfStream, out error);
                else
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, MessageManager.channels[channelName], stream.GetBuffer(), sizeOfStream, out error);
            }
        }

        internal static void Send(uint[] clientIds, string messageType, string channelName, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            if (netManager.NetworkConfig.EncryptedChannelsHashSet.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return;
            }

            int sizeOfStream = 6;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(false);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
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
                    byte error;
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channel, stream.GetBuffer(), sizeOfStream, out error);
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

            //2 bytes for messageType, 2 bytes for buffer length and one byte for target bool
            int sizeOfStream = 6;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(false);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
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
                    byte error;
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channel, stream.GetBuffer(), sizeOfStream, out error);
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

            //2 bytes for messageType, 2 bytes for buffer length and one byte for target bool
            int sizeOfStream = 6;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(false);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
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
                    byte error;
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channel, stream.GetBuffer(), sizeOfStream, out error);
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

            //2 bytes for messageType, 2 bytes for buffer length and one byte for target bool
            int sizeOfStream = 5;
            if (networkId != null)
                sizeOfStream += 4;
            if (orderId != null)
                sizeOfStream += 2;
            sizeOfStream += data.Length;

            using (MemoryStream stream = new MemoryStream(sizeOfStream))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(MessageManager.messageTypes[messageType]);
                    writer.Write(networkId != null);
                    if (networkId != null)
                        writer.Write(networkId.Value);
                    if (orderId != null)
                        writer.Write(orderId.Value);
                    writer.Write(false);
                    writer.Write((ushort)data.Length);
                    writer.Write(data);
                }
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
                    byte error;
                    NetworkTransport.QueueMessageForSending(netId.HostId, netId.ConnectionId, channel, stream.GetBuffer(), sizeOfStream, out error);
                }
            }
        }
    }
}
