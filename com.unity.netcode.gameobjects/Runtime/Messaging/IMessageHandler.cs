namespace Unity.Netcode
{
    public interface IMessageHandler
    {
        public void HandleMessage(in MessageHeader header, ref FastBufferReader reader, ulong senderId,
            float timestamp);
    }
}