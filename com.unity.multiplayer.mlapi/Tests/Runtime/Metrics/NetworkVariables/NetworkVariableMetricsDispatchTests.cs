using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.NetworkVariables
{
    public class NetworkVariableMetricsDispatchTests : MetricsTestBase
    {
        NetworkManager m_NetworkManager;
        NetworkMetrics m_NetworkMetrics;

        [SetUp]
        public void SetUp()
        {
            NetworkManagerHelper.StartNetworkManager(out m_NetworkManager);
            m_NetworkMetrics = m_NetworkManager.NetworkMetrics as NetworkMetrics;

            var gameObjectId = NetworkManagerHelper.AddGameNetworkObject("NetworkVariableTestComponent");
            NetworkManagerHelper.AddComponentToObject<NetworkVariableComponent>(gameObjectId);
            NetworkManagerHelper.SpawnNetworkObject(gameObjectId);
        }

        [TearDown]
        public void TearDown()
        {
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [UnityTest]
        public IEnumerator TrackNetworkVariableDeltaSentMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<NetworkVariableEvent>(m_NetworkMetrics.Dispatcher, MetricNames.NetworkVariableDeltaSent);

            yield return waitForMetricValues.WaitForAFewFrames();

            var metricValues = waitForMetricValues.Values;
            Assert.AreEqual(1, metricValues.Count);

            var networkVariableDeltaSent = metricValues.First();
            Assert.AreEqual(nameof(NetworkVariableComponent.MyNetworkVariable), networkVariableDeltaSent.Name);
            Assert.AreEqual(m_NetworkManager.LocalClientId, networkVariableDeltaSent.Connection.Id);
        }
    }
}
