using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.NetworkVariables
{
    public class NetworkVariableMetricsReceptionTests : MetricsTestBase
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
        public IEnumerator NetworkMetrics_WhenNetworkVariableDeltaReceived_TracksNetworkVariableDeltaMetric()
        {
            var found = false;
            m_ClientMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var networkVariableUpdateMetric = collection.Metrics.SingleOrDefault(x => x.Name == MetricNames.NetworkVariableDeltaReceived);
                Assert.NotNull(networkVariableUpdateMetric);

                var typedMetric = networkVariableUpdateMetric as IEventMetric<NetworkVariableEvent>;
                Assert.NotNull(typedMetric);
                if (typedMetric.Values.Any()) // We always get the metric, but when it has values, something has been tracked
                {
                    // We have an instance each of the player prefabs
                    Assert.AreEqual(2, typedMetric.Values.Count);

                    var first = typedMetric.Values.First();
                    Assert.AreEqual(nameof(NetworkVariableComponent.MyNetworkVariable), first.Name);

                    var last = typedMetric.Values.Last();
                    Assert.AreEqual(nameof(NetworkVariableComponent.MyNetworkVariable), last.Name);

                    found = true;
                }
            }));

            yield return WaitForAFewFrames();

            Assert.True(found);
        }
    }
}