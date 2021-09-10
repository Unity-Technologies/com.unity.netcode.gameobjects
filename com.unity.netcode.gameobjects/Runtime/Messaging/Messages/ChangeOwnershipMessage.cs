namespace Unity.Netcode.Messages
{
    internal struct ChangeOwnershipMessage: INetworkMessage
    {
        public ulong NetworkObjectId;
        public ulong OwnerClientId;
        
        public void Serialize(ref FastBufferWriter writer)
        {
            writer.WriteValue(this);
        }

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            var networkManager = (NetworkManager) context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return;
            }
            reader.ReadValue(out ChangeOwnershipMessage message);
            message.Handle(context.SenderId, networkManager, reader.Length);
        }

        public void Handle(ulong senderId, NetworkManager networkManager, int messageSize)
        {
            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject))
            {
                if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                {
                    NetworkLog.LogWarning($"Trying to handle owner change but {nameof(NetworkObject)} #{NetworkObjectId} does not exist in {nameof(NetworkSpawnManager.SpawnedObjects)} anymore!");
                }

                return;
            }

            if (networkObject.OwnerClientId == networkManager.LocalClientId)
            {
                //We are current owner.
                networkObject.InvokeBehaviourOnLostOwnership();
            }

            networkObject.OwnerClientId = OwnerClientId;

            if (OwnerClientId == networkManager.LocalClientId)
            {
                //We are new owner.
                networkObject.InvokeBehaviourOnGainedOwnership();
            }

            networkManager.NetworkMetrics.TrackOwnershipChangeReceived(senderId, networkObject.NetworkObjectId, networkObject.name, messageSize);
        }
    }
}