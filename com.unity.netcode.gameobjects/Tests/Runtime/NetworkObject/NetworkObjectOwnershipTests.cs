using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectOwnershipComponent : NetworkBehaviour
    {
        public bool OnLostOwnershipFired = false;
        public ulong CachedOwnerIdOnLostOwnership = 0;

        public override void OnLostOwnership()
        {
            OnLostOwnershipFired = true;
            CachedOwnerIdOnLostOwnership = OwnerClientId;
        }

        public bool OnGainedOwnershipFired = false;
        public ulong CachedOwnerIdOnGainedOwnership = 0;

        public override void OnGainedOwnership()
        {
            OnGainedOwnershipFired = true;
            CachedOwnerIdOnGainedOwnership = OwnerClientId;
        }
    }

    [TestFixture(true)]
    [TestFixture(false)]
    public class NetworkObjectOwnershipTests
    {
        private const int k_ClientInstanceCount = 1;

        private NetworkManager m_ServerNetworkManager;
        private NetworkManager[] m_ClientNetworkManagers;

        private GameObject m_DummyPrefab;
        private GameObject m_DummyGameObject;

        private readonly bool m_IsHost;

        public NetworkObjectOwnershipTests(bool isHost)
        {
            m_IsHost = isHost;
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // we need at least 1 client for tests
            Assert.That(k_ClientInstanceCount, Is.GreaterThan(0));

            // create NetworkManager instances
            Assert.That(NetcodeIntegrationTestHelpers.Create(k_ClientInstanceCount, out m_ServerNetworkManager, out m_ClientNetworkManagers));
            Assert.That(m_ServerNetworkManager, Is.Not.Null);
            Assert.That(m_ClientNetworkManagers, Is.Not.Null);
            Assert.That(m_ClientNetworkManagers.Length, Is.EqualTo(k_ClientInstanceCount));

            // create and register our ad-hoc DummyPrefab (we'll spawn it later during tests)
            m_DummyPrefab = new GameObject("DummyPrefabPrototype");
            m_DummyPrefab.AddComponent<NetworkObject>();
            m_DummyPrefab.AddComponent<NetworkObjectOwnershipComponent>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(m_DummyPrefab.GetComponent<NetworkObject>());
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab { Prefab = m_DummyPrefab });
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab { Prefab = m_DummyPrefab });
            }

            // start server and client NetworkManager instances
            Assert.That(NetcodeIntegrationTestHelpers.Start(m_IsHost, m_ServerNetworkManager, m_ClientNetworkManagers));

            // wait for connection on client side
            yield return NetcodeIntegrationTestHelpers.WaitForClientsConnected(m_ClientNetworkManagers);

            // wait for connection on server side
            yield return NetcodeIntegrationTestHelpers.WaitForClientConnectedToServer(m_ServerNetworkManager);
        }

        [TearDown]
        public void Teardown()
        {
            NetcodeIntegrationTestHelpers.Destroy();

            if (m_DummyGameObject != null)
            {
                Object.DestroyImmediate(m_DummyGameObject);
            }

            if (m_DummyPrefab != null)
            {
                Object.DestroyImmediate(m_DummyPrefab);
            }
        }

        [UnityTest]
        public IEnumerator TestOwnershipCallbacks()
        {
            m_DummyGameObject = Object.Instantiate(m_DummyPrefab);
            var dummyNetworkObject = m_DummyGameObject.GetComponent<NetworkObject>();
            Assert.That(dummyNetworkObject, Is.Not.Null);

            dummyNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            dummyNetworkObject.Spawn();
            var dummyNetworkObjectId = dummyNetworkObject.NetworkObjectId;
            Assert.That(dummyNetworkObjectId, Is.GreaterThan(0));

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfType<CreateObjectMessage>(m_ClientNetworkManagers[0]);

            Assert.That(m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(dummyNetworkObjectId));
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                Assert.That(clientNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(dummyNetworkObjectId));
            }

            // Verifies that removing the ownership when the default (server) is already set does not cause
            // a Key Not Found Exception
            m_ServerNetworkManager.SpawnManager.RemoveOwnership(dummyNetworkObject);

            var serverObject = m_ServerNetworkManager.SpawnManager.SpawnedObjects[dummyNetworkObjectId];
            var clientObject = m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects[dummyNetworkObjectId];
            Assert.That(serverObject, Is.Not.Null);
            Assert.That(clientObject, Is.Not.Null);

            var serverComponent = serverObject.GetComponent<NetworkObjectOwnershipComponent>();
            var clientComponent = clientObject.GetComponent<NetworkObjectOwnershipComponent>();
            Assert.That(serverComponent, Is.Not.Null);
            Assert.That(clientComponent, Is.Not.Null);


            Assert.That(serverObject.OwnerClientId, Is.EqualTo(m_ServerNetworkManager.ServerClientId));
            Assert.That(clientObject.OwnerClientId, Is.EqualTo(m_ClientNetworkManagers[0].ServerClientId));

            Assert.That(m_ServerNetworkManager.ConnectedClients.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            serverObject.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfType<ChangeOwnershipMessage>(m_ClientNetworkManagers[0]);


            Assert.That(clientComponent.OnGainedOwnershipFired);
            Assert.That(clientComponent.CachedOwnerIdOnGainedOwnership, Is.EqualTo(m_ClientNetworkManagers[0].LocalClientId));
            serverObject.ChangeOwnership(m_ServerNetworkManager.ServerClientId);

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfType<ChangeOwnershipMessage>(m_ClientNetworkManagers[0]);

            Assert.That(serverObject.OwnerClientId, Is.EqualTo(m_ServerNetworkManager.LocalClientId));
            Assert.That(clientComponent.OnLostOwnershipFired);
            Assert.That(clientComponent.CachedOwnerIdOnLostOwnership, Is.EqualTo(m_ClientNetworkManagers[0].LocalClientId));
        }
    }
}
