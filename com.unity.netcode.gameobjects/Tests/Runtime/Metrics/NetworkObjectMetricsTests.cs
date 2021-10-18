#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class NetworkObjectMetricsTests : SingleClientMetricTestBase
    {
        private const string k_NewNetworkObjectName = "TestNetworkObjectToSpawn";
        private NetworkObject m_NewNetworkPrefab;

        protected override Action<GameObject> UpdatePlayerPrefab => _ =>
        {
            var gameObject = new GameObject(k_NewNetworkObjectName);
            m_NewNetworkPrefab = gameObject.AddComponent<NetworkObject>();
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(m_NewNetworkPrefab);

            var networkPrefab = new NetworkPrefab { Prefab = gameObject };
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            }
        };

        private NetworkObject SpawnNetworkObject()
        {
            // Spawn another network object so we can hide multiple.
            var gameObject = UnityEngine.Object.Instantiate(m_NewNetworkPrefab); // new GameObject(NewNetworkObjectName);
            var networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = Server;
            networkObject.Spawn();

            return networkObject;
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectSpawnSentMetric()
        {
            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.ObjectSpawnedSent);

            SpawnNetworkObject();

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectSpawnedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedSentMetricValues.Count);

            var objectSpawned = objectSpawnedSentMetricValues.Last();
            Assert.AreEqual(Client.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual($"{k_NewNetworkObjectName}(Clone)", objectSpawned.NetworkId.Name);
            Assert.AreNotEqual(0, objectSpawned.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectSpawnReceivedMetric()
        {
            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.ObjectSpawnedReceived);

            var networkObject = SpawnNetworkObject();

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectSpawnedReceivedMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectSpawnedReceivedMetricValues.Count);

            var objectSpawned = objectSpawnedReceivedMetricValues.First();
            Assert.AreEqual(Server.LocalClientId, objectSpawned.Connection.Id);
            Assert.AreEqual(networkObject.NetworkObjectId, objectSpawned.NetworkId.NetworkId);
            Assert.AreEqual($"{k_NewNetworkObjectName}(Clone)", objectSpawned.NetworkId.Name);
            Assert.AreNotEqual(0, objectSpawned.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroySentMetric()
        {
            var networkObject = SpawnNetworkObject();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.ObjectDestroyedSent);

            Server.SpawnManager.OnDespawnObject(networkObject, true);

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectDestroyedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, objectDestroyedSentMetricValues.Count); // As there's a client and server, this event is emitted twice.

            var objectDestroyed = objectDestroyedSentMetricValues.Last();
            Assert.AreEqual(Client.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual($"{k_NewNetworkObjectName}(Clone)", objectDestroyed.NetworkId.Name);
            Assert.AreNotEqual(0, objectDestroyed.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNetworkObjectDestroyReceivedMetric()
        {
            var networkObject = SpawnNetworkObject();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.ObjectDestroyedReceived);

            Server.SpawnManager.OnDespawnObject(networkObject, true);

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectDestroyedReceivedMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, objectDestroyedReceivedMetricValues.Count);

            var objectDestroyed = objectDestroyedReceivedMetricValues.First();
            Assert.AreEqual(Server.LocalClientId, objectDestroyed.Connection.Id);
            Assert.AreEqual(networkObject.NetworkObjectId, objectDestroyed.NetworkId.NetworkId);
            Assert.AreEqual($"{k_NewNetworkObjectName}(Clone)", objectDestroyed.NetworkId.Name);
            Assert.AreNotEqual(0, objectDestroyed.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackMultipleNetworkObjectSpawnSentMetric()
        {
            var networkObject1 = SpawnNetworkObject();
            var networkObject2 = SpawnNetworkObject();

            yield return new WaitForSeconds(0.2f);

            NetworkObject.NetworkHide(new List<NetworkObject> { networkObject1, networkObject2 }, Client.LocalClientId);

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectSpawnedEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.ObjectSpawnedSent);

            NetworkObject.NetworkShow(new List<NetworkObject> { networkObject1, networkObject2 }, Client.LocalClientId);

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectSpawnedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, objectSpawnedSentMetricValues.Count); // As there's a client and server, this event is emitted twice.
            Assert.That(
                objectSpawnedSentMetricValues,
                Has.Exactly(1).Matches<ObjectSpawnedEvent>(
                    x => Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == networkObject1.NetworkObjectId
                         && x.NetworkId.Name == networkObject1.name));
            Assert.That(
                objectSpawnedSentMetricValues,
                Has.Exactly(1).Matches<ObjectSpawnedEvent>(
                    x => Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == networkObject2.NetworkObjectId
                         && x.NetworkId.Name == networkObject2.name));

            Assert.AreEqual(1, objectSpawnedSentMetricValues.Select(x => x.BytesCount).Distinct().Count());
            Assert.That(objectSpawnedSentMetricValues.Select(x => x.BytesCount), Has.All.Not.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TrackMultipleNetworkObjectDestroySentMetric()
        {
            var networkObject1 = SpawnNetworkObject();
            var networkObject2 = SpawnNetworkObject();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricEvent = new WaitForMetricValues<ObjectDestroyedEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.ObjectDestroyedSent);

            NetworkObject.NetworkHide(new List<NetworkObject> { networkObject1, networkObject2 }, Client.LocalClientId);

            yield return waitForMetricEvent.WaitForMetricsReceived();

            var objectDestroyedSentMetricValues = waitForMetricEvent.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, objectDestroyedSentMetricValues.Count); // As there's a client and server, this event is emitted twice.
            Assert.That(
                objectDestroyedSentMetricValues,
                Has.Exactly(1).Matches<ObjectDestroyedEvent>(
                    x => Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == networkObject1.NetworkObjectId
                         && x.NetworkId.Name == networkObject1.name));
            Assert.That(
                objectDestroyedSentMetricValues,
                Has.Exactly(1).Matches<ObjectDestroyedEvent>(
                    x => Client.LocalClientId == x.Connection.Id
                         && x.NetworkId.NetworkId == networkObject2.NetworkObjectId
                         && x.NetworkId.Name == networkObject2.name));

            Assert.AreEqual(1, objectDestroyedSentMetricValues.Select(x => x.BytesCount).Distinct().Count());
            Assert.That(objectDestroyedSentMetricValues.Select(x => x.BytesCount), Has.All.Not.EqualTo(0));
        }
    }
}
#endif
