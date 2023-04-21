namespace Unity.Netcode
{
    internal struct CreateObjectMessage : INetworkMessage
    {
        public int Version => 0;

        public NetworkObject.SceneObject ObjectInfo;
        private FastBufferReader m_ReceivedNetworkVariableData;

        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            ObjectInfo.Serialize(writer);
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }

            ObjectInfo.Deserialize(reader);
            if (!networkManager.NetworkConfig.ForceSamePrefabs && !networkManager.SpawnManager.HasPrefab(ObjectInfo))
            {
                networkManager.DeferredMessageManager.DeferMessage(IDeferredNetworkMessageManager.TriggerType.OnAddPrefab, ObjectInfo.Hash, reader, ref context);
                return false;
            }
            m_ReceivedNetworkVariableData = reader;

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var networkObject = NetworkObject.AddSceneObject(ObjectInfo, m_ReceivedNetworkVariableData, networkManager);

            networkManager.NetworkMetrics.TrackObjectSpawnReceived(context.SenderId, networkObject, context.MessageSize);
        }
    }
}
