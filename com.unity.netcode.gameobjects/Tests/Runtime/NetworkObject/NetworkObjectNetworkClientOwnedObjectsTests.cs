using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectNetworkClientOwnedObjectsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private NetworkPrefab m_NetworkPrefab;
        protected override void OnServerAndClientsCreated()
        {
            // create prefab
            var gameObject = new GameObject("ClientOwnedObject");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            m_NetworkPrefab = (new NetworkPrefab()
            {
                Prefab = gameObject
            });

            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(m_NetworkPrefab);

            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.NetworkPrefabs.Add(m_NetworkPrefab);
            }
        }

        [UnityTest]
        public IEnumerator ChangeOwnershipOwnedObjectsAddTest()
        {
            NetworkObject serverObject = Object.Instantiate(m_NetworkPrefab.Prefab).GetComponent<NetworkObject>();
            serverObject.NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.Spawn();

            // Provide enough time for the client to receive and process the spawned message.
            yield return s_DefaultWaitForTick;

            // The object is owned by server
            Assert.False(m_ServerNetworkManager.SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));

            // Change the ownership
            serverObject.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            // Provide enough time for the client to receive and process the change in ownership message.
            yield return s_DefaultWaitForTick;

            // Ensure it's now added to the list
            yield return WaitForConditionOrTimeOut(() => m_ClientNetworkManagers[0].SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client to gain ownership!");
            Assert.True(m_ClientNetworkManagers[0].SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));
            Assert.True(m_ServerNetworkManager.SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));
        }
    }
}
