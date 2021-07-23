using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI.Configuration;
using MLAPI.Metrics;
using MLAPI.RuntimeTests.Metrics.Utility;
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
            var initializer = new SingleClientMetricTestInitializer((server, clients) =>
            {
                MetricTestInitializer.CreateAndAssignPlayerPrefabs(server, clients);

                var gameObject = new GameObject(m_NewNetworkObjectName);
                m_NewNetworkObject = gameObject.AddComponent<NetworkObject>();
                m_NewNetworkObject.NetworkManagerOwner = server;
                MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(m_NewNetworkObject);
                var networkPrefab = new NetworkPrefab { Prefab = gameObject };
                server.NetworkConfig.NetworkPrefabs.Add(networkPrefab);

                foreach (var client in clients)
                {
                    client.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
                }
            });

            yield return initializer.Initialize();

            m_Server = initializer.Server;
            m_Client = initializer.Client;
            m_ClientMetrics = initializer.ClientMetrics;
            m_ServerMetrics = initializer.ServerMetrics;
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

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectSpawnedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedSentMetricValues.Count);

            var objectSpawned = objectSpawnedSentMetricValues.First();
            Assert.AreEqual(m_Client.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(m_NewNetworkObjectName, objectSpawned.NetworkId.Name);
            Assert.AreNotEqual(0, objectSpawned.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectSpawnReceivedMetric()
        {
            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(m_ClientMetrics.Dispatcher, MetricNames.ObjectSpawnedReceived);

            m_NewNetworkObject.Spawn();

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectSpawnedReceivedMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedReceivedMetricValues.Count);

            var objectSpawned = objectSpawnedReceivedMetricValues.First();
            Assert.AreEqual(m_Server.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(m_NewNetworkObject.NetworkObjectId, objectSpawned.NetworkId.NetworkId);
            Assert.AreEqual($"{m_NewNetworkObjectName}(Clone)", objectSpawned.NetworkId.Name);
            Assert.AreNotEqual(0, objectSpawned.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroySentMetric()
        {
            m_NewNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(m_ServerMetrics.Dispatcher, MetricNames.ObjectDestroyedSent);

            m_Server.SpawnManager.OnDespawnObject(m_NewNetworkObject, true);

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectDestroyedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, objectDestroyedSentMetricValues.Count); // As there's a client and server, this event is emitted twice.

            var objectDestroyed = objectDestroyedSentMetricValues.Last();
            Assert.AreEqual(m_Client.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(m_NewNetworkObjectName, objectDestroyed.NetworkId.Name);
            Assert.AreNotEqual(0, objectDestroyed.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroyReceivedMetric()
        {
            m_NewNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(m_ClientMetrics.Dispatcher, MetricNames.ObjectDestroyedReceived);
            
            m_Server.SpawnManager.OnDespawnObject(m_NewNetworkObject, true);
            
            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectDestroyedReceivedMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectDestroyedReceivedMetricValues.Count);

            var objectDestroyed = objectDestroyedReceivedMetricValues.First();
            Assert.AreEqual(m_Server.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(m_NewNetworkObject.NetworkObjectId, objectDestroyed.NetworkId.NetworkId);
            Assert.AreEqual($"{m_NewNetworkObjectName}(Clone)", objectDestroyed.NetworkId.Name);
            Assert.AreNotEqual(0, objectDestroyed.BytesCount);
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

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectSpawnedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, objectSpawnedSentMetricValues.Count); // As there's a client and server, this event is emitted twice.
            Assert.That(
                objectSpawnedSentMetricValues,
                Has.Exactly(1).Matches<ObjectSpawnedEvent>(
                    x => m_Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == m_NewNetworkObject.NetworkObjectId
                         && x.NetworkId.Name == m_NewNetworkObject.name));
            Assert.That(
                objectSpawnedSentMetricValues,
                Has.Exactly(1).Matches<ObjectSpawnedEvent>(
                    x => m_Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == anotherNetworkObject.NetworkObjectId
                         && x.NetworkId.Name == anotherNetworkObject.name));

            Assert.AreEqual(1, objectSpawnedSentMetricValues.Select(x => x.BytesCount).Distinct().Count());
            Assert.That(objectSpawnedSentMetricValues.Select(x => x.BytesCount), Has.All.Not.EqualTo(0));
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

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectDestroyedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, objectDestroyedSentMetricValues.Count); // As there's a client and server, this event is emitted twice.
            Assert.That(
                objectDestroyedSentMetricValues,
                Has.Exactly(1).Matches<ObjectDestroyedEvent>(
                    x => m_Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == m_NewNetworkObject.NetworkObjectId
                         && x.NetworkId.Name == m_NewNetworkObject.name));
            Assert.That(
                objectDestroyedSentMetricValues,
                Has.Exactly(1).Matches<ObjectDestroyedEvent>(
                    x => m_Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == anotherNetworkObject.NetworkObjectId
                         && x.NetworkId.Name == anotherNetworkObject.name));

            Assert.AreEqual(1, objectDestroyedSentMetricValues.Select(x => x.BytesCount).Distinct().Count());
            Assert.That(objectDestroyedSentMetricValues.Select(x => x.BytesCount), Has.All.Not.EqualTo(0));
        }
    }
}
