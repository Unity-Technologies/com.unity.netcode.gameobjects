using System;
using System.Collections;
using MLAPI.Configuration;
using MLAPI.Metrics;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.RuntimeTests.Metrics.Utility
{
    internal abstract class MetricTestInitializer
    {
        private readonly int m_NbClients;
        private readonly Action<NetworkManager, NetworkManager[]> m_RunAfterCreate;
        
        protected MetricTestInitializer(int nbClients, Action<NetworkManager, NetworkManager[]> runAfterCreate)
        {
            m_NbClients = nbClients;
            m_RunAfterCreate = runAfterCreate;
        }

        internal NetworkManager Server { get; private set; }

        internal NetworkMetrics ServerMetrics { get; private set; }

        protected NetworkManager[] Clients { get; private set; }
        
        public static void CreateAndAssignPlayerPrefabs(NetworkManager server, NetworkManager[] clients)
        {
            var playerPrefab = new GameObject("Player");
            var networkObject = playerPrefab.AddComponent<NetworkObject>();
            playerPrefab.AddComponent<RpcTestComponent>();
            playerPrefab.AddComponent<NetworkVariableComponent>();

            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            server.NetworkConfig.PlayerPrefab = playerPrefab;

            foreach (var client in clients)
            {
                client.NetworkConfig.PlayerPrefab = playerPrefab;
            }
        }

        internal virtual IEnumerator Initialize()
        {
            if (!MultiInstanceHelpers.Create(m_NbClients, out var server, out var clients))
            {
                Assert.Fail("Failed to create instances");
            }

            m_RunAfterCreate?.Invoke(server, clients);

            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Assert.Fail("Failed to start instances");
            }

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(server));

            Server = server;
            ServerMetrics = server.NetworkMetrics as NetworkMetrics;
            Clients = clients;
        }
    }

    internal class SingleClientMetricTestInitializer : MetricTestInitializer
    {
        internal SingleClientMetricTestInitializer(Action<NetworkManager, NetworkManager[]> runAfterCreate = null)
            : base(1, runAfterCreate)
        {
        }

        internal NetworkManager Client { get; private set; }

        internal NetworkMetrics ClientMetrics { get; private set; }

        internal override IEnumerator Initialize()
        {
            yield return base.Initialize();

            Client = Clients[0];
            ClientMetrics = Client.NetworkMetrics as NetworkMetrics;
        }
    }

    internal class DualClientMetricTestInitializer : MetricTestInitializer
    {
        internal DualClientMetricTestInitializer(Action<NetworkManager, NetworkManager[]> runAfterCreate = null)
            : base(2, runAfterCreate)
        {
        }
        
        internal NetworkManager FirstClient { get; private set; }

        internal NetworkMetrics FirstClientMetrics { get; private set; }
        
        internal NetworkManager SecondClient { get; private set; }

        internal NetworkMetrics SecondClientMetrics { get; private set; }

        internal override IEnumerator Initialize()
        {
            yield return base.Initialize();

            FirstClient = Clients[0];
            FirstClientMetrics = FirstClient.NetworkMetrics as NetworkMetrics;
            SecondClient = Clients[1];
            SecondClientMetrics = SecondClient.NetworkMetrics as NetworkMetrics;
        }
    }
}