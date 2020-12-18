using MLAPI.Logging;
using MLAPI.Serialization;
using System;
using MLAPI.Configuration;
using MLAPI.Serialization.Pooled;
#if !DISABLE_CRYPTOGRAPHY
using System.Security.Cryptography;
#endif
using MLAPI.Security;

namespace MLAPI.Internal
{
    internal static class MessagePacker
    {
        private static readonly byte[] IV_BUFFER = new byte[16];
        private static readonly byte[] HMAC_BUFFER = new byte[32];
        private static readonly byte[] HMAC_PLACEHOLDER = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // This method is responsible for unwrapping a message, that is extracting the messagebody.
        // Could include decrypting and/or authentication.
        internal static BitStream UnwrapMessage(BitStream inputStream, ulong clientId, out byte messageType, out SecuritySendFlags security)
        {
            using (PooledBitReader inputHeaderReader = PooledBitReader.Get(inputStream))
            {
                try
                {
                    if (inputStream.Length < 1)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("The incoming message was too small");
                        messageType = MLAPIConstants.INVALID;
                        security = SecuritySendFlags.None;
                        return null;
                    }

                    bool isEncrypted = inputHeaderReader.ReadBit();
                    bool isAuthenticated = inputHeaderReader.ReadBit();

                    if (isEncrypted && isAuthenticated) security = SecuritySendFlags.Encrypted | SecuritySendFlags.Authenticated;
                    else if (isEncrypted) security = SecuritySendFlags.Encrypted;
                    else if (isAuthenticated) security = SecuritySendFlags.Authenticated;
                    else security = SecuritySendFlags.None;


#if !DISABLE_CRYPTOGRAPHY
                    if (isEncrypted || isAuthenticated)
                    {
                        if (!NetworkingManager.Singleton.NetworkConfig.EnableEncryption)
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Got a encrypted and/or authenticated message but key exchange (\"encryption\") was not enabled");
                            messageType = MLAPIConstants.INVALID;
                            return null;
                        }

                        // Skip last bits in first byte
                        inputHeaderReader.SkipPadBits();

                        if (isAuthenticated)
                        {
                            long hmacStartPos = inputStream.Position;

                            int readHmacLength = inputStream.Read(HMAC_BUFFER, 0, HMAC_BUFFER.Length);

                            if (readHmacLength != HMAC_BUFFER.Length)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("HMAC length was invalid");
                                messageType = MLAPIConstants.INVALID;
                                return null;
                            }

                            // Now we have read the HMAC, we need to set the hmac in the input to 0s to perform the HMAC.
                            inputStream.Position = hmacStartPos;
                            inputStream.Write(HMAC_PLACEHOLDER, 0, HMAC_PLACEHOLDER.Length);

                            byte[] key = NetworkingManager.Singleton.IsServer ? CryptographyHelper.GetClientKey(clientId) : CryptographyHelper.GetServerKey();

                            if (key == null)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Failed to grab key");
                                messageType = MLAPIConstants.INVALID;
                                return null;
                            }

                            using (HMACSHA256 hmac = new HMACSHA256(key))
                            {
                                byte[] computedHmac = hmac.ComputeHash(inputStream.GetBuffer(), 0, (int)inputStream.Length);


                                if (!CryptographyHelper.ConstTimeArrayEqual(computedHmac, HMAC_BUFFER))
                                {
                                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Received HMAC did not match the computed HMAC");
                                    messageType = MLAPIConstants.INVALID;
                                    return null;
                                }
                            }
                        }

                        if (isEncrypted)
                        {
                            int ivRead = inputStream.Read(IV_BUFFER, 0, IV_BUFFER.Length);

                            if (ivRead != IV_BUFFER.Length)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("Invalid IV size");
                                messageType = MLAPIConstants.INVALID;
                                return null;
                            }

                            PooledBitStream outputStream = PooledBitStream.Get();

                            using (RijndaelManaged rijndael = new RijndaelManaged())
                            {
                                rijndael.IV = IV_BUFFER;
                                rijndael.Padding = PaddingMode.PKCS7;

                                byte[] key = NetworkingManager.Singleton.IsServer ? CryptographyHelper.GetClientKey(clientId) : CryptographyHelper.GetServerKey();

                                if (key == null)
                                {
                                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Failed to grab key");
                                    messageType = MLAPIConstants.INVALID;
                                    return null;
                                }

                                rijndael.Key = key;

                                using (CryptoStream cryptoStream = new CryptoStream(outputStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(inputStream.GetBuffer(), (int)inputStream.Position, (int)(inputStream.Length - inputStream.Position));
                                }

                                outputStream.Position = 0;

                                if (outputStream.Length == 0)
                                {
                                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("The incoming message was too small");
                                    messageType = MLAPIConstants.INVALID;
                                    return null;
                                }

                                int msgType = outputStream.ReadByte();
                                messageType = msgType == -1 ? MLAPIConstants.INVALID : (byte)msgType;
                            }

                            return outputStream;
                        }
                        else
                        {
                            if (inputStream.Length - inputStream.Position <= 0)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("The incoming message was too small");
                                messageType = MLAPIConstants.INVALID;
                                return null;
                            }

                            int msgType = inputStream.ReadByte();
                            messageType = msgType == -1 ? MLAPIConstants.INVALID : (byte)msgType;
                            return inputStream;
                        }
                    }
                    else
                    {
#endif
                        // 6 is because ...
                        messageType = inputHeaderReader.ReadByteBits(6);
                        // The input stream is now ready to be read from. It's "safe" and has the correct position
                        return inputStream;
#if !DISABLE_CRYPTOGRAPHY
                    }
#endif
                }
                catch (Exception e)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("Error while unwrapping headers");
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError(e.ToString());

                    security = SecuritySendFlags.None;
                    messageType = MLAPIConstants.INVALID;
                    return null;
                }
            }
        }

        internal static BitStream WrapMessage(byte messageType, ulong clientId, BitStream messageBody, SecuritySendFlags flags)
        {
            try
            {
                bool encrypted = ((flags & SecuritySendFlags.Encrypted) == SecuritySendFlags.Encrypted) && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;
                bool authenticated = (flags & SecuritySendFlags.Authenticated) == SecuritySendFlags.Authenticated && NetworkingManager.Singleton.NetworkConfig.EnableEncryption;

                PooledBitStream outStream = PooledBitStream.Get();

                using (PooledBitWriter outWriter = PooledBitWriter.Get(outStream))
                {
                    outWriter.WriteBit(encrypted);
                    outWriter.WriteBit(authenticated);

#if !DISABLE_CRYPTOGRAPHY
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
                                rijndael.Padding = PaddingMode.PKCS7;

                                byte[] key = NetworkingManager.Singleton.IsServer ? CryptographyHelper.GetClientKey(clientId) : CryptographyHelper.GetServerKey();

                                if (key == null)
                                {
                                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Failed to grab key");
                                    return null;
                                }

                                rijndael.Key = key;

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
                            byte[] key = NetworkingManager.Singleton.IsServer ? CryptographyHelper.GetClientKey(clientId) : CryptographyHelper.GetServerKey();

                            if (key == null)
                            {
                                if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError("Failed to grab key");
                                return null;
                            }

                            using (HMACSHA256 hmac = new HMACSHA256(key))
                            {
                                byte[] computedHmac = hmac.ComputeHash(outStream.GetBuffer(), 0, (int)outStream.Length);

                                outStream.Position = hmacWritePos;
                                outStream.Write(computedHmac, 0, computedHmac.Length);
                            }
                        }
                    }
                    else
                    {
#endif
                        outWriter.WriteBits(messageType, 6);
                        outStream.Write(messageBody.GetBuffer(), 0, (int)messageBody.Length);
#if !DISABLE_CRYPTOGRAPHY
                    }
#endif
                }

                return outStream;
            }
            catch (Exception e)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("Error while wrapping headers");
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError(e.ToString());

                return null;
            }
        }
    }
}
