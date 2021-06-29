using System;
using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using MLAPI.RuntimeTests.Metrics.NetworkVariables;
using NUnit.Framework;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.Ownership
{
    public class NetworkMetricsOwnershipChangeDispatchTests
    {
        NetworkManager m_NetworkManager;
        NetworkMetrics m_NetworkMetrics;

        Guid m_GameObjectId;

        [SetUp]
        public void SetUp()
        {
            NetworkManagerHelper.StartNetworkManager(out m_NetworkManager);
            m_NetworkMetrics = m_NetworkManager.NetworkMetrics as NetworkMetrics;

            m_GameObjectId = NetworkManagerHelper.AddGameNetworkObject("ObjectToChangeOwner");
            NetworkManagerHelper.SpawnNetworkObject(m_GameObjectId);
        }

        [TearDown]
        public void TearDown()
        {
            NetworkManagerHelper.ShutdownNetworkManager();
        }
        
        [UnityTest]
        public IEnumerator TrackOwnershipChangeSentMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<OwnershipChangeEvent>(m_NetworkMetrics.Dispatcher, MetricNames.OwnershipChangeSent);

            NetworkManagerHelper.InstantiatedNetworkObjects[m_GameObjectId].ChangeOwnership(1);

            yield return waitForMetricValues.WaitForAFewFrames();

            var metricValues = waitForMetricValues.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, metricValues.Count);

            var ownershipChangeSent = metricValues.First();
            Assert.AreEqual(m_NetworkManager.LocalClientId, ownershipChangeSent.Connection.Id);
        }
    }
}