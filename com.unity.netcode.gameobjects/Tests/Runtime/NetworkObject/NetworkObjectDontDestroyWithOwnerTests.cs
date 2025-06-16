using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;


namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class NetworkObjectDontDestroyWithOwnerTests : NetcodeIntegrationTest
    {
        private const int k_NumberObjectsToSpawn = 32;
        protected override int NumberOfClients => 1;

        protected GameObject m_PrefabToSpawn;
        private GameObject m_PrefabNoObserversSpawn;

        public NetworkObjectDontDestroyWithOwnerTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("ClientOwnedObject");
            m_PrefabToSpawn.GetComponent<NetworkObject>().DontDestroyWithOwner = true;

            m_PrefabNoObserversSpawn = CreateNetworkObjectPrefab("NoObserversObject");
            var prefabNoObserversNetworkObject = m_PrefabNoObserversSpawn.GetComponent<NetworkObject>();
            prefabNoObserversNetworkObject.SpawnWithObservers = false;
            prefabNoObserversNetworkObject.DontDestroyWithOwner = true;
        }

        [UnityTest]
        public IEnumerator DontDestroyWithOwnerTest()
        {
            var client = m_ClientNetworkManagers[0];
            var clientId = client.LocalClientId;
            var networkObjects = SpawnObjects(m_PrefabToSpawn, m_ClientNetworkManagers[0], k_NumberObjectsToSpawn);

            // wait for object spawn on client to reach k_NumberObjectsToSpawn + 1 (k_NumberObjectsToSpawn and 1 for the player)
            yield return WaitForConditionOrTimeOut(() => client.SpawnManager.GetClientOwnedObjects(clientId).Count() == k_NumberObjectsToSpawn + 1);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client to have 33 NetworkObjects spawned! Only {client.SpawnManager.GetClientOwnedObjects(clientId).Count()} were assigned!");

            // disconnect the client that owns all the clients
            NetcodeIntegrationTestHelpers.StopOneClient(client);

            var remainingClients = Mathf.Max(0, TotalClients - 1);
            // wait for disconnect
            yield return WaitForConditionOrTimeOut(() => m_ServerNetworkManager.ConnectedClients.Count == remainingClients);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for client to disconnect!");

            for (int i = 0; i < networkObjects.Count; i++)
            {
                var networkObject = networkObjects[i].GetComponent<NetworkObject>();
                // ensure ownership was transferred back
                Assert.That(networkObject.OwnerClientId == m_ServerNetworkManager.LocalClientId);
            }
        }

        /// <summary>
        /// Validates that when a <see cref="NetworkObject"/> is spawned with no observers
        /// and has the <see cref="NetworkObject.DontDestroyWithOwner"/> property set, that
        /// upon showing the <see cref="NetworkObject"/> to a client and changing the owner
        /// to the client that upon the client disconnecting the <see cref="NetworkObject"/>
        /// continues to persist in the network session.
        /// </summary>
        /// <returns><see cref="IEnumerator"/></returns>
        [UnityTest]
        public IEnumerator NetworkShowThenClientDisconnects()
        {
            var authorityManager = m_ServerNetworkManager;
            var networkObject = SpawnObject(m_PrefabNoObserversSpawn, authorityManager).GetComponent<NetworkObject>();
            var longWait = new WaitForSeconds(0.25f);
            yield return longWait;
            var nonAuthorityManager = m_ClientNetworkManagers[0];
            Assert.False(nonAuthorityManager.SpawnManager.SpawnedObjects.ContainsKey(networkObject.NetworkObjectId), $"[Client-{nonAuthorityManager.LocalClientId}] " +
                $"Already has an instance of {networkObject.name} when it should not!");
            networkObject.NetworkShow(nonAuthorityManager.LocalClientId);
            networkObject.ChangeOwnership(nonAuthorityManager.LocalClientId);

            yield return WaitForConditionOrTimeOut(() => nonAuthorityManager.SpawnManager.SpawnedObjects.ContainsKey(networkObject.NetworkObjectId)
            && nonAuthorityManager.SpawnManager.SpawnedObjects[networkObject.NetworkObjectId].OwnerClientId == nonAuthorityManager.LocalClientId);
            AssertOnTimeout($"[Client-{nonAuthorityManager.LocalClientId}] Failed to spawn {networkObject.name} when it was shown!");

            yield return s_DefaultWaitForTick;

            nonAuthorityManager.Shutdown();

            yield return longWait;
            Assert.True(networkObject.IsSpawned, $"The spawned test prefab was despawned on the authority side when it shouldn't have been!");
        }
    }
}
