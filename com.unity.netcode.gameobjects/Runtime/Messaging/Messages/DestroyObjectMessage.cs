namespace Unity.Netcode
{
    internal struct DestroyObjectMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        public ulong NetworkObjectId;
        public bool DestroyGameObject;

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

            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context);
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
