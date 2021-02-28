using MLAPI.Logging;
using MLAPI.Serialization;
using System;
using MLAPI.Configuration;
using MLAPI.Serialization.Pooled;
using MLAPI.Security;

namespace MLAPI.Internal
{
    internal static class MessagePacker
    {
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

                    messageType = inputHeaderReader.ReadByteBits(6);
                    // The input stream is now ready to be read from. It's "safe" and has the correct position
                    return inputStream;
                }
                catch (Exception ex)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("Error while unwrapping headers");
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError(ex.ToString());

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
                bool encrypted = false; // TODO @mfatihmar: remove
                bool authenticated = false; // TODO @mfatihmar: remove

                PooledBitStream outStream = PooledBitStream.Get();

                using (PooledBitWriter outWriter = PooledBitWriter.Get(outStream))
                {
                    outWriter.WriteBit(encrypted);
                    outWriter.WriteBit(authenticated);

                    outWriter.WriteBits(messageType, 6);
                    outStream.Write(messageBody.GetBuffer(), 0, (int)messageBody.Length);
                }

                return outStream;
            }
            catch (Exception ex)
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("Error while wrapping headers");
                if (NetworkLog.CurrentLogLevel <= LogLevel.Error) NetworkLog.LogError(ex.ToString());

                return null;
            }
        }
    }
}
