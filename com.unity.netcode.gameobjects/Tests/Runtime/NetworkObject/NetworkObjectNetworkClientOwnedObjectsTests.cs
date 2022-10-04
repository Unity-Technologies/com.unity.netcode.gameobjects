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
        private class DummyNetworkBehaviour : NetworkBehaviour
        {

        }

        protected override int NumberOfClients => 1;
        private NetworkPrefab m_NetworkPrefab;
        protected override void OnServerAndClientsCreated()
        {
            // create prefab
            var gameObject = new GameObject("ClientOwnedObject");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            gameObject.AddComponent<DummyNetworkBehaviour>();
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
            yield return WaitForMessageReceived<CreateObjectMessage>(m_ClientNetworkManagers.ToList());

            // The object is owned by server
            Assert.False(m_ServerNetworkManager.SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));

            // Change the ownership
            serverObject.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            // Provide enough time for the client to receive and process the change in ownership message.
            yield return WaitForMessageReceived<ChangeOwnershipMessage>(m_ClientNetworkManagers.ToList());

            // Ensure it's now added to the list
            Assert.True(m_ClientNetworkManagers[0].SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));
            Assert.True(m_ServerNetworkManager.SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));
        }

        [UnityTest]
        public IEnumerator WhenOwnershipIsChanged_OwnershipValuesUpdateCorrectly()
        {
            NetworkObject serverObject = Object.Instantiate(m_NetworkPrefab.Prefab).GetComponent<NetworkObject>();
            serverObject.NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.Spawn();

            // Provide enough time for the client to receive and process the spawned message.
            yield return WaitForMessageReceived<CreateObjectMessage>(m_ClientNetworkManagers.ToList());

            // The object is owned by server
            Assert.False(m_ServerNetworkManager.SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));

            // Change the ownership
            serverObject.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            // Provide enough time for the client to receive and process the change in ownership message.
            yield return WaitForMessageReceived<ChangeOwnershipMessage>(m_ClientNetworkManagers.ToList());

            Assert.IsFalse(serverObject.IsOwner);
            Assert.IsFalse(serverObject.IsOwnedByServer);
            Assert.AreEqual(m_ClientNetworkManagers[0].LocalClientId, serverObject.OwnerClientId);

            var serverBehaviour = serverObject.GetComponent<DummyNetworkBehaviour>();
            Assert.IsFalse(serverBehaviour.IsOwner);
            Assert.IsFalse(serverBehaviour.IsOwnedByServer);
            Assert.AreEqual(m_ClientNetworkManagers[0].LocalClientId, serverBehaviour.OwnerClientId);

            var clientObject = Object.FindObjectsOfType<NetworkObject>().Where((obj) => obj.NetworkManagerOwner == m_ClientNetworkManagers[0]).FirstOrDefault();

            Assert.IsNotNull(clientObject);
            Assert.IsTrue(clientObject.IsOwner);
            Assert.IsFalse(clientObject.IsOwnedByServer);
            Assert.AreEqual(m_ClientNetworkManagers[0].LocalClientId, clientObject.OwnerClientId);

            var clientBehaviour = clientObject.GetComponent<DummyNetworkBehaviour>();
            Assert.IsTrue(clientBehaviour.IsOwner);
            Assert.IsFalse(clientBehaviour.IsOwnedByServer);
            Assert.AreEqual(m_ClientNetworkManagers[0].LocalClientId, clientBehaviour.OwnerClientId);

            serverObject.RemoveOwnership();

            // Provide enough time for the client to receive and process the change in ownership message.
            yield return WaitForMessageReceived<ChangeOwnershipMessage>(m_ClientNetworkManagers.ToList());

            Assert.IsTrue(serverObject.IsOwner);
            Assert.IsTrue(serverObject.IsOwnedByServer);
            Assert.AreEqual(NetworkManager.ServerClientId, serverObject.OwnerClientId);
            Assert.IsTrue(serverBehaviour.IsOwner);
            Assert.IsTrue(serverBehaviour.IsOwnedByServer);
            Assert.AreEqual(NetworkManager.ServerClientId, serverBehaviour.OwnerClientId);

            Assert.IsFalse(clientObject.IsOwner);
            Assert.IsTrue(clientObject.IsOwnedByServer);
            Assert.AreEqual(NetworkManager.ServerClientId, clientObject.OwnerClientId);
            Assert.IsFalse(clientBehaviour.IsOwner);
            Assert.IsTrue(clientBehaviour.IsOwnedByServer);
            Assert.AreEqual(NetworkManager.ServerClientId, clientBehaviour.OwnerClientId);
        }
    }
}
