namespace Unity.Netcode.Messages
{
    public struct ParentSyncMessage : INetworkMessage
    {
        public ulong NetworkObjectId;

        public bool IsReparented;

        #region If(Metadata.IsReparented)
        public bool IsLatestParentSet;

        #region If(IsLatestParentSet)
        public ulong? LatestParent;
        #endregion
        #endregion

        public void Serialize(ref FastBufferWriter writer)
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

        public static void Receive(ref FastBufferReader reader, NetworkContext context)
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

            message.Handle(networkManager);
        }

        public void Handle(NetworkManager networkManager)
        {
            if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(NetworkObjectId))
            {
                var networkObject = networkManager.SpawnManager.SpawnedObjects[NetworkObjectId];
                networkObject.SetNetworkParenting(IsReparented, LatestParent);
                networkObject.ApplyNetworkParenting();
            }
            else if (NetworkLog.CurrentLogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogWarning($"Read {nameof(ParentSyncMessage)} for {nameof(NetworkObject)} #{NetworkObjectId} but could not find it in the {nameof(networkManager.SpawnManager.SpawnedObjects)}");
            }
        }
    }
}
