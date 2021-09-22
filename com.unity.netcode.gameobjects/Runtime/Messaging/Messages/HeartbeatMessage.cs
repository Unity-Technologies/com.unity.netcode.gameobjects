namespace Unity.Netcode
{
    internal struct HeartbeatMessage : INetworkMessage
    {
        public void Serialize(FastBufferWriter writer)
        {
        }

        public static void Receive(FastBufferReader reader, in NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            Handle(context.SenderId, context.Timestamp, networkManager);
        }

        public static void Handle(ulong senderId, float receivedTimestamp, NetworkManager networkManager)
        {
            networkManager.heartbeatSystem?.ProcessHeartbeat(senderId, receivedTimestamp);
        }
    }
}
