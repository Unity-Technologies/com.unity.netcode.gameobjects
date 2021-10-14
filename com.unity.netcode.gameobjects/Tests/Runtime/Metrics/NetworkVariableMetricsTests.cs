#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class NetworkVariableMetricsTests : SingleClientMetricTestBase
    {
        protected override Action<GameObject> UpdatePlayerPrefab => prefab => prefab.AddComponent<NetworkVariableComponent>();

        [UnityTest]
        public IEnumerator TrackNetworkVariableDeltaSentMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<NetworkVariableEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.NetworkVariableDeltaSent);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();

            var networkVariableDeltaSent = metricValues.First();
            Assert.AreEqual(nameof(NetworkVariableComponent.MyNetworkVariable), networkVariableDeltaSent.Name);
            Assert.AreEqual(Server.LocalClientId, networkVariableDeltaSent.Connection.Id);
            Assert.AreNotEqual(0, networkVariableDeltaSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkVariableDeltaReceivedMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<NetworkVariableEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.NetworkVariableDeltaReceived);

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
