using System.Collections.Generic;
using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.NetworkingManagerComponents.Cryptography;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static partial class InternalMessageHandler
    {
        internal static byte[] FinalMessageBuffer;
        internal static void PassthroughSend(uint targetId, uint sourceId, ushort messageType, int channelId, byte[] data, uint? networkId = null, ushort? orderId = null)
        {
            if (netManager.isHost && targetId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
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
                netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), channelId, false, out error);
            }
        }

        //RETURNS IF IT SUCCEDED OR FAILED BECAUSE OF NON-OBSERVER. ANY OTHER FAIL WILL RETURN TRUE
        internal static bool Send(uint clientId, string messageType, string channelName, byte[] data, uint? fromNetId, uint? networkId = null, ushort? orderId = null, bool skipQueue = false)
        {
            uint targetClientId = clientId;
            if (netManager.isHost && targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
            {
                //Don't invoke the message on our own machine. Instant stack overflow.
                Debug.LogWarning("MLAPI: Cannot send message to own client");
                return true;
            }
            else if (targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
            {
                //Client trying to send data to host
                targetClientId = netManager.NetworkConfig.NetworkTransport.ServerNetId;
            }
            //If we respect the observers, and the message is targeted (networkId != null) and the targetedNetworkId isnt observing the receiver. Then we return
            if (netManager.isServer && fromNetId != null && !SpawnManager.spawnedObjects[fromNetId.Value].observers.Contains(clientId))
                return false;

            bool isPassthrough = (!netManager.isServer && clientId != netManager.NetworkConfig.NetworkTransport.ServerNetId && netManager.NetworkConfig.AllowPassthroughMessages);
            if (isPassthrough && !netManager.NetworkConfig.PassthroughMessageHashSet.Contains(MessageManager.messageTypes[messageType]))
            {
                Debug.LogWarning("MLAPI: The The MessageType " + messageType + " is not registered as an allowed passthrough message type.");
                return true;
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

                if (isPassthrough)
                    targetClientId = netManager.NetworkConfig.NetworkTransport.ServerNetId;

                writer.Finalize(ref FinalMessageBuffer);

                byte error;
                if (skipQueue)
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), MessageManager.channels[channelName], true, out error);
                else
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), MessageManager.channels[channelName], false, out error);

                return true;
            }
        }

        internal static void Send(uint[] clientIds, string messageType, string channelName, byte[] data, uint? fromNetId, uint? networkId = null, ushort? orderId = null)
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
                    uint targetClientId = clientIds[i];
                    if (netManager.isHost && targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Client trying to send data to host
                        targetClientId = netManager.NetworkConfig.NetworkTransport.ServerNetId;
                    }

                    //If we respect the observers, and the message is targeted (networkId != null) and the targetedNetworkId isnt observing the receiver. Then we continue
                    if (netManager.isServer && fromNetId != null && !SpawnManager.spawnedObjects[fromNetId.Value].observers.Contains(clientIds[i]))
                        continue;

                    writer.Finalize(ref FinalMessageBuffer);

                    byte error;
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), channel, false, out error);
                }
            }
        }

        internal static void Send(List<uint> clientIds, string messageType, string channelName, byte[] data, uint? fromNetId, uint? networkId = null, ushort? orderId = null)
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
                    uint targetClientId = clientIds[i];
                    if (netManager.isHost && targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Client trying to send data to host
                        targetClientId = netManager.NetworkConfig.NetworkTransport.ServerNetId;
                    }

                    //If we respect the observers, and the message is targeted (networkId != null) and the targetedNetworkId isnt observing the receiver. Then we continue
                    if (netManager.isServer && fromNetId != null && !SpawnManager.spawnedObjects[fromNetId.Value].observers.Contains(clientIds[i]))
                        continue;

                    writer.Finalize(ref FinalMessageBuffer);

                     byte error;
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), channel, false, out error);
                }
            }
        }

        private static List<uint> failedObservers = new List<uint>();
        //RETURNS THE CLIENTIDS WHICH WAS NOT BEING OBSERVED
        internal static ref List<uint> Send(string messageType, string channelName, byte[] data, uint? fromNetId,  uint? networkId = null, ushort? orderId = null)
        {
            failedObservers.Clear();
            if (netManager.connectedClients.Count == 0)
                return ref failedObservers;
            if (netManager.NetworkConfig.EncryptedChannels.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return ref failedObservers;
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
                    uint targetClientId = pair.Key;
                    if (netManager.isHost && targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Client trying to send data to host
                        targetClientId = netManager.NetworkConfig.NetworkTransport.ServerNetId;
                    }

                    //If we respect the observers, and the message is targeted (networkId != null) and the targetedNetworkId isnt observing the receiver. Then we continue
                    if (netManager.isServer && fromNetId != null && !SpawnManager.spawnedObjects[fromNetId.Value].observers.Contains(pair.Key))
                    {
                        failedObservers.Add(pair.Key);
                        continue;
                    }

                    writer.Finalize(ref FinalMessageBuffer);

                    byte error;
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), channel, false, out error);
                }
                return ref failedObservers;
            }
        }

        //RETURNS THE CLIENTIDS WHICH WAS NOT BEING OBSERVED
        internal static ref List<uint> Send(string messageType, string channelName, byte[] data, uint clientIdToIgnore, uint? fromNetId, uint? networkId = null, ushort? orderId = null)
        {
            failedObservers.Clear();
            if (netManager.NetworkConfig.EncryptedChannels.Contains(channelName))
            {
                Debug.LogWarning("MLAPI: Cannot send messages over encrypted channel to multiple clients.");
                return ref failedObservers;
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

                    uint targetClientId = pair.Key;
                    if (netManager.isHost && targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Don't invoke the message on our own machine. Instant stack overflow.
                        continue;
                    }
                    else if (targetClientId == netManager.NetworkConfig.NetworkTransport.HostDummyId)
                    {
                        //Client trying to send data to host
                        targetClientId = netManager.NetworkConfig.NetworkTransport.ServerNetId;
                    }

                    //If we respect the observers, and the message is targeted (networkId != null) and the targetedNetworkId isnt observing the receiver. Then we continue
                    if (netManager.isServer && fromNetId != null && !SpawnManager.spawnedObjects[fromNetId.Value].observers.Contains(pair.Key))
                    {
                        failedObservers.Add(pair.Key);
                        continue;
                    }

                    writer.Finalize(ref FinalMessageBuffer);

                    byte error;
                    netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(targetClientId, ref FinalMessageBuffer, (int)writer.GetFinalizeSize(), channel, false, out error);
                }
                return ref failedObservers;
            }
        }
    }
}
