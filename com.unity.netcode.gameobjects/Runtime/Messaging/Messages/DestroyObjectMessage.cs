namespace Unity.Netcode
{
    internal struct DestroyObjectMessage : INetworkMessage
    {
        public ulong NetworkObjectId;

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
            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject))
            {
                // This is the same check and log message that happens inside OnDespawnObject, but we have to do it here
                // while we still have access to the network ID, otherwise the log message will be less useful.
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"Trying to destroy {nameof(NetworkObject)} #{NetworkObjectId} but it does not exist in {nameof(NetworkSpawnManager.SpawnedObjects)} anymore!");
                }

                return;
            }

            networkManager.NetworkMetrics.TrackObjectDestroyReceived(context.SenderId, networkObject, context.MessageSize);
            networkManager.SpawnManager.OnDespawnObject(networkObject, true);
        }
    }
}
