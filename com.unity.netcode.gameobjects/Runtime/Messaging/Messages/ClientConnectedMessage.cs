namespace Unity.Netcode
{
    internal struct ClientConnectedMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        public int Version => 0;

        public ulong ClientId;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            BytePacker.WriteValueBitPacked(writer, ClientId);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }
            ByteUnpacker.ReadValueBitPacked(reader, out ClientId);
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            networkManager.ConnectionManager.ConnectedClientIds.Add(ClientId);
            if (networkManager.IsConnectedClient)
            {
                networkManager.ConnectionManager.InvokeOnPeerConnectedCallback(ClientId);
            }
        }
    }
}
