using System.Collections;
using System.Collections.Generic;
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
        protected override int NumberOfClients => 9;

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

        [Test]
        public void TestPlayerIsOwned()
        {
            var clientOwnedObjects = m_ClientNetworkManagers[0].SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId);

            var clientPlayerObject = clientOwnedObjects.Where((c) => c.IsLocalPlayer).FirstOrDefault();
            Assert.NotNull(clientPlayerObject, $"Client Id {m_ClientNetworkManagers[0].LocalClientId} does not have its local player marked as an owned object!");

            clientPlayerObject = m_ClientNetworkManagers[0].LocalClient.OwnedObjects.Where((c) => c.IsLocalPlayer).FirstOrDefault();
            Assert.NotNull(clientPlayerObject, $"Client Id {m_ClientNetworkManagers[0].LocalClientId} does not have its local player marked as an owned object using local client!");
        }

        [UnityTest]
        public IEnumerator TestOwnershipCallbacks()
        {
            m_OwnershipObject = SpawnObject(m_OwnershipPrefab, m_ServerNetworkManager);
            m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<CreateObjectMessage>(m_ClientNetworkManagers[0]);

            var ownershipNetworkObjectId = m_OwnershipNetworkObject.NetworkObjectId;
            Assert.That(ownershipNetworkObjectId, Is.GreaterThan(0));
            Assert.That(m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(ownershipNetworkObjectId));
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                Assert.That(clientNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(ownershipNetworkObjectId));
            }

            // Verifies that removing the ownership when the default (server) is already set does not cause a Key Not Found Exception
            m_ServerNetworkManager.SpawnManager.RemoveOwnership(m_OwnershipNetworkObject);

            var serverObject = m_ServerNetworkManager.SpawnManager.SpawnedObjects[ownershipNetworkObjectId];
            var clientObject = m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects[ownershipNetworkObjectId];
            Assert.That(serverObject, Is.Not.Null);
            Assert.That(clientObject, Is.Not.Null);

            var serverComponent = serverObject.GetComponent<NetworkObjectOwnershipComponent>();
            var clientComponent = clientObject.GetComponent<NetworkObjectOwnershipComponent>();
            Assert.That(serverComponent, Is.Not.Null);
            Assert.That(clientComponent, Is.Not.Null);

            Assert.That(serverObject.OwnerClientId, Is.EqualTo(NetworkManager.ServerClientId));
            Assert.That(clientObject.OwnerClientId, Is.EqualTo(NetworkManager.ServerClientId));

            Assert.That(m_ServerNetworkManager.ConnectedClients.ContainsKey(m_ClientNetworkManagers[0].LocalClientId));

            serverObject.ChangeOwnership(clientComponent.NetworkManager.LocalClientId);
            yield return s_DefaultWaitForTick;

            Assert.That(serverComponent.OnLostOwnershipFired);
            Assert.That(serverComponent.OwnerClientId, Is.EqualTo(m_ClientNetworkManagers[0].LocalClientId));

            yield return WaitForConditionOrTimeOut(() => clientComponent.OnGainedOwnershipFired);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client to gain ownership!");
            Assert.That(clientComponent.OnGainedOwnershipFired);
            Assert.That(clientComponent.OwnerClientId, Is.EqualTo(m_ClientNetworkManagers[0].LocalClientId));

            serverComponent.ResetFlags();
            clientComponent.ResetFlags();

            serverObject.ChangeOwnership(NetworkManager.ServerClientId);
            yield return s_DefaultWaitForTick;

            Assert.That(serverComponent.OnGainedOwnershipFired);
            Assert.That(serverComponent.OwnerClientId, Is.EqualTo(m_ServerNetworkManager.LocalClientId));

            yield return WaitForConditionOrTimeOut(() => clientComponent.OnLostOwnershipFired);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client to lose ownership!");
            Assert.That(clientComponent.OnLostOwnershipFired);
            Assert.That(clientComponent.OwnerClientId, Is.EqualTo(m_ServerNetworkManager.LocalClientId));
        }

        /// <summary>
        /// Verifies that switching ownership between several clients works properly
        /// </summary>
        [UnityTest]
        public IEnumerator TestOwnershipCallbacksSeveralClients()
        {
            // Build our message hook entries tables so we can determine if all clients received spawn or ownership messages
            var messageHookEntriesForSpawn = new List<MessageHookEntry>();
            var messageHookEntriesForOwnership = new List<MessageHookEntry>();
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                var messageHook = new MessageHookEntry(clientNetworkManager);
                messageHook.AssignMessageType<CreateObjectMessage>();
                messageHookEntriesForSpawn.Add(messageHook);
                messageHook = new MessageHookEntry(clientNetworkManager);
                messageHook.AssignMessageType<ChangeOwnershipMessage>();
                messageHookEntriesForOwnership.Add(messageHook);
            }
            // Used to determine if all clients received the CreateObjectMessage
            var spawnMessageHooks = new MessageHooksConditional(messageHookEntriesForSpawn);

            // Used to determine if all clients received the ChangeOwnershipMessage
            var ownershipMessageHooks = new MessageHooksConditional(messageHookEntriesForOwnership);

            // Spawn our test object from server with server ownership
            m_OwnershipObject = SpawnObject(m_OwnershipPrefab, m_ServerNetworkManager);
            m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();

            // Wait for all clients to receive the CreateObjectMessage
            yield return WaitForConditionOrTimeOut(spawnMessageHooks);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive the {nameof(CreateObjectMessage)} message.");

            // Validate the NetworkObjectId and that the server and all clients have this NetworkObject
            var ownershipNetworkObjectId = m_OwnershipNetworkObject.NetworkObjectId;
            Assert.That(ownershipNetworkObjectId, Is.GreaterThan(0));
            Assert.That(m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(ownershipNetworkObjectId));

            bool WaitForClientsToSpawnNetworkObject()
            {
                foreach (var clientNetworkManager in m_ClientNetworkManagers)
                {
                    if (!clientNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(ownershipNetworkObjectId))
                    {
                        return false;
                    }
                }
                return true;
            }

            yield return WaitForConditionOrTimeOut(WaitForClientsToSpawnNetworkObject);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for all clients to change ownership!");

            // Verifies that removing the ownership when the default (server) is already set does not cause a Key Not Found Exception
            m_ServerNetworkManager.SpawnManager.RemoveOwnership(m_OwnershipNetworkObject);
            var serverObject = m_ServerNetworkManager.SpawnManager.SpawnedObjects[ownershipNetworkObjectId];
            Assert.That(serverObject, Is.Not.Null);

            var clientObjects = new List<NetworkObject>();
            for (int i = 0; i < NumberOfClients; i++)
            {
                var clientObject = m_ClientNetworkManagers[i].SpawnManager.SpawnedObjects[ownershipNetworkObjectId];
                Assert.That(clientObject, Is.Not.Null);
                clientObjects.Add(clientObject);
            }

            // Verify the server side component
            var serverComponent = serverObject.GetComponent<NetworkObjectOwnershipComponent>();
            Assert.That(serverComponent, Is.Not.Null);
            Assert.That(serverObject.OwnerClientId, Is.EqualTo(NetworkManager.ServerClientId));

            // Verify the clients components
            for (int i = 0; i < NumberOfClients; i++)
            {
                var clientComponent = clientObjects[i].GetComponent<NetworkObjectOwnershipComponent>();
                Assert.That(clientComponent.OwnerClientId, Is.EqualTo(NetworkManager.ServerClientId));
                clientComponent.ResetFlags();
            }

            // After the 1st client has been given ownership to the object, this will be used to make sure each previous owner properly received the remove ownership message
            var previousClientComponent = (NetworkObjectOwnershipComponent)null;
            for (int clientIndex = 0; clientIndex < NumberOfClients; clientIndex++)
            {
                var clientObject = clientObjects[clientIndex];
                var clientId = m_ClientNetworkManagers[clientIndex].LocalClientId;

                Assert.That(m_ServerNetworkManager.ConnectedClients.ContainsKey(clientId));
                serverObject.ChangeOwnership(clientId);
                yield return s_DefaultWaitForTick;
                Assert.That(serverComponent.OnLostOwnershipFired);
                Assert.That(serverComponent.OwnerClientId, Is.EqualTo(clientId));
                // Wait for all clients to receive the CreateObjectMessage
                yield return WaitForConditionOrTimeOut(ownershipMessageHooks);
                Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive the {nameof(ChangeOwnershipMessage)} message.");

                var previousNetworkManager = m_ServerNetworkManager;
                if (previousClientComponent != null)
                {
                    // Once we have a previousClientComponent, we want to verify the server is keeping track for the removal of ownership in the OwnershipToObjectsTable
                    Assert.That(!m_ServerNetworkManager.SpawnManager.OwnershipToObjectsTable[m_ServerNetworkManager.LocalClientId].ContainsKey(serverObject.NetworkObjectId));
                    previousNetworkManager = previousClientComponent.NetworkManager;
                    Assert.That(previousClientComponent.OnLostOwnershipFired);
                    Assert.That(previousClientComponent.OwnerClientId, Is.EqualTo(clientId));
                }

                // Assure the previous owner is no longer in the local table of the previous owner.
                Assert.That(!previousNetworkManager.SpawnManager.OwnershipToObjectsTable[previousNetworkManager.LocalClientId].ContainsKey(serverObject.NetworkObjectId));

                var currentClientComponent = clientObjects[clientIndex].GetComponent<NetworkObjectOwnershipComponent>();
                Assert.That(currentClientComponent.OnGainedOwnershipFired);

                // Possibly the more important part of this test:
                // Check to make sure all other non-former or current ownership clients are synchronized to each ownership change
                for (int i = 0; i < NumberOfClients; i++)
                {
                    var clientComponent = clientObjects[i].GetComponent<NetworkObjectOwnershipComponent>();
                    Assert.That(clientComponent, Is.Not.Null);
                    Assert.That(clientComponent.OwnerClientId, Is.EqualTo(clientId));
                    clientComponent.ResetFlags();
                }
                // We must reset this for each iteration in order to make sure all clients receive the ChangeOwnershipMessage
                ownershipMessageHooks.Reset();

                // Set the current owner client to the previous one
                previousClientComponent = currentClientComponent;
            }

            // Now change ownership back to the server
            serverObject.ChangeOwnership(NetworkManager.ServerClientId);
            yield return WaitForConditionOrTimeOut(ownershipMessageHooks);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive the {nameof(ChangeOwnershipMessage)} message (back to server).");

            Assert.That(serverComponent.OnGainedOwnershipFired);
            Assert.That(serverComponent.OwnerClientId, Is.EqualTo(m_ServerNetworkManager.LocalClientId));

            yield return WaitForConditionOrTimeOut(() => previousClientComponent.OnLostOwnershipFired);

            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {previousClientComponent.name} to lose ownership!");

            // Make sure all client-side versions of the object is once again owned by the server
            for (int i = 0; i < NumberOfClients; i++)
            {
                var clientComponent = clientObjects[i].GetComponent<NetworkObjectOwnershipComponent>();
                Assert.That(clientComponent, Is.Not.Null);
                Assert.That(clientComponent.OwnerClientId, Is.EqualTo(m_ServerNetworkManager.LocalClientId));
                clientComponent.ResetFlags();
            }
            serverComponent.ResetFlags();
        }
    }
}
