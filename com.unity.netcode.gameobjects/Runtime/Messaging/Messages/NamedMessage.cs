namespace Unity.Netcode
{
    internal struct NamedMessage : INetworkMessage
    {
        public ulong Hash;
        public FastBufferWriter SendData;

        private FastBufferReader m_ReceiveData;

        public unsafe void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(Hash);
            writer.WriteBytesSafe(SendData.GetUnsafePtr(), SendData.Length);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            reader.ReadValueSafe(out Hash);
            m_ReceiveData = reader;
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            ((NetworkManager)context.SystemOwner).CustomMessagingManager.InvokeNamedMessage(Hash, context.SenderId, m_ReceiveData, context.SerializedHeaderSize);
        }
    }
}
