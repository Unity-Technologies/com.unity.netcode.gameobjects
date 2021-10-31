#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class OwnershipChangeMetricsTests : SingleClientMetricTestBase
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
        public IEnumerator TrackOwnershipChangeSentMetric()
        {
            var networkObject = SpawnNetworkObject();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricValues = new WaitForMetricValues<OwnershipChangeEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.OwnershipChangeSent);

            networkObject.ChangeOwnership(1);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();

            var ownershipChangeSent = metricValues.First();
            Assert.AreEqual(networkObject.NetworkObjectId, ownershipChangeSent.NetworkId.NetworkId);
            Assert.AreEqual(Server.LocalClientId, ownershipChangeSent.Connection.Id);
            Assert.AreEqual(FastBufferWriter.GetWriteSize<ChangeOwnershipMessage>() + FastBufferWriter.GetWriteSize<MessageHeader>(), ownershipChangeSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackOwnershipChangeReceivedMetric()
        {
            var networkObject = SpawnNetworkObject();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricValues = new WaitForMetricValues<OwnershipChangeEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.OwnershipChangeReceived);

            networkObject.ChangeOwnership(1);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, metricValues.Count);

            var ownershipChangeReceived = metricValues.First();
            Assert.AreEqual(networkObject.NetworkObjectId, ownershipChangeReceived.NetworkId.NetworkId);
            Assert.AreEqual(FastBufferWriter.GetWriteSize<ChangeOwnershipMessage>(), ownershipChangeReceived.BytesCount);
        }
    }
}
#endif
