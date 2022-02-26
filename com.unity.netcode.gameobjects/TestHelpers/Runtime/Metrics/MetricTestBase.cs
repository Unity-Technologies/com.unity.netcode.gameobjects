#if MULTIPLAYER_TOOLS
using System.Collections;

namespace Unity.Netcode.TestHelpers.Runtime.Metrics
{
    internal abstract class SingleClientMetricTestBase : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        internal NetworkManager Server { get; private set; }

        internal NetworkMetrics ServerMetrics { get; private set; }

        internal NetworkManager Client { get; private set; }

        internal NetworkMetrics ClientMetrics { get; private set; }

        protected override void OnServerAndClientsCreated()
        {
            Server = m_ServerNetworkManager;
            Client = m_ClientNetworkManagers[0];
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnStartedServerAndClients()
        {
            ServerMetrics = Server.NetworkMetrics as NetworkMetrics;
            ClientMetrics = Client.NetworkMetrics as NetworkMetrics;
            yield return base.OnStartedServerAndClients();
        }
    }

    public abstract class DualClientMetricTestBase : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        internal NetworkManager Server { get; private set; }

        internal NetworkMetrics ServerMetrics { get; private set; }

        internal NetworkManager FirstClient { get; private set; }

        internal NetworkMetrics FirstClientMetrics { get; private set; }

        internal NetworkManager SecondClient { get; private set; }

        internal NetworkMetrics SecondClientMetrics { get; private set; }

        protected override void OnServerAndClientsCreated()
        {
            Server = m_ServerNetworkManager;
            FirstClient = m_ClientNetworkManagers[0];
            SecondClient = m_ClientNetworkManagers[1];
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnStartedServerAndClients()
        {
            ServerMetrics = Server.NetworkMetrics as NetworkMetrics;
            FirstClientMetrics = FirstClient.NetworkMetrics as NetworkMetrics;
            SecondClientMetrics = SecondClient.NetworkMetrics as NetworkMetrics;
            yield return base.OnStartedServerAndClients();
        }
    }
}
#endif
