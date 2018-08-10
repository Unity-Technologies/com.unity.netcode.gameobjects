using System.IO;
using System.Security.Cryptography;
using MLAPI.Data;
using MLAPI.Profiling;
using MLAPI.Serialization;

namespace MLAPI.Internal
{
    internal static partial class InternalMessageHandler
    {
        internal static void Send(uint clientId, byte messageType, string channelName, Stream messageStream, InternalSecuritySendOptions securityOptions, bool skipQueue = false)
        {
            if (NetworkingManager.singleton.isServer && clientId == NetworkingManager.singleton.ServerClientId) return;
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteBool(securityOptions.encrypted);
                    writer.WriteBool(securityOptions.authenticated);
                    if (securityOptions.encrypted && netManager.NetworkConfig.EnableEncryption)
                    {
                        writer.WritePadBits();
                        using (RijndaelManaged rijndael = new RijndaelManaged())
                        {
                            rijndael.Key = netManager.isServer ? netManager.ConnectedClients[clientId].AesKey : netManager.clientAesKey;
                            rijndael.GenerateIV();
                            writer.WriteByteArray(rijndael.IV, 16);
                            using (CryptoStream cryptoStream = new CryptoStream(stream, rijndael.CreateEncryptor(), CryptoStreamMode.Write))
                            {
                                using (PooledBitWriter encryptedWriter = PooledBitWriter.Get(cryptoStream))
                                {
                                    encryptedWriter.WriteByte(messageType);
                                    // Copy data
                                    messageStream.Position = 0;
                                    int messageByte;
                                    while ((messageByte = messageStream.ReadByte()) != -1) encryptedWriter.WriteByte((byte)messageByte);
                                }
                            }
                        }
                    }
                    else if (securityOptions.authenticated && netManager.NetworkConfig.EnableEncryption)
                    {
                        writer.WritePadBits();
                        
                        using (HMACSHA256 hmac = new HMACSHA256(netManager.isServer ? netManager.ConnectedClients[clientId].AesKey : netManager.clientAesKey))
                        {
                            writer.WriteByteArray(hmac.ComputeHash(messageStream), 32);
                        }
                        writer.WriteByte(messageType);
                        stream.CopyFrom(messageStream);
                    }
                    else
                    {
                        writer.WriteBits(messageType, 6);
                        stream.CopyFrom(messageStream);
                    }

                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    byte error;
                    if (skipQueue)
                        netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], true, out error);
                    else
                        netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(clientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                    NetworkProfiler.EndEvent();
                }
            }
        }

        internal static void Send(byte messageType, string channelName, Stream messageStream, InternalSecuritySendOptions securityOptions)
        {
            if (netManager.NetworkConfig.EnableEncryption && (securityOptions.authenticated || securityOptions.encrypted))
            {
                for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                {
                    Send(netManager.ConnectedClientsList[i].ClientId, messageType, channelName, messageStream, securityOptions);
                }
                return;
            }
            
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteByte(messageType);
                    stream.CopyFrom(messageStream);

                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                    {
                        if (NetworkingManager.singleton.isServer && netManager.ConnectedClientsList[i].ClientId == NetworkingManager.singleton.ServerClientId) continue;
                        byte error;
                        netManager.NetworkConfig.NetworkTransport.QueueMessageForSending(netManager.ConnectedClientsList[i].ClientId, stream.GetBuffer(), (int)stream.Length, MessageManager.channels[channelName], false, out error);
                    }
                    NetworkProfiler.EndEvent();
                }
            }
        }
        
        internal static void Send(byte messageType, string channelName, uint clientIdToIgnore, Stream messageStream, InternalSecuritySendOptions securityOptions)
        {
            if (netManager.NetworkConfig.EnableEncryption && (securityOptions.authenticated || securityOptions.encrypted))
            {
                for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                {
                    if (netManager.ConnectedClientsList[i].ClientId == clientIdToIgnore) continue;
                    Send(netManager.ConnectedClientsList[i].ClientId, messageType, channelName, messageStream, securityOptions);
                }
                return;
            }
            
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteByte(messageType);
                    stream.CopyFrom(messageStream);

                    NetworkProfiler.StartEvent(TickType.Send, (uint)stream.Length, channelName, MLAPIConstants.MESSAGE_NAMES[messageType]);
                    for (int i = 0; i < netManager.ConnectedClientsList.Count; i++)
                    {
                        if (netManager.ConnectedClientsList[i].ClientId == clientIdToIgnore ||
                            (NetworkingManager.singleton.isServer && netManager.ConnectedClientsList[i].ClientId == NetworkingManager.singleton.ServerClientId))
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
