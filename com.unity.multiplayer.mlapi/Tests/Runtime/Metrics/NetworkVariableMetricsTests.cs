using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using MLAPI.RuntimeTests.Metrics.Utility;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
    public class NetworkVariableMetricsTests
    {
        NetworkManager m_Server;
        NetworkMetrics m_ServerMetrics;
        NetworkMetrics m_ClientMetrics;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var initializer = new SingleClientMetricTestInitializer(MetricTestInitializer.CreateAndAssignPlayerPrefabs);

            yield return initializer.Initialize();

            m_Server = initializer.Server;
            m_ServerMetrics = initializer.ServerMetrics;
            m_ClientMetrics = initializer.ClientMetrics;
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
            Assert.AreNotEqual(0, networkVariableDeltaSent.BytesCount);
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
            Assert.AreNotEqual(0, first.BytesCount);

            var last = metricValues.Last();
            Assert.AreEqual(nameof(NetworkVariableComponent.MyNetworkVariable), last.Name);
            Assert.AreNotEqual(0, last.BytesCount);
        }
    }
}
