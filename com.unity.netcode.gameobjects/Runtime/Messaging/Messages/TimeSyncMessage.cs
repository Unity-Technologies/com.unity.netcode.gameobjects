namespace Unity.Netcode
{
    internal struct TimeSyncMessage : INetworkMessage
    {
        public int Tick;

        public void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(this);
        }

        public static void Receive(FastBufferReader reader, in NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return;
            }
            reader.ReadValueSafe(out TimeSyncMessage message);
            message.Handle(context.SenderId, networkManager);
        }

        public void Handle(ulong senderId, NetworkManager networkManager)
        {
            var time = new NetworkTime(networkManager.NetworkTickSystem.TickRate, Tick);
            networkManager.NetworkTimeSystem.Sync(time.Time, networkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(senderId) / 1000d);
        }
    }
}
