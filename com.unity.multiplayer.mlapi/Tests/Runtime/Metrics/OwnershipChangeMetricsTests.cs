#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class OwnershipChangeMetricsTests : SingleClientMetricTestBase
    {
        [UnityTest]
        public IEnumerator TrackOwnershipChangeSentMetric()
        {
            var gameObject = new GameObject(Guid.NewGuid().ToString());
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = Server;
            networkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricValues = new WaitForMetricValues<OwnershipChangeEvent>(ServerMetrics.Dispatcher, MetricNames.OwnershipChangeSent);

            networkObject.ChangeOwnership(1);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();

            var ownershipChangeSent = metricValues.First();
            Assert.AreEqual(networkObject.NetworkObjectId, ownershipChangeSent.NetworkId.NetworkId);
            Assert.AreEqual(Server.LocalClientId, ownershipChangeSent.Connection.Id);
            Assert.AreEqual(2, ownershipChangeSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackOwnershipChangeReceivedMetric()
        {
            var gameObject = new GameObject(Guid.NewGuid().ToString());
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = Server;
            networkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricValues = new WaitForMetricValues<OwnershipChangeEvent>(ClientMetrics.Dispatcher, MetricNames.OwnershipChangeReceived);

            networkObject.ChangeOwnership(1);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, metricValues.Count);

            var ownershipChangeReceived = metricValues.First();
            Assert.AreEqual(networkObject.NetworkObjectId, ownershipChangeReceived.NetworkId.NetworkId);
            Assert.AreEqual(2, ownershipChangeReceived.BytesCount);
        }
    }
}
#endif
