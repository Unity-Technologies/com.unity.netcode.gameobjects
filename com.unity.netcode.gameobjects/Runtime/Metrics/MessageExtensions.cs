namespace Unity.Netcode
{
    internal static class MessageUtil
    {
        public static int GetTotalMessageSizeFromPayloadSize(int payloadSize)
            => payloadSize + FastBufferWriter.GetWriteSize<MessageHeader>();
    }
}
