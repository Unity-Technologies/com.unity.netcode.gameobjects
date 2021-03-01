using MLAPI.Logging;
using MLAPI.Serialization;
using MLAPI.Configuration;
using MLAPI.Serialization.Pooled;

namespace MLAPI.Internal
{
    internal static class MessagePacker
    {
        // This method is responsible for unwrapping a message, that is extracting the messagebody.
        internal static NetworkStream UnwrapMessage(NetworkStream inputStream, out byte messageType)
        {
            using (var inputHeaderReader = PooledNetworkReader.Get(inputStream))
            {
                if (inputStream.Length < 1)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal) NetworkLog.LogError("The incoming message was too small");
                    messageType = MLAPIConstants.INVALID;
                    return null;
                }

                messageType = inputHeaderReader.ReadByteDirect();
                // The input stream is now ready to be read from. It's "safe" and has the correct position
                return inputStream;
            }
        }

        internal static NetworkStream WrapMessage(byte messageType, NetworkStream messageBody)
        {
            var outStream = PooledNetworkStream.Get();

            using (var outWriter = PooledNetworkWriter.Get(outStream))
            {
                outWriter.WriteByte(messageType);
                outStream.Write(messageBody.GetBuffer(), 0, (int)messageBody.Length);
            }

            return outStream;
        }
    }
}