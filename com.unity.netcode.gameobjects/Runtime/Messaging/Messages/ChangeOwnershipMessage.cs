namespace Unity.Netcode
{
    internal struct ChangeOwnershipMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        public ulong NetworkObjectId;
        public ulong OwnerClientId;

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
            if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredMessageManager.TriggerType.OnSpawn, NetworkObjectId, reader, ref context);
                return false;
            }

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];
            var originalOwner = networkObject.OwnerClientId;

            networkObject.OwnerClientId = OwnerClientId;

            // We are current owner.
            if (originalOwner == networkManager.LocalClientId)
            {
                networkObject.InvokeBehaviourOnLostOwnership();
            }

            // We are new owner.
            if (OwnerClientId == networkManager.LocalClientId)
            {
                networkObject.InvokeBehaviourOnGainedOwnership();
            }

            // For all other clients that are neither the former or current owner, update the behaviours' properties
            if (OwnerClientId != networkManager.LocalClientId && originalOwner != networkManager.LocalClientId)
            {
                for (int i = 0; i < networkObject.ChildNetworkBehaviours.Count; i++)
                {
                    networkObject.ChildNetworkBehaviours[i].UpdateNetworkProperties();
                }
            }

            networkManager.NetworkMetrics.TrackOwnershipChangeReceived(context.SenderId, networkObject, context.MessageSize);
        }
    }
}
