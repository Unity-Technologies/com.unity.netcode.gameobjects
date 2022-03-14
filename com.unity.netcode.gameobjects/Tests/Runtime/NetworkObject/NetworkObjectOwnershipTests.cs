using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectOwnershipComponent : NetworkBehaviour
    {
        public bool OnLostOwnershipFired = false;
        public bool OnGainedOwnershipFired = false;

        public override void OnLostOwnership()
        {
            OnLostOwnershipFired = true;
        }

        public override void OnGainedOwnership()
        {
            OnGainedOwnershipFired = true;
        }
        
        public void ResetFlags()
        {
            OnLostOwnershipFired = false;
            OnGainedOwnershipFired = false;
        }
    }

    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class NetworkObjectOwnershipTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_OwnershipPrefab;
        private GameObject m_OwnershipObject;
        private NetworkObject m_OwnershipNetworkObject;

        public NetworkObjectOwnershipTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnServerAndClientsCreated()
        {
            m_OwnershipPrefab = CreateNetworkObjectPrefab("OnwershipPrefab");
            m_OwnershipPrefab.AddComponent<NetworkObjectOwnershipComponent>();
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_OwnershipObject = SpawnObject(m_OwnershipPrefab, m_ServerNetworkManager);
            m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfType<CreateObjectMessage>(m_ClientNetworkManagers[0]);
        }

        [Test]
        public void TestPlayerIsOwned()
        {
            var clientOwnedObjects = m_ClientNetworkManagers[0].SpawnManager.SpawnedObjectsList.Where(x => x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId);

            var clientPlayerObject = clientOwnedObjects.Where((c) => c.IsLocalPlayer).FirstOrDefault();
            Assert.NotNull(clientPlayerObject, $"Client Id {m_ClientNetworkManagers[0].LocalClientId} does not have its local player marked as an owned object!");
        }

        [UnityTest]
        public IEnumerator TestOwnershipCallbacks()
        {
            var dummyNetworkObjectId = m_OwnershipNetworkObject.NetworkObjectId;
            Assert.That(dummyNetworkObjectId, Is.GreaterThan(0));
            Assert.That(m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(dummyNetworkObjectId));
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                Assert.That(clientNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(dummyNetworkObjectId));
            }

            // Verifies that removing the ownership when the default (server) is already set does not cause a Key Not Found Exception
            m_ServerNetworkManager.SpawnManager.RemoveOwnership(m_OwnershipNetworkObject);

            var serverObject = m_ServerNetworkManager.SpawnManager.SpawnedObjects[dummyNetworkObjectId];
            var clientObject = m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects[dummyNetworkObjectId];
            Assert.That(serverObject, Is.Not.Null);
            Assert.That(clientObject, Is.Not.Null);

            var serverComponent = serverObject.GetComponent<NetworkObjectOwnershipComponent>();
            var clientComponent = clientObject.GetComponent<NetworkObjectOwnershipComponent>();
            Assert.That(serverComponent, Is.Not.Null);
            Assert.That(clientComponent, Is.Not.Null);

            Assert.That(serverObject.OwnerClientId, Is.EqualTo(NetworkManager.ServerClientId));
            Assert.That(clientObject.OwnerClientId, Is.EqualTo(NetworkManager.ServerClientId));

            Assert.That(m_ServerNetworkManager.ConnectedClients.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));

            serverObject.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);
            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 2);

            Assert.That(serverComponent.OnLostOwnershipFired);
            Assert.That(serverComponent.OwnerClientId, Is.EqualTo(m_ClientNetworkManagers[0].LocalClientId));
            Assert.That(clientComponent.OnGainedOwnershipFired);
            Assert.That(clientComponent.OwnerClientId, Is.EqualTo(m_ClientNetworkManagers[0].LocalClientId));

            serverComponent.ResetFlags();
            clientComponent.ResetFlags();

            serverObject.ChangeOwnership(NetworkManager.ServerClientId);
            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 2);

            Assert.That(serverComponent.OnGainedOwnershipFired);
            Assert.That(serverComponent.OwnerClientId, Is.EqualTo(m_ServerNetworkManager.LocalClientId));
            Assert.That(clientComponent.OnLostOwnershipFired);
            Assert.That(clientComponent.OwnerClientId, Is.EqualTo(m_ServerNetworkManager.LocalClientId));
        }
    }
}