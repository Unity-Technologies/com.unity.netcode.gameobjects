namespace Unity.Netcode
{
    internal struct NamedMessage : INetworkMessage
    {
        public ulong Hash;
        public FastBufferWriter Data;

        public unsafe void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(Hash);
            writer.WriteBytesSafe(Data.GetUnsafePtr(), Data.Length);
        }

        public static void Receive(FastBufferReader reader, in NetworkContext context)
        {
            var message = new NamedMessage();
            reader.ReadValueSafe(out message.Hash);

            ((NetworkManager)context.SystemOwner).CustomMessagingManager.InvokeNamedMessage(message.Hash, context.SenderId, reader);
        }
    }
}
