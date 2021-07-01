using MLAPI.Serialization;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;

namespace MLAPI.EditorTests
{
    internal static class MessagePacker
    {
        internal static NetworkBuffer WrapMessage(MessageQueueContainer.MessageType messageType, NetworkBuffer messageBody)
        {
            var outBuffer = PooledNetworkBuffer.Get();
            var outStream = PooledNetworkWriter.Get(outBuffer);
            {

                if (NetworkManager.Singleton.MessageQueueContainer.IsUsingBatching())
                {
                    // write the amounts of bytes that are coming up
                    MessageBatcher.PushLength((int)messageBody.Length, ref outStream);

                    // write the message to send
                    outStream.WriteBytes(messageBody.GetBuffer(), messageBody.Length);
                }
                outStream.WriteByte((byte)messageType);
            }
            outStream.Dispose();
            return outBuffer;
        }
    }
}
