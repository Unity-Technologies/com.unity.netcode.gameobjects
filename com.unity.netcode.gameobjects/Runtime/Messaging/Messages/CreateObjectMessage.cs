namespace Unity.Netcode
{
    internal struct CreateObjectMessage : INetworkMessage
    {
        public NetworkObject.SceneObject ObjectInfo;

        public void Serialize(FastBufferWriter writer)
        {
            ObjectInfo.Serialize(writer);
        }

        public static void Receive(FastBufferReader reader, in NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return;
            }
            var message = new CreateObjectMessage();
            message.ObjectInfo.Deserialize(reader);
            message.Handle(context.SenderId, reader, networkManager);
        }

        public void Handle(ulong senderId, FastBufferReader reader, NetworkManager networkManager)
        {
            var networkObject = NetworkObject.AddSceneObject(ObjectInfo, reader, networkManager);
            networkManager.NetworkMetrics.TrackObjectSpawnReceived(senderId, networkObject, reader.Length);
        }
    }
}
