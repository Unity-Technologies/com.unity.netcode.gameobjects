using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.NetworkVariables
{
    public class NetworkVariableMetricsTests
    {
        NetworkManager m_Server;
        NetworkMetrics m_ServerMetrics;
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
            m_ServerMetrics = m_Server.NetworkMetrics as NetworkMetrics;
            m_ClientMetrics = m_Client.NetworkMetrics as NetworkMetrics;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MultiInstanceHelpers.Destroy();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TrackNetworkVariableDeltaSentMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<NetworkVariableEvent>(m_ServerMetrics.Dispatcher, MetricNames.NetworkVariableDeltaSent);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();

            var networkVariableDeltaSent = metricValues.First();
            Assert.AreEqual(nameof(NetworkVariableComponent.MyNetworkVariable), networkVariableDeltaSent.Name);
            Assert.AreEqual(m_Server.LocalClientId, networkVariableDeltaSent.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackNetworkVariableDeltaReceivedMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<NetworkVariableEvent>(m_ClientMetrics.Dispatcher, MetricNames.NetworkVariableDeltaReceived);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, metricValues.Count); // We have an instance each of the player prefabs

            var first = metricValues.First();
            Assert.AreEqual(nameof(NetworkVariableComponent.MyNetworkVariable), first.Name);

            var last = metricValues.Last();
            Assert.AreEqual(nameof(NetworkVariableComponent.MyNetworkVariable), last.Name);
        }
    }
}
