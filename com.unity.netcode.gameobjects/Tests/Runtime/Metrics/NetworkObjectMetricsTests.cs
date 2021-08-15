#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utlity;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    public class NetworkObjectMetricsTests : SingleClientMetricTestBase
    {
        const string NewNetworkObjectName = "TestNetworkObjectToSpawn";

        NetworkObject m_NewNetworkObject;

        protected override Action<GameObject> UpdatePlayerPrefab => _ =>
        {
            var gameObject = new GameObject(NewNetworkObjectName);
            m_NewNetworkObject = gameObject.AddComponent<NetworkObject>();
            m_NewNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(m_NewNetworkObject);
            var networkPrefab = new NetworkPrefab { Prefab = gameObject };
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);

            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            }
        };

        [UnityTest]
        public IEnumerator TrackNetworkObjectSpawnSentMetric()
        {
            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(ServerMetrics.Dispatcher, MetricNames.ObjectSpawnedSent);

            m_NewNetworkObject.Spawn();

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectSpawnedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedSentMetricValues.Count);

            var objectSpawned = objectSpawnedSentMetricValues.First();
            Assert.AreEqual(Client.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(NewNetworkObjectName, objectSpawned.NetworkId.Name);
            Assert.AreNotEqual(0, objectSpawned.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectSpawnReceivedMetric()
        {
            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(ClientMetrics.Dispatcher, MetricNames.ObjectSpawnedReceived);

            m_NewNetworkObject.Spawn();

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectSpawnedReceivedMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedReceivedMetricValues.Count);

            var objectSpawned = objectSpawnedReceivedMetricValues.First();
            Assert.AreEqual(Server.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(m_NewNetworkObject.NetworkObjectId, objectSpawned.NetworkId.NetworkId);
            Assert.AreEqual($"{NewNetworkObjectName}(Clone)", objectSpawned.NetworkId.Name);
            Assert.AreNotEqual(0, objectSpawned.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroySentMetric()
        {
            m_NewNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(ServerMetrics.Dispatcher, MetricNames.ObjectDestroyedSent);

            Server.SpawnManager.OnDespawnObject(m_NewNetworkObject, true);

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectDestroyedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, objectDestroyedSentMetricValues.Count); // As there's a client and server, this event is emitted twice.

            var objectDestroyed = objectDestroyedSentMetricValues.Last();
            Assert.AreEqual(Client.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(NewNetworkObjectName, objectDestroyed.NetworkId.Name);
            Assert.AreNotEqual(0, objectDestroyed.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroyReceivedMetric()
        {
            m_NewNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(ClientMetrics.Dispatcher, MetricNames.ObjectDestroyedReceived);

            Server.SpawnManager.OnDespawnObject(m_NewNetworkObject, true);

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectDestroyedReceivedMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectDestroyedReceivedMetricValues.Count);

            var objectDestroyed = objectDestroyedReceivedMetricValues.First();
            Assert.AreEqual(Server.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(m_NewNetworkObject.NetworkObjectId, objectDestroyed.NetworkId.NetworkId);
            Assert.AreEqual($"{NewNetworkObjectName}(Clone)", objectDestroyed.NetworkId.Name);
            Assert.AreNotEqual(0, objectDestroyed.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackMultipleNetworkObjectSpawnSentMetric()
        {
            m_NewNetworkObject.Spawn();

            // Spawn another network object so we can hide multiple.
            var gameObject = new GameObject(NewNetworkObjectName);
            var anotherNetworkObject = gameObject.AddComponent<NetworkObject>();
            anotherNetworkObject.NetworkManagerOwner = Server;
            anotherNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            NetworkObject.NetworkHide(new List<NetworkObject>{m_NewNetworkObject, anotherNetworkObject}, Client.LocalClientId);

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(ServerMetrics.Dispatcher, MetricNames.ObjectSpawnedSent);

            NetworkObject.NetworkShow(new List<NetworkObject>{m_NewNetworkObject, anotherNetworkObject}, Client.LocalClientId);

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectSpawnedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, objectSpawnedSentMetricValues.Count); // As there's a client and server, this event is emitted twice.
            Assert.That(
                objectSpawnedSentMetricValues,
                Has.Exactly(1).Matches<ObjectSpawnedEvent>(
                    x => Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == m_NewNetworkObject.NetworkObjectId
                         && x.NetworkId.Name == m_NewNetworkObject.name));
            Assert.That(
                objectSpawnedSentMetricValues,
                Has.Exactly(1).Matches<ObjectSpawnedEvent>(
                    x => Client.LocalClientId == x.Connection.Id
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
            var gameObject = new GameObject(NewNetworkObjectName);
            var anotherNetworkObject = gameObject.AddComponent<NetworkObject>();
            anotherNetworkObject.NetworkManagerOwner = Server;
            anotherNetworkObject.Spawn();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(ServerMetrics.Dispatcher, MetricNames.ObjectDestroyedSent);

            NetworkObject.NetworkHide(new List<NetworkObject>{m_NewNetworkObject, anotherNetworkObject}, Client.LocalClientId);

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectDestroyedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, objectDestroyedSentMetricValues.Count); // As there's a client and server, this event is emitted twice.
            Assert.That(
                objectDestroyedSentMetricValues,
                Has.Exactly(1).Matches<ObjectDestroyedEvent>(
                    x => Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == m_NewNetworkObject.NetworkObjectId
                         && x.NetworkId.Name == m_NewNetworkObject.name));
            Assert.That(
                objectDestroyedSentMetricValues,
                Has.Exactly(1).Matches<ObjectDestroyedEvent>(
                    x => Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == anotherNetworkObject.NetworkObjectId
                         && x.NetworkId.Name == anotherNetworkObject.name));

            Assert.AreEqual(1, objectDestroyedSentMetricValues.Select(x => x.BytesCount).Distinct().Count());
            Assert.That(objectDestroyedSentMetricValues.Select(x => x.BytesCount), Has.All.Not.EqualTo(0));
        }
    }
}
#endif
