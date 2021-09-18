namespace Unity.Netcode
{
    internal struct UnnamedMessage : INetworkMessage
    {
        public FastBufferWriter Data;

        public unsafe void Serialize(ref FastBufferWriter writer)
        {
            writer.WriteBytesSafe(Data.GetUnsafePtr(), Data.Length);
        }

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            ((NetworkManager)context.SystemOwner).CustomMessagingManager.InvokeUnnamedMessage(context.SenderId, ref reader);
        }
    }
}
