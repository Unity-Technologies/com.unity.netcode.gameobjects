namespace Unity.Netcode
{
    internal struct DestroyObjectMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        public int Version => 0;

        public ulong NetworkObjectId;
        public bool DestroyGameObject;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            BytePacker.WriteValueBitPacked(writer, NetworkObjectId);
            writer.WriteValueSafe(DestroyGameObject);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }

            ByteUnpacker.ReadValueBitPacked(reader, out NetworkObjectId);
            reader.ReadValueSafe(out DestroyGameObject);

            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context);
                return false;
            }
            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject))
            {
                // This is the same check and log message that happens inside OnDespawnObject, but we have to do it here
                return;
            }

            networkManager.NetworkMetrics.TrackObjectDestroyReceived(context.SenderId, networkObject, context.MessageSize);
            networkManager.SpawnManager.OnDespawnObject(networkObject, DestroyGameObject);
        }
    }
}
