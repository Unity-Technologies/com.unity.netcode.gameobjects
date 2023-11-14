namespace Unity.Netcode
{
    internal struct NamedMessage : INetworkMessage
    {
        public int Version => 0;

        public ulong Hash;
        public FastBufferWriter SendData;

        private FastBufferReader m_ReceiveData;

        public unsafe void Serialize(FastBufferWriter writer, int targetVersion)
        {
            writer.WriteValueSafe(Hash);
            writer.WriteBytesSafe(SendData.GetUnsafePtr(), SendData.Length);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            reader.ReadValueSafe(out Hash);
            m_ReceiveData = reader;
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.ShutdownInProgress && networkManager.CustomMessagingManager != null)
            {
                networkManager.CustomMessagingManager.InvokeNamedMessage(Hash, context.SenderId, m_ReceiveData, context.SerializedHeaderSize);
            }
        }
    }
}
