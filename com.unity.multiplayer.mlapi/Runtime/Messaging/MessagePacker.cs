using MLAPI.Logging;
using MLAPI.Serialization;
using MLAPI.Configuration;
using MLAPI.Serialization.Pooled;

namespace MLAPI.Internal
{
    internal class MessagePacker
    {
        private NetworkManager m_NetworkManager;

        internal MessagePacker(NetworkManager manager) { m_NetworkManager = manager; }

        // This method is responsible for unwrapping a message, that is extracting the messagebody.
        internal NetworkBuffer UnwrapMessage(NetworkBuffer inputBuffer, out byte messageType)
        {
            using (var inputHeaderReader = m_NetworkManager.NetworkReaderPool.GetReader(inputBuffer))
            {
                if (inputBuffer.Length < 1)
                {
                    if (NetworkManager.LogLevelStatic <= LogLevel.Normal) NetworkLog.LogErrorStatic("The incoming message was too small");
                    messageType = NetworkConstants.INVALID;

                    return null;
                }

                messageType = inputHeaderReader.ReadByteDirect();

                // The input stream is now ready to be read from. It's "safe" and has the correct position
                return inputBuffer;
            }
        }

        internal NetworkBuffer WrapMessage(byte messageType, NetworkBuffer messageBody)
        {
            var outStream = PooledNetworkBuffer.Get();

            using (var outWriter = m_NetworkManager.NetworkWriterPool.GetWriter(outStream))
            {
                outWriter.WriteByte(messageType);
                outStream.Write(messageBody.GetBuffer(), 0, (int)messageBody.Length);
            }

            return outStream;
        }
    }
}
