#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools;
#endif

namespace Unity.Netcode
{
    /// <summary>
    /// This probably needs to all be migrated into <see cref="NetworkConnectionManager"/>, but
    /// keeping it separated for the time being
    /// </summary>
    internal class NetworkMetricsManager
    {
        internal INetworkMetrics NetworkMetrics { get; private set; }

        private NetworkManager m_NetworkManager;

        public void UpdateMetrics()
        {
            NetworkMetrics.UpdateNetworkObjectsCount(m_NetworkManager.SpawnManager.SpawnedObjects.Count);
            NetworkMetrics.UpdateConnectionsCount((m_NetworkManager.IsServer) ? m_NetworkManager.ConnectionManager.ConnectedClients.Count : 1);
            NetworkMetrics.DispatchFrame();
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
            }

#if MULTIPLAYER_TOOLS
            NetworkSolutionInterface.SetInterface(new NetworkSolutionInterfaceParameters
            {
                NetworkObjectProvider = new NetworkObjectProvider(networkManager),
            });
#endif
        }
    }
}
