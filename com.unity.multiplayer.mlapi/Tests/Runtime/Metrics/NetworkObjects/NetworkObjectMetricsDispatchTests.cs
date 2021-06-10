using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.NetworkObjects
{
#if true
    public class NetworkObjectMetricsDispatchTests
    {
        NetworkManager m_Server;
        NetworkManager m_Client;
        NetworkMetrics m_ClientMetrics;
        NetworkMetrics m_ServerMetrics;

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
            m_ServerMetrics = m_Server.NetworkMetrics as NetworkMetrics;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MultiInstanceHelpers.Destroy();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectSpawnSentMetric()
        {
            const string objectName = "TestNetworkObjectToSpawn";
            var playerPrefab = new GameObject(objectName);
            var networkObject = playerPrefab.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_Server;

            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);
            // NetworkManagerHelper.AddConnectedClient(m_Client.LocalClientId);

            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(m_ServerMetrics.Dispatcher, MetricNames.ObjectSpawned);

            // networkObj.Observers.Add(m_Client.LocalClientId);
            networkObject.Spawn();

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectSpawnedSentMetricValues = waitForMetricEvent.Values;
            Assert.AreEqual(1, objectSpawnedSentMetricValues.Count);
            Assert.AreEqual(m_Client.LocalClientId, objectSpawnedSentMetricValues.Select(x => x.Connection.Id).First());
            Assert.AreEqual(objectName, objectSpawnedSentMetricValues.Select(x => x.NetworkId.Name));
        }
    }
#endif
}