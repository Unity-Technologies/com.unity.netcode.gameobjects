namespace Unity.Netcode
{
    internal static class MessageUtility
    {
        public static int GetTotalMessageSizeFromPayloadSize(int payloadSize)
            => payloadSize + FastBufferWriter.GetWriteSize<MessageHeader>();
    }
}
