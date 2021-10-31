namespace Unity.Netcode
{
    internal struct ParentSyncMessage : INetworkMessage
    {
        public ulong NetworkObjectId;

        public bool IsReparented;

        //If(Metadata.IsReparented)
        public bool IsLatestParentSet;

        //If(IsLatestParentSet)
        public ulong? LatestParent;

        public void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(NetworkObjectId);
            writer.WriteValueSafe(IsReparented);
            if (IsReparented)
            {
                writer.WriteValueSafe(IsLatestParentSet);
                if (IsLatestParentSet)
                {
                    writer.WriteValueSafe((ulong)LatestParent);
                }
            }
        }

        public static void Receive(FastBufferReader reader, in NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return;
            }

            var message = new ParentSyncMessage();
            reader.ReadValueSafe(out message.NetworkObjectId);
            reader.ReadValueSafe(out message.IsReparented);
            if (message.IsReparented)
            {
                reader.ReadValueSafe(out message.IsLatestParentSet);
                if (message.IsLatestParentSet)
                {
                    reader.ReadValueSafe(out ulong latestParent);
                    message.LatestParent = latestParent;
                }
            }

            message.Handle(reader, context, networkManager);
        }

        public void Handle(FastBufferReader reader, in NetworkContext context, NetworkManager networkManager)
        {
            if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId))
            {
                var networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];
                networkObject.SetNetworkParenting(IsReparented, LatestParent);
                networkObject.ApplyNetworkParenting();
            }
            else
            {
                networkManager.SpawnManager.TriggerOnSpawn(NetworkObjectId, reader, context);
            }
        }
    }
}
