namespace Unity.Netcode.Messages
{
    internal struct NamedMessage : INetworkMessage
    {
        public ulong Hash;
        public FastBufferWriter Data;

        public unsafe void Serialize(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe(Hash);
            writer.WriteBytesSafe(Data.GetUnsafePtr(), Data.Length);
        }

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            var message = new NamedMessage();
            reader.ReadValueSafe(out message.Hash);

            ((NetworkManager)context.SystemOwner).CustomMessagingManager.InvokeNamedMessage(message.Hash, context.SenderId, ref reader);
        }
    }
}
