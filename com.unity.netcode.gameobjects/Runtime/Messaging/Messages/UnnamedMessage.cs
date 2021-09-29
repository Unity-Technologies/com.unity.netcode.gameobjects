namespace Unity.Netcode
{
    internal struct UnnamedMessage : INetworkMessage
    {
        public FastBufferWriter Data;

        public unsafe void Serialize(FastBufferWriter writer)
        {
            writer.WriteBytesSafe(Data.GetUnsafePtr(), Data.Length);
        }

        public static void Receive(FastBufferReader reader, in NetworkContext context)
        {
            ((NetworkManager)context.SystemOwner).CustomMessagingManager.InvokeUnnamedMessage(context.SenderId, reader);
        }
    }
}
