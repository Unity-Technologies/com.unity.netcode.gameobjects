using System.Collections;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{

    [TestFixture(NetworkTopologyTypes.DistributedAuthority)]
    [TestFixture(NetworkTopologyTypes.ClientServer)]
    internal class NetworkObjectOwnershipPropertiesTests : NetcodeIntegrationTest
    {
        private class DummyNetworkBehaviour : NetworkBehaviour
        {

        }

        protected override int NumberOfClients => 2;
        private GameObject m_PrefabToSpawn;
        private NetworkObject m_OwnerSpawnedInstance;
        private NetworkObject m_TargetOwnerInstance;
        private NetworkManager m_InitialOwner;
        private NetworkManager m_NextTargetOwner;

        private ulong m_InitialOwnerId;
        private ulong m_TargetOwnerId;
        private bool m_SpawnedInstanceIsOwner;
        private bool m_InitialOwnerOwnedBySever;
        private bool m_TargetOwnerOwnedBySever;

        public NetworkObjectOwnershipPropertiesTests(NetworkTopologyTypes networkTopologyType) : base(networkTopologyType) { }

        protected override IEnumerator OnTearDown()
        {
            m_OwnerSpawnedInstance = null;
            m_InitialOwner = null;
            m_NextTargetOwner = null;
            m_PrefabToSpawn = null;
            return base.OnTearDown();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("ClientOwnedObject");
            m_PrefabToSpawn.gameObject.AddComponent<DummyNetworkBehaviour>();
            m_PrefabToSpawn.GetComponent<NetworkObject>().SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
        }

        public enum InstanceTypes
        {
            Authority,
            NonAuthority,
        }

        private bool OwnershipPropagated(StringBuilder errorLog)
        {
            var conditionMet = true;

            foreach (var manager in m_NetworkManagers)
            {
                if (!manager.SpawnManager.SpawnedObjects.TryGetValue(m_OwnerSpawnedInstance.NetworkObjectId, out var networkObject))
                {
                    conditionMet = false;
                    errorLog.AppendLine($"Client-{manager.LocalClientId} has not spawned {m_OwnerSpawnedInstance.name}");

                }
                else if (networkObject.OwnerClientId != m_NextTargetOwner.LocalClientId)
                {
                    conditionMet = false;
                    errorLog.AppendLine($"Client-{manager.LocalClientId} has incorrect ownership set for {m_OwnerSpawnedInstance.name} ({m_OwnerSpawnedInstance.NetworkObjectId})");
                }
            }

            return conditionMet;
        }

        private void ValidateOwnerShipProperties(bool targetIsOwner = false)
        {
            Assert.AreEqual(m_OwnerSpawnedInstance.IsOwner, m_SpawnedInstanceIsOwner);
            Assert.AreEqual(m_OwnerSpawnedInstance.IsOwnedByServer, m_InitialOwnerOwnedBySever);
            Assert.AreEqual(targetIsOwner ? m_TargetOwnerId : m_InitialOwnerId, m_OwnerSpawnedInstance.OwnerClientId);

            var initialOwnerBehaviour = m_OwnerSpawnedInstance.GetComponent<DummyNetworkBehaviour>();
            Assert.AreEqual(initialOwnerBehaviour.IsOwner, m_SpawnedInstanceIsOwner);
            Assert.AreEqual(initialOwnerBehaviour.IsOwnedByServer, m_InitialOwnerOwnedBySever);
            Assert.AreEqual(targetIsOwner ? m_TargetOwnerId : m_InitialOwnerId, initialOwnerBehaviour.OwnerClientId);

            Assert.AreEqual(m_TargetOwnerInstance.IsOwner, targetIsOwner);
            Assert.AreEqual(m_TargetOwnerInstance.IsOwnedByServer, m_TargetOwnerOwnedBySever);

            Assert.AreEqual(targetIsOwner ? m_TargetOwnerId : m_InitialOwnerId, m_TargetOwnerInstance.OwnerClientId);
            var targetOwnerBehaviour = m_TargetOwnerInstance.GetComponent<DummyNetworkBehaviour>();
            Assert.AreEqual(targetOwnerBehaviour.IsOwner, targetIsOwner);
            Assert.AreEqual(targetOwnerBehaviour.IsOwnedByServer, m_TargetOwnerOwnedBySever);
            Assert.AreEqual(targetIsOwner ? m_TargetOwnerId : m_InitialOwnerId, m_TargetOwnerInstance.OwnerClientId);
        }


        [UnityTest]
        public IEnumerator ValidatePropertiesWithOwnershipChanges([Values(InstanceTypes.Authority, InstanceTypes.NonAuthority)] InstanceTypes instanceType)
        {
            var authority = GetAuthorityNetworkManager();
            var firstClient = GetNonAuthorityNetworkManager(0);
            var secondClient = GetNonAuthorityNetworkManager(1);

            m_NextTargetOwner = instanceType == InstanceTypes.Authority ? authority : firstClient;
            m_InitialOwner = instanceType == InstanceTypes.NonAuthority ? authority : firstClient;

            // In distributed authority mode, we will check client owner to DAHost owner with InstanceTypes.Authority and client owner to client
            // when InstanceTypes.NonAuthority
            if (m_DistributedAuthority)
            {
                m_InitialOwner = firstClient;
                if (instanceType == InstanceTypes.NonAuthority)
                {
                    m_NextTargetOwner = secondClient;
                }
                m_PrefabToSpawn.GetComponent<NetworkObject>().SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            }

            m_InitialOwnerId = m_InitialOwner.LocalClientId;
            m_TargetOwnerId = m_NextTargetOwner.LocalClientId;
            m_InitialOwnerOwnedBySever = m_InitialOwner.IsServer;
            m_TargetOwnerOwnedBySever = m_InitialOwner.IsServer;
            var objectInstance = SpawnObject(m_PrefabToSpawn, m_InitialOwner);

            m_OwnerSpawnedInstance = objectInstance.GetComponent<NetworkObject>();
            m_SpawnedInstanceIsOwner = m_OwnerSpawnedInstance.NetworkManager == m_InitialOwner;
            // Sanity check to verify that the next owner to target is not the owner of the spawned object
            var hasEntry = m_InitialOwner.SpawnManager.GetClientOwnedObjects(m_NextTargetOwner.LocalClientId).Any(x => x.NetworkObjectId == m_OwnerSpawnedInstance.NetworkObjectId);
            Assert.False(hasEntry);

            // Since CreateObjectMessage gets proxied by DAHost, just wait until the next target owner has the spawned instance in the s_GlobalNetworkObjects table.
            yield return WaitForConditionOrTimeOut(() => s_GlobalNetworkObjects.ContainsKey(m_NextTargetOwner.LocalClientId) && s_GlobalNetworkObjects[m_NextTargetOwner.LocalClientId].ContainsKey(m_OwnerSpawnedInstance.NetworkObjectId));
            AssertOnTimeout($"Timed out waiting for Client-{m_NextTargetOwner.LocalClientId} to have an instance entry of {m_OwnerSpawnedInstance.name}-{m_OwnerSpawnedInstance.NetworkObjectId}!");

            // Get the target client's instance of the spawned object
            m_TargetOwnerInstance = s_GlobalNetworkObjects[m_NextTargetOwner.LocalClientId][m_OwnerSpawnedInstance.NetworkObjectId];

            // Validate that NetworkObject and NetworkBehaviour ownership properties are correct
            ValidateOwnerShipProperties();

            // The authority always changes the ownership
            // Client-Server: It will always be the host instance
            // Distributed Authority: It can be either the DAHost or the client
            if (m_DistributedAuthority)
            {
                // Use the target client's instance to change ownership
                m_TargetOwnerInstance.ChangeOwnership(m_NextTargetOwner.LocalClientId);
                if (instanceType == InstanceTypes.NonAuthority)
                {
                    var networkManagersList = new System.Collections.Generic.List<NetworkManager> { authority, firstClient };
                    // Provide enough time for the client to receive and process the spawned message.
                    yield return WaitForMessageReceived<ChangeOwnershipMessage>(networkManagersList);
                }
                else
                {
                    var networkManagersList = new System.Collections.Generic.List<NetworkManager> { firstClient, secondClient };
                    // Provide enough time for the client to receive and process the change in ownership message.
                    yield return WaitForMessageReceived<ChangeOwnershipMessage>(networkManagersList);
                }
            }
            else
            {
                m_OwnerSpawnedInstance.ChangeOwnership(m_NextTargetOwner.LocalClientId);
                // Provide enough time for the client to receive and process the change in ownership message.
                yield return WaitForMessageReceived<ChangeOwnershipMessage>(m_ClientNetworkManagers.ToList());
            }

            // Ensure it's the ownership tables are updated
            yield return WaitForConditionOrTimeOut(OwnershipPropagated);
            AssertOnTimeout($"Timed out waiting for ownership to propagate!");

            m_SpawnedInstanceIsOwner = m_OwnerSpawnedInstance.NetworkManager == m_NextTargetOwner;
            if (m_SpawnedInstanceIsOwner)
            {
                m_InitialOwnerOwnedBySever = m_OwnerSpawnedInstance.NetworkManager.IsServer;
            }
            m_InitialOwnerOwnedBySever = m_NextTargetOwner.IsServer;
            m_TargetOwnerOwnedBySever = m_NextTargetOwner.IsServer;

            // Validate that NetworkObject and NetworkBehaviour ownership properties are correct
            ValidateOwnerShipProperties(true);
        }
    }
}
