using System;
using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using MLAPI.NetworkVariable;
using NUnit.Framework;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
    public class NetworkVariableMetricsTests : MetricsTestBase
    {
        NetworkManager m_Server;
        NetworkManager m_Client;
        NetworkMetrics m_ClientMetrics;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!MultiInstanceHelpers.Create(1, out m_Server, out var clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }
            
            var playerPrefab = new GameObject("Player");
            var networkObject = playerPrefab.AddComponent<NetworkObject>();
            playerPrefab.AddComponent<NetworkVariableComponent>();

            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            m_Server.NetworkConfig.PlayerPrefab = playerPrefab;

            foreach (var client in clients)
            {
                client.NetworkConfig.PlayerPrefab = playerPrefab;
            }

            if (!MultiInstanceHelpers.Start(true, m_Server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(m_Server));

            m_Client = clients.First();
            m_ClientMetrics = m_Client.NetworkMetrics as NetworkMetrics;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MultiInstanceHelpers.Destroy();

            yield return null;
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenNetworkVariableDeltaSent_TracksNetworkVariableDeltaMetric()
        {
            var found = false;
            m_ClientMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var networkVariableUpdateMetric = collection.Metrics.SingleOrDefault(x => x.Name == MetricNames.NetworkVariableDelta);
                Assert.NotNull(networkVariableUpdateMetric);

                var typedMetric = networkVariableUpdateMetric as IEventMetric<NetworkVariableEvent>;
                Assert.NotNull(typedMetric);
                if (typedMetric.Values.Any()) // We always get the metric, but when it has values, something has been tracked
                {
                    // We have an instance each of the player prefabs
                    Assert.AreEqual(2, typedMetric.Values.Count);

                    var first = typedMetric.Values.First();
                    Assert.True(first.Name.Contains(nameof(NetworkVariableString)));

                    var last = typedMetric.Values.Last();
                    Assert.True(last.Name.Contains(nameof(NetworkVariableString)));

                    found = true;
                }
            }));

            yield return WaitForAFewFrames();

            Assert.True(found);
        }

        private class NetworkVariableComponent : NetworkBehaviour
        {
            public NetworkVariableString NetworkVariableString { get; } = new NetworkVariableString();

            void Update()
            {
                if (IsServer)
                {
                    NetworkVariableString.Value = Guid.NewGuid().ToString();
                }
            }
        }
    }
}