namespace Unity.Netcode
{
    internal struct AnticipationTickSyncPingMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        public int Version => 0;

        public double Tick;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            writer.WriteValueSafe(Tick);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsServer)
            {
                return false;
            }
            reader.ReadValueSafe(out Tick);
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (networkManager.IsListening && !networkManager.ShutdownInProgress && networkManager.ConnectedClients.ContainsKey(context.SenderId))
            {
                var message = new AnticipationTickSyncPongMessage { Tick = Tick };
                networkManager.MessageManager.SendMessage(ref message, NetworkDelivery.Reliable, context.SenderId);
            }
        }
    }
    internal struct AnticipationTickSyncPongMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        public int Version => 0;

        public double Tick;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            writer.WriteValueSafe(Tick);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }
            reader.ReadValueSafe(out Tick);
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            networkManager.NetworkTickSystem.AnticipationTick = Tick;
        }
    }
}
