#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools;
#endif

namespace Unity.Netcode
{
    public class NetworkMetricsManager : INetworkUpdateSystem
    {
        internal INetworkMetrics NetworkMetrics { get; private set; }

        private NetworkManager m_NetworkManager;

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (updateStage == NetworkUpdateStage.PostLateUpdate)
            {
                NetworkMetrics.UpdateNetworkObjectsCount(m_NetworkManager.SpawnManager.SpawnedObjects.Count);
                NetworkMetrics.UpdateConnectionsCount((m_NetworkManager.IsServer) ? m_NetworkManager.ConnectionManager.ConnectedClients.Count : 1);
                NetworkMetrics.DispatchFrame();
            }
        }

        public void Initialize(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            if (NetworkMetrics == null)
            {
#if MULTIPLAYER_TOOLS
                NetworkMetrics = new NetworkMetrics();
#else
                NetworkMetrics = new NullNetworkMetrics();
#endif
                this.RegisterNetworkUpdate(NetworkUpdateStage.PostLateUpdate);
            }

#if MULTIPLAYER_TOOLS
            NetworkSolutionInterface.SetInterface(new NetworkSolutionInterfaceParameters
            {
                NetworkObjectProvider = new NetworkObjectProvider(networkManager),
            });
#endif
        }

        public void Shutdown()
        {
            this.UnregisterNetworkUpdate(NetworkUpdateStage.PostLateUpdate);
        }
    }
}
