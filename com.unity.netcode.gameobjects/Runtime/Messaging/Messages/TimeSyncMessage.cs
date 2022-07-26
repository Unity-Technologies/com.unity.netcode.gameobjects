namespace Unity.Netcode
{
    internal struct TimeSyncMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        public int Tick;

        public void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(this);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }
            reader.ReadValueSafe(out this);
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var time = new NetworkTime(networkManager.NetworkTickSystem.TickRate, Tick);
            networkManager.NetworkTimeSystem.Sync(time.Time, networkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(context.SenderId) / 1000d);
        }
    }
}
