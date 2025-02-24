#if MULTIPLAYER_TOOLS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.TestHelpers.Runtime.Metrics;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class OwnershipChangeMetricsTests : SingleClientMetricTestBase
    {
        private const string k_NewNetworkObjectName = "TestNetworkObjectToSpawn";
        private NetworkObject m_NewNetworkPrefab;
        // Header is dynamically sized due to packing, will be 2 bytes for all test messages.
        private const int k_MessageHeaderSize = 2;

        protected override void OnServerAndClientsCreated()
        {
            var gameObject = new GameObject(k_NewNetworkObjectName);
            m_NewNetworkPrefab = gameObject.AddComponent<NetworkObject>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(m_NewNetworkPrefab);

            var networkPrefab = new NetworkPrefab { Prefab = gameObject };
            m_ServerNetworkManager.NetworkConfig.Prefabs.Add(networkPrefab);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.Prefabs.Add(networkPrefab);
            }
            base.OnServerAndClientsCreated();
        }

        private NetworkObject SpawnNetworkObject()
        {
            // Spawn another network object so we can hide multiple.
            var gameObject = Object.Instantiate(m_NewNetworkPrefab); // new GameObject(NewNetworkObjectName);
            var networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = Server;
            networkObject.Spawn();

            return networkObject;
        }

        private int GetWriteSizeForOwnerChange(NetworkObject networkObject, ulong newOwner)
        {
            var message = new ChangeOwnershipMessage
            {
                NetworkObjectId = networkObject.NetworkObjectId,
                OwnerClientId = newOwner
            };
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            message.Serialize(writer, message.Version);
            return writer.Length;
        }

        [UnityTest]
        public IEnumerator TrackOwnershipChangeSentMetric()
        {
            var networkObject = SpawnNetworkObject();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricValues = new WaitForEventMetricValues<OwnershipChangeEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.OwnershipChangeSent);

            networkObject.ChangeOwnership(1);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();

            var ownershipChangeSent = metricValues.First();
            Assert.AreEqual(networkObject.NetworkObjectId, ownershipChangeSent.NetworkId.NetworkId);
            Assert.AreEqual(Server.LocalClientId, ownershipChangeSent.Connection.Id);
            Assert.AreEqual(0, ownershipChangeSent.BytesCount);

            // The first metric is to the server(self), so its size is now correctly reported as 0.
            // Let's check the last one instead, to have a valid value
            ownershipChangeSent = metricValues.Last();
            Assert.AreEqual(networkObject.NetworkObjectId, ownershipChangeSent.NetworkId.NetworkId);
            Assert.AreEqual(Client.LocalClientId, ownershipChangeSent.Connection.Id);

            var serializedLength = GetWriteSizeForOwnerChange(networkObject, 1);
            Assert.AreEqual(serializedLength + k_MessageHeaderSize, ownershipChangeSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackOwnershipChangeReceivedMetric()
        {
            var networkObject = SpawnNetworkObject();

            yield return new WaitForSeconds(0.2f);

            var waitForMetricValues = new WaitForEventMetricValues<OwnershipChangeEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.OwnershipChangeReceived);

            networkObject.ChangeOwnership(1);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, metricValues.Count);

            var ownershipChangeReceived = metricValues.First();
            Assert.AreEqual(networkObject.NetworkObjectId, ownershipChangeReceived.NetworkId.NetworkId);

            var serializedLength = GetWriteSizeForOwnerChange(networkObject, 1);
            Assert.AreEqual(serializedLength, ownershipChangeReceived.BytesCount);
        }
    }
}
#endif
