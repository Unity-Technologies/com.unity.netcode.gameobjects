using MLAPI.Data;
using MLAPI.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace MLAPI.Internal
{
    internal static class MessageManager
    {
        internal static readonly Dictionary<string, int> channels = new Dictionary<string, int>();
        internal static readonly Dictionary<int, string> reverseChannels = new Dictionary<int, string>();

        private static readonly byte[] IV_BUFFER = new byte[16];
        private static readonly byte[] HMAC_BUFFER = new byte[32];
        private static readonly byte[] HMAC_PLACEHOLDER = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        internal static BitStream UnwrapMessage(BitStream inputStream, uint clientId, out byte messageType)
        {
            using (PooledBitReader inputHeaderReader = PooledBitReader.Get(inputStream))
            {
                bool isEncrypted = inputHeaderReader.ReadBit();
                bool isAuthenticated = inputHeaderReader.ReadBit();

                if (isEncrypted || isAuthenticated)
                {
                    // Skip last bits in first byte
                    inputHeaderReader.SkipPadBits();

                    if (isAuthenticated)
                    {
                        long hmacStartPos = inputStream.Position;

                        int readHmacLength = inputStream.Read(HMAC_BUFFER, 0, HMAC_BUFFER.Length);

                        if (readHmacLength != HMAC_BUFFER.Length)
                        {
                            messageType = MLAPIConstants.INVALID;
                            return null;
                        }

                        // Now we have read the HMAC, we need to set the hmac in the input to 0s to perform the HMAC.
                        inputStream.Position = hmacStartPos;
                        inputStream.Write(HMAC_PLACEHOLDER, 0, HMAC_PLACEHOLDER.Length);

                        using (HMACSHA256 hmac = new HMACSHA256(NetworkingManager.singleton.isServer ? ((NetworkingManager.singleton.ConnectedClients.ContainsKey(clientId) ? NetworkingManager.singleton.ConnectedClients[clientId].AesKey : NetworkingManager.singleton.PendingClients[clientId].AesKey)) : NetworkingManager.singleton.clientAesKey))
                        {
                            byte[] computedHmac = hmac.ComputeHash(inputStream.GetBuffer(), 0, (int)inputStream.Length);

                            for (int i = 0; i < computedHmac.Length; i++)
                            {
                                if (computedHmac[i] != HMAC_BUFFER[i])
                                {
                                    messageType = MLAPIConstants.INVALID;
                                    return null;
                                }
                            }
                        }
                    }

                    if (isEncrypted)
                    {
                        inputStream.Read(IV_BUFFER, 0, IV_BUFFER.Length);
                        PooledBitStream outputStream = PooledBitStream.Get();

                        using (RijndaelManaged rijndael = new RijndaelManaged())
                        {
                            rijndael.IV = IV_BUFFER;
                            rijndael.Key = NetworkingManager.singleton.isServer ? ((NetworkingManager.singleton.ConnectedClients.ContainsKey(clientId) ? NetworkingManager.singleton.ConnectedClients[clientId].AesKey : NetworkingManager.singleton.PendingClients[clientId].AesKey)) : NetworkingManager.singleton.clientAesKey;
                            rijndael.Padding = PaddingMode.PKCS7;

                            using (CryptoStream cryptoStream = new CryptoStream(outputStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(inputStream.GetBuffer(), (int)inputStream.Position, (int)(inputStream.Length - inputStream.Position));
                            }

                            outputStream.Position = 0;
                            int msgType = outputStream.ReadByte();
                            messageType = msgType == -1 ? MLAPIConstants.INVALID : (byte)msgType;
                        }

                        return outputStream;
                    }
                    else
                    {
                        int msgType = inputStream.ReadByte();
                        messageType = msgType == -1 ? MLAPIConstants.INVALID : (byte)msgType;
                        return inputStream;
                    }
                }
                else
                {
                    messageType = inputHeaderReader.ReadByteBits(6);
                    // The input stream is now ready to be read from. It's "safe" and has the correct position
                    return inputStream;
                }
            }
        }

        internal static BitStream WrapMessage(byte messageType, uint clientId, BitStream messageBody, SecuritySendFlags flags)
        {
            bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && NetworkingManager.singleton.NetworkConfig.EnableEncryption;
            bool authenticated = (flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated && NetworkingManager.singleton.NetworkConfig.EnableEncryption;

            PooledBitStream outStream = PooledBitStream.Get();

            using (PooledBitWriter outWriter = PooledBitWriter.Get(outStream))
            {
                outWriter.WriteBit(encrypted);
                outWriter.WriteBit(authenticated);

                if (authenticated || encrypted)
                {
                    outWriter.WritePadBits();
                    long hmacWritePos = outStream.Position;

                    if (authenticated) outStream.Write(HMAC_PLACEHOLDER, 0, HMAC_PLACEHOLDER.Length);

                    if (encrypted)
                    {
                        using (RijndaelManaged rijndael = new RijndaelManaged())
                        {
                            rijndael.GenerateIV();
                            rijndael.Key = NetworkingManager.singleton.isServer ? ((NetworkingManager.singleton.ConnectedClients.ContainsKey(clientId) ? NetworkingManager.singleton.ConnectedClients[clientId].AesKey : NetworkingManager.singleton.PendingClients[clientId].AesKey)) : NetworkingManager.singleton.clientAesKey;
                            rijndael.Padding = PaddingMode.PKCS7;

                            outStream.Write(rijndael.IV);

                            using (CryptoStream encryptionStream = new CryptoStream(outStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write))
                            {
                                encryptionStream.WriteByte(messageType);
                                encryptionStream.Write(messageBody.GetBuffer(), 0, (int)messageBody.Length);
                            }
                        }
                    }
                    else
                    {
                        outStream.WriteByte(messageType);                       
                        outStream.Write(messageBody.GetBuffer(), 0, (int)messageBody.Length);
                    }

                    if (authenticated)
                    {
                        using (HMACSHA256 hmac = new HMACSHA256(NetworkingManager.singleton.isServer ? ((NetworkingManager.singleton.ConnectedClients.ContainsKey(clientId) ? NetworkingManager.singleton.ConnectedClients[clientId].AesKey : NetworkingManager.singleton.PendingClients[clientId].AesKey)) : NetworkingManager.singleton.clientAesKey))
                        {
                            byte[] computedHmac = hmac.ComputeHash(outStream.GetBuffer(), 0, (int)outStream.Length);

                            outStream.Position = hmacWritePos;
                            outStream.Write(computedHmac, 0, computedHmac.Length);
                        }
                    }
                }
                else
                {
                    outWriter.WriteBits(messageType, 6);
                    outStream.Write(messageBody.GetBuffer(), 0, (int)messageBody.Length);
                }
            }

            return outStream;
        }
    }
}
