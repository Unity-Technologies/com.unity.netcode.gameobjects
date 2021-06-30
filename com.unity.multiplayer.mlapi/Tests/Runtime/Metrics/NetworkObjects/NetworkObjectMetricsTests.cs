using System.Collections;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.NetworkObjects
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

            var gameObject = new GameObject(m_NewNetworkObjectName);
            m_NewNetworkObject = gameObject.AddComponent<NetworkObject>();
            m_NewNetworkObject.NetworkManagerOwner = m_Server;
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

            var objectSpawnedSentMetricValues = waitForMetricEvent.EnsureMetricValuesHaveBeenFound();
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

            var objectSpawnedReceivedMetricValues = waitForMetricEvent.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedReceivedMetricValues.Count);

            var objectSpawned = objectSpawnedReceivedMetricValues.First();
            Assert.AreEqual(m_Server.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(m_NewNetworkObject.NetworkObjectId, objectSpawned.NetworkId.NetworkId);
            // Bug: this should not be the name of the network object
            // Assert.AreEqual("Player(Clone)", objectSpawned.NetworkId.Name); // What?
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroySentMetric()
        {
            m_NewNetworkObject.Spawn();

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(m_ServerMetrics.Dispatcher, MetricNames.ObjectDestroyedSent);
            // TODO: is there a better way of waiting here?
            yield return waitForMetricEvent.WaitForAFewFrames();

            m_Server.SpawnManager.OnDespawnObject(m_NewNetworkObject, true);

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectDestroyedSentMetricValues = waitForMetricEvent.EnsureMetricValuesHaveBeenFound();
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
            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(m_ClientMetrics.Dispatcher, MetricNames.ObjectDestroyedReceived);

            yield return waitForMetricEvent.WaitForAFewFrames();

            m_Server.SpawnManager.OnDespawnObject(m_NewNetworkObject, true);
            yield return waitForMetricEvent.Wait(60);

            var objectDestroyedReceivedMetricValues = waitForMetricEvent.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectDestroyedReceivedMetricValues.Count);

            var objectDestroyed = objectDestroyedReceivedMetricValues.First();
            Assert.AreEqual(m_Server.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(m_NewNetworkObject.NetworkObjectId, objectDestroyed.NetworkId.NetworkId);
            // Bug: Currently the object name is always "Player Clone"
            // Assert.AreEqual(m_NewNetworkObjectName, objectDestroyed.NetworkId.Name);
        }
    }
}
