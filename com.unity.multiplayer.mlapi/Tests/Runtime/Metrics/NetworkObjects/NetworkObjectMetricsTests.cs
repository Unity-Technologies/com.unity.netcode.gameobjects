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
#if true
    public class NetworkObjectMetricsTests
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
            var objectName = "TestNetworkObjectToSpawn";
            var gameObject = new GameObject(objectName);
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_Server;

            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(m_ServerMetrics.Dispatcher, MetricNames.ObjectSpawnedSent);

            networkObject.Spawn();

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectSpawnedSentMetricValues = waitForMetricEvent.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedSentMetricValues.Count);

            var objectSpawned = objectSpawnedSentMetricValues.First();
            Assert.AreEqual(m_Client.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(objectName, objectSpawned.NetworkId.Name);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectSpawnReceivedMetric()
        {
            var objectName = "TestNetworkObjectToSpawn";
            var gameObject = new GameObject(objectName);
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_Server;

            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(m_ClientMetrics.Dispatcher, MetricNames.ObjectSpawnedReceived);

            networkObject.Spawn();

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectSpawnedReceivedMetricValues = waitForMetricEvent.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedReceivedMetricValues.Count);

            var objectSpawned = objectSpawnedReceivedMetricValues.First();
            Assert.AreEqual(m_Server.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(networkObject.NetworkObjectId, objectSpawned.NetworkId.NetworkId);
            // TODO: this should not be the name of the network object
            Assert.AreEqual("Player(Clone)", objectSpawned.NetworkId.Name); // What?
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroySentMetric()
        {
            var objectName = "TestNetworkObjectToDestroy";
            var gameObject = new GameObject(objectName);
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_Server;
            networkObject.Spawn();

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(m_ServerMetrics.Dispatcher, MetricNames.ObjectDestroyedSent);
            // TODO: is there a better way of waiting here?
            yield return waitForMetricEvent.WaitForAFewFrames();

            m_Server.SpawnManager.OnDestroyObject(networkObject.NetworkObjectId, true);

            yield return waitForMetricEvent.WaitForAFewFrames();

            var objectDestroyedSentMetricValues = waitForMetricEvent.EnsureMetricValuesHaveBeenFound();
            // As there's a client and server, this event is emitted twice.
            Assert.AreEqual(2, objectDestroyedSentMetricValues.Count);

            var objectDestroyed = objectDestroyedSentMetricValues.Last();
            Assert.AreEqual(m_Client.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(objectName, objectDestroyed.NetworkId.Name);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroyReceivedMetric()
        {
            var objectName = "TestNetworkObjectToDestroy";
            var gameObject = new GameObject(objectName);
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = m_Server;

            networkObject.Spawn();
            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(m_ClientMetrics.Dispatcher, MetricNames.ObjectDestroyedReceived);

            yield return waitForMetricEvent.WaitForAFewFrames();

            m_Server.SpawnManager.OnDestroyObject(networkObject.NetworkObjectId, true);
            yield return waitForMetricEvent.Wait(60);

            var objectDestroyedReceivedMetricValues = waitForMetricEvent.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectDestroyedReceivedMetricValues.Count);

            var objectDestroyed = objectDestroyedReceivedMetricValues.First();
            Assert.AreEqual(m_Server.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(networkObject.NetworkObjectId, objectDestroyed.NetworkId.NetworkId);
            // Assert.AreEqual(, objectDestroyed.NetworkId.Name); // What?
        }
    }
#endif
}
