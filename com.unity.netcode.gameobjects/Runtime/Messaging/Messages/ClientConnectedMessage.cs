namespace Unity.Netcode
{
    internal struct ClientConnectedMessage : INetworkMessage, INetworkSerializeByMemcpy
    {
        public int Version => 0;

        public ulong ClientId;

#if NGO_DAMODE
        public bool ShouldSynchronize;
#endif


        public void Serialize(FastBufferWriter writer, int targetVersion)
        {
            BytePacker.WriteValueBitPacked(writer, ClientId);
#if NGO_DAMODE
            writer.WriteValueSafe(ShouldSynchronize);
#endif
        }

        public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            if (!networkManager.IsClient)
            {
                return false;
            }
            ByteUnpacker.ReadValueBitPacked(reader, out ClientId);
#if NGO_DAMODE
            reader.ReadValueSafe(out ShouldSynchronize);
#endif

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
#if NGO_DAMODE
            if ((ShouldSynchronize || networkManager.CMBServiceConnection) && networkManager.DistributedAuthorityMode && networkManager.LocalClient.IsSessionOwner)
            {
                networkManager.SceneManager.SynchronizeNetworkObjects(ClientId);
            }
            else
            {
                // All modes support adding NetworkClients
                networkManager.ConnectionManager.AddClient(ClientId);
            }
#endif
            if (!networkManager.ConnectionManager.ConnectedClientIds.Contains(ClientId))
            {
                networkManager.ConnectionManager.ConnectedClientIds.Add(ClientId);
            }
            if (networkManager.IsConnectedClient)
            {
                networkManager.ConnectionManager.InvokeOnPeerConnectedCallback(ClientId);
            }

#if NGO_DAMODE
            // DANGO-TODO: Remove the session owner object distribution check once the service handles object distribution
            if (networkManager.DistributedAuthorityMode && networkManager.CMBServiceConnection && !networkManager.NetworkConfig.EnableSceneManagement)
            {
                // Don't redistribute for the local instance
                if (ClientId != networkManager.LocalClientId)
                {
                    // We defer redistribution to the end of the NetworkUpdateStage.PostLateUpdate
                    networkManager.RedistributeToClient = true;
                    networkManager.ClientToRedistribute = ClientId;
                    networkManager.TickToRedistribute = networkManager.ServerTime.Tick + 20;
                }
            }
#endif
        }
    }
}
