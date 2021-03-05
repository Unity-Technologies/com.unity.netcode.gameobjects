using MLAPI.Logging;
using MLAPI.Serialization;
using MLAPI.Configuration;
using MLAPI.Serialization.Pooled;

namespace MLAPI.Internal
{
    internal static class MessagePacker
    {
        // This method is responsible for unwrapping a message, that is extracting the messagebody.
        internal static NetworkBuffer UnwrapMessage(NetworkBuffer inputBuffer, out byte messageType)
        {
            using (var inputHeaderReader = PooledNetworkReader.Get(inputBuffer))
            {
                if (inputBuffer.Length < 1)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("The incoming message was too small");
                    messageType = NetworkConstants.INVALID;

                    return null;
                }

                messageType = inputHeaderReader.ReadByteDirect();

                // The input stream is now ready to be read from. It's "safe" and has the correct position
                return inputBuffer;
            }
        }

        internal static NetworkBuffer WrapMessage(byte messageType, NetworkBuffer messageBody)
        {
            var outStream = PooledNetworkBuffer.Get();

            using (var outWriter = PooledNetworkWriter.Get(outStream))
            {
                outWriter.WriteByte(messageType);
                outStream.Write(messageBody.GetBuffer(), 0, (int)messageBody.Length);
            }

            return outStream;
        }
    }
}