using System.IO;
using System.Security.Cryptography;
using MLAPI.Data;
using MLAPI.Profiling;
using MLAPI.Serialization;

namespace MLAPI.Internal
{
    internal static partial class InternalMessageHandler
    {
        internal static void Send(uint clientId, byte messageType, string channelName, Stream messageStream, SecuritySendFlags securityOptions, bool skipQueue = false)
        {
            if (NetworkingManager.singleton.isServer && clientId == NetworkingManager.singleton.ServerClientId) return;
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    bool encrypted = ((securityOptions & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && netManager.NetworkConfig.EnableEncryption;
                    bool authenticated = ((securityOptions & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && netManager.NetworkConfig.EnableEncryption;
                    writer.WriteBit(encrypted);
                    writer.WriteBit(authenticated);
                    
                    if (encrypted || authenticated)
                    {
                        writer.WritePadBits();

                        long hmacPosition = stream.Position; // Save the position where the HMAC should be written.
                        if (authenticated) stream.Position += 32; // Skip 32 bytes. These will be replaced later on by the HMAC.

                        if (encrypted)
                        {
                            using (RijndaelManaged rijndael = new RijndaelManaged())
                            {
                                rijndael.Key = netManager.isServer ? netManager.ConnectedClients[clientId].AesKey : netManager.clientAesKey;
                                rijndael.GenerateIV();
                                rijndael.Padding = PaddingMode.PKCS7;
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

                        if (authenticated)
                        {
                            if (!encrypted) writer.WriteByte(messageType); // If we are not using encryption, write the byte. Note that the current position in the stream is just after the HMAC.
                            
                            stream.Position = hmacPosition; // Set the position to where the HMAC should be written.
                            using (HMACSHA256 hmac = new HMACSHA256(netManager.isServer ? netManager.ConnectedClients[clientId].AesKey : netManager.clientAesKey))
                            {
                                writer.WriteByteArray(hmac.ComputeHash(stream.GetBuffer(), (32 + 1), (int)stream.Length - (32 + 1)), 32);
                            }
                            stream.CopyFrom(messageStream);
                        }
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

        internal static void Send(byte messageType, string channelName, Stream messageStream, SecuritySendFlags securityOptions)
        {
            bool encrypted = ((securityOptions & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && netManager.NetworkConfig.EnableEncryption;
            bool authenticated = ((securityOptions & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && netManager.NetworkConfig.EnableEncryption;
            
            if (authenticated || encrypted)
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
                    writer.WriteBool(false); // Encryption
                    writer.WriteBool(false); // Authentication
                    
                    writer.WriteBits(messageType, 6);
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
        
        internal static void Send(byte messageType, string channelName, uint clientIdToIgnore, Stream messageStream, SecuritySendFlags securityOptions)
        {
            bool encrypted = ((securityOptions & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && netManager.NetworkConfig.EnableEncryption;
            bool authenticated = ((securityOptions & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated) && netManager.NetworkConfig.EnableEncryption;
            
            if (authenticated || encrypted)
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
                    writer.WriteBool(false); // Encryption
                    writer.WriteBool(false); // Authentication
                    
                    writer.WriteBits(messageType, 6);
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
