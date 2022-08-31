#if MULTIPLAYER_TOOLS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime.Metrics;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class NetworkVariableMetricsTests : SingleClientMetricTestBase
    {
        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<NetworkVariableComponent>();
            base.OnCreatePlayerPrefab();
        }

        [UnityTest]
        public IEnumerator TrackNetworkVariableDeltaSentMetric()
        {
            var waitForMetricValues = new WaitForEventMetricValues<NetworkVariableEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.NetworkVariableDeltaSent);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();

            bool found = false;
            foreach (var networkVariableDeltaSent in metricValues)
            {
                if (nameof(NetworkVariableComponent.MyNetworkVariable) == networkVariableDeltaSent.Name &&
                    Client.LocalClientId == networkVariableDeltaSent.Connection.Id &&
                    0 != networkVariableDeltaSent.BytesCount)
                {
                    found = true;
                }
            }
            Assert.IsTrue(found);
        }

        [UnityTest]
        public IEnumerator TrackNetworkVariableDeltaReceivedMetric()
        {
            var waitForMetricValues = new WaitForEventMetricValues<NetworkVariableEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.NetworkVariableDeltaReceived);

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
#endif
