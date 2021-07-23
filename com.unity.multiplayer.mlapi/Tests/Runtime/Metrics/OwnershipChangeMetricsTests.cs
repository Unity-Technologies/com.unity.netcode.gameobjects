using System;
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
    public class OwnershipChangeMetricsTests
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
        public IEnumerator TrackOwnershipChangeSentMetric()
        {
            var gameObject = new GameObject(Guid.NewGuid().ToString());
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_Server;
            networkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricValues = new WaitForMetricValues<OwnershipChangeEvent>(m_ServerMetrics.Dispatcher, MetricNames.OwnershipChangeSent);

            networkObject.ChangeOwnership(1);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();

            var ownershipChangeSent = metricValues.First();
            Assert.AreEqual(networkObject.NetworkObjectId, ownershipChangeSent.NetworkId.NetworkId);
            Assert.AreEqual(m_Server.LocalClientId, ownershipChangeSent.Connection.Id);
            Assert.AreEqual(2, ownershipChangeSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackOwnershipChangeReceivedMetric()
        {
            var gameObject = new GameObject(Guid.NewGuid().ToString());
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_Server;
            networkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricValues = new WaitForMetricValues<OwnershipChangeEvent>(m_ClientMetrics.Dispatcher, MetricNames.OwnershipChangeReceived);

            networkObject.ChangeOwnership(1);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, metricValues.Count);

            var ownershipChangeReceived = metricValues.First();
            Assert.AreEqual(networkObject.NetworkObjectId, ownershipChangeReceived.NetworkId.NetworkId);
            Assert.AreEqual(3, ownershipChangeReceived.BytesCount);
        }
    }
}