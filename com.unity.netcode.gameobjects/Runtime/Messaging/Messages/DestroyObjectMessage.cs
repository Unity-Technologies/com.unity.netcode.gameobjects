namespace Unity.Netcode
{
    internal struct DestroyObjectMessage : INetworkMessage
    {
        public ulong NetworkObjectId;

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
            reader.ReadValueSafe(out DestroyObjectMessage message);
            message.Handle(context.SenderId, networkManager, reader.Length);
        }

        public void Handle(ulong senderId, NetworkManager networkManager, int messageSize)
        {
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

            networkManager.NetworkMetrics.TrackObjectDestroyReceived(senderId, networkObject, messageSize);
            networkManager.SpawnManager.OnDespawnObject(networkObject, true);
        }
    }
}
