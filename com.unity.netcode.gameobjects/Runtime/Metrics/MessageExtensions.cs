namespace Unity.Netcode
{
    internal static class MessageUtil
    {
        public static int GetTotalMessageSizeFromPayloadSize(int payloadSize)
        {
            unsafe
            {
                return sizeof(MessageHeader) + payloadSize;
            }
        }
    }
}
