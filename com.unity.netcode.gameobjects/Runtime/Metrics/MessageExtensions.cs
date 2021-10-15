namespace Unity.Netcode
{
    internal static class MessageUtil
    {
        public static int TotalMessageSize(int size)
        {
            unsafe
            {
                return sizeof(MessageHeader) + size;
            }
        }
    }
}
