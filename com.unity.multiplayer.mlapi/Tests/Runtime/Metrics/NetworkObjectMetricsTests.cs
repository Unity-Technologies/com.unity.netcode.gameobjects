using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI.Configuration;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
    public class NetworkObjectMetricsTests
    {
        NetworkManager m_Server;
        NetworkManager m_Client;
        NetworkMetrics m_ClientMetrics;
        NetworkMetrics m_ServerMetrics;

        private NetworkObject m_NewNetworkObject;
        private const string m_NewNetworkObjectName = "TestNetworkObjectToSpawn";

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

            var gameObject = new GameObject(m_NewNetworkObjectName);
            m_NewNetworkObject = gameObject.AddComponent<NetworkObject>();
            m_NewNetworkObject.NetworkManagerOwner = m_Server;
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(m_NewNetworkObject);
            var networkPrefab = new NetworkPrefab {Prefab = gameObject};
            m_Server.NetworkConfig.NetworkPrefabs.Add(networkPrefab);

            foreach (var client in clients)
            {
                client.NetworkConfig.PlayerPrefab = playerPrefab;
                client.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            }

            if (!MultiInstanceHelpers.Start(true, m_Server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            m_Client = clients.First();

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(m_Server));

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
            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(m_ServerMetrics.Dispatcher, MetricNames.ObjectSpawnedSent);

            m_NewNetworkObject.Spawn();

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectSpawnedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedSentMetricValues.Count);

            var objectSpawned = objectSpawnedSentMetricValues.First();
            Assert.AreEqual(m_Client.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(m_NewNetworkObjectName, objectSpawned.NetworkId.Name);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectSpawnReceivedMetric()
        {
            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(m_ClientMetrics.Dispatcher, MetricNames.ObjectSpawnedReceived);

            m_NewNetworkObject.Spawn();

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectSpawnedReceivedMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedReceivedMetricValues.Count);

            var objectSpawned = objectSpawnedReceivedMetricValues.First();
            Assert.AreEqual(m_Server.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(m_NewNetworkObject.NetworkObjectId, objectSpawned.NetworkId.NetworkId);
            Assert.AreEqual($"{m_NewNetworkObjectName}(Clone)", objectSpawned.NetworkId.Name);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroySentMetric()
        {
            m_NewNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(m_ServerMetrics.Dispatcher, MetricNames.ObjectDestroyedSent);

            m_Server.SpawnManager.OnDespawnObject(m_NewNetworkObject, true);

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectDestroyedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            // As there's a client and server, this event is emitted twice.
            Assert.AreEqual(2, objectDestroyedSentMetricValues.Count);

            var objectDestroyed = objectDestroyedSentMetricValues.Last();
            Assert.AreEqual(m_Client.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(m_NewNetworkObjectName, objectDestroyed.NetworkId.Name);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroyReceivedMetric()
        {
            m_NewNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(m_ClientMetrics.Dispatcher, MetricNames.ObjectDestroyedReceived);
            
            m_Server.SpawnManager.OnDespawnObject(m_NewNetworkObject, true);
            
            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectDestroyedReceivedMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectDestroyedReceivedMetricValues.Count);

            var objectDestroyed = objectDestroyedReceivedMetricValues.First();
            Assert.AreEqual(m_Server.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(m_NewNetworkObject.NetworkObjectId, objectDestroyed.NetworkId.NetworkId);
            Assert.AreEqual($"{m_NewNetworkObjectName}(Clone)", objectDestroyed.NetworkId.Name);
        }

        [UnityTest]
        public IEnumerator TrackMultipleNetworkObjectDestroySentMetric()
        {
            m_NewNetworkObject.Spawn();

            // Spawn another network object so we can hide multiple.
            var gameObject = new GameObject(m_NewNetworkObjectName);
            var anotherNetworkObject = gameObject.AddComponent<NetworkObject>();
            anotherNetworkObject.NetworkManagerOwner = m_Server;
            anotherNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(m_ServerMetrics.Dispatcher, MetricNames.ObjectDestroyedSent);

            NetworkObject.NetworkHide(new List<NetworkObject>{m_NewNetworkObject, anotherNetworkObject}, m_Client.LocalClientId);

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectDestroyedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            // As there's a client and server, this event is emitted twice.
            Assert.AreEqual(2, objectDestroyedSentMetricValues.Count);

            var networkIDFound = true;
            var networkNameFound = true;
            // not sure that we can guarantee the order of these so just ensure the data is in the received metrics.
            foreach (var metricValue in objectDestroyedSentMetricValues)
            {
                Assert.AreEqual(m_Client.LocalClientId, metricValue.Connection.Id);
                networkIDFound &= metricValue.NetworkId.NetworkId == m_NewNetworkObject.NetworkObjectId || metricValue.NetworkId.NetworkId == anotherNetworkObject.NetworkObjectId;
                networkNameFound &= metricValue.NetworkId.Name == m_NewNetworkObject.name || metricValue.NetworkId.Name == anotherNetworkObject.name;
            }
            Assert.IsTrue(networkIDFound);
            Assert.IsTrue(networkNameFound);
        }

        [UnityTest]
        public IEnumerator TrackMultipleNetworkObjectSpawnSentMetric()
        {
            m_NewNetworkObject.Spawn();

            // Spawn another network object so we can hide multiple.
            var gameObject = new GameObject(m_NewNetworkObjectName);
            var anotherNetworkObject = gameObject.AddComponent<NetworkObject>();
            anotherNetworkObject.NetworkManagerOwner = m_Server;
            anotherNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            NetworkObject.NetworkHide(new List<NetworkObject>{m_NewNetworkObject, anotherNetworkObject}, m_Client.LocalClientId);

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(m_ServerMetrics.Dispatcher, MetricNames.ObjectSpawnedSent);

            NetworkObject.NetworkShow(new List<NetworkObject>{m_NewNetworkObject, anotherNetworkObject}, m_Client.LocalClientId);

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectSpawnedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            // As there's a client and server, this event is emitted twice.
            Assert.AreEqual(2, objectSpawnedSentMetricValues.Count);

            var networkIDFound = true;
            var networkNameFound = true;
            // not sure that we can guarantee the order of these so just ensure the data is in the received metrics.
            foreach (var metricValue in objectSpawnedSentMetricValues)
            {
                Assert.AreEqual(m_Client.LocalClientId, metricValue.Connection.Id);
                networkIDFound &= metricValue.NetworkId.NetworkId == m_NewNetworkObject.NetworkObjectId || metricValue.NetworkId.NetworkId == anotherNetworkObject.NetworkObjectId;
                networkNameFound &= metricValue.NetworkId.Name == m_NewNetworkObject.name || metricValue.NetworkId.Name == anotherNetworkObject.name;
            }

            Assert.IsTrue(networkIDFound);
            Assert.IsTrue(networkNameFound);
        }
    }
}
