namespace Unity.Netcode
{
    internal struct ChangeOwnershipMessage : INetworkMessage
    {
        public ulong NetworkObjectId;
        public ulong OwnerClientId;

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
            reader.ReadValueSafe(out ChangeOwnershipMessage message);
            message.Handle(reader, context, context.SenderId, networkManager, reader.Length);
        }

        public void Handle(FastBufferReader reader, in NetworkContext context, ulong senderId, NetworkManager networkManager, int messageSize)
        {
            if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject))
            {
                networkManager.SpawnManager.TriggerOnSpawn(NetworkObjectId, reader, context);
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

            networkManager.NetworkMetrics.TrackOwnershipChangeReceived(senderId, networkObject, messageSize);
        }
    }
}
