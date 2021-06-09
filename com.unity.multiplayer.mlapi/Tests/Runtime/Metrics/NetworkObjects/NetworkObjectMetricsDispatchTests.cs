using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.NetworkObjects
{
#if true
    public class NetworkObjectMetricsDispatchTests : MetricsTestBase
    {
        NetworkManager m_NetworkManager;
        NetworkMetrics m_NetworkMetrics;

        [SetUp]
        public void SetUp()
        {
            NetworkManagerHelper.StartNetworkManager(out m_NetworkManager);
            m_NetworkMetrics = m_NetworkManager.NetworkMetrics as NetworkMetrics;
        }

        [TearDown]
        public void TearDown()
        {
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenNetworkObjectSpawnSentToClient_TracksObjectSpawnSentMetric()
        {
            const string objectName = "TestNetworkObjectToSpawn";
            const ulong clientId = 100;
            var gameObjectId = NetworkManagerHelper.AddGameNetworkObject(objectName);
            NetworkManagerHelper.InstantiatedNetworkObjects.TryGetValue(gameObjectId, out var networkObj);
            NetworkManagerHelper.AddConnectedClient(clientId);

            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var objectSpawnedMetric = AssertSingleMetricEventOfType<ObjectSpawnedEvent>(collection, MetricNames.ObjectSpawned);
                Assert.AreEqual(1, objectSpawnedMetric.Values.Count);
                Assert.AreEqual(clientId, objectSpawnedMetric.Values.Select(x => x.Connection.Id).First());
                Assert.AreEqual(objectName, objectSpawnedMetric.Values.Select(x => x.NetworkId.Name));
                m_NetworkManager.SpawnManager.DespawnObject(networkObj);
            }));
            networkObj.Observers.Add(clientId);
            networkObj.Spawn();

            yield return WaitForMetricsDispatch();
        }
    }
#endif
}