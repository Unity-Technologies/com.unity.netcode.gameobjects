using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectOwnershipComponent : NetworkBehaviour
    {
        public bool OnLostOwnershipFired = false;
        public bool OnGainedOwnershipFired = false;


        /// <inheritdoc/>
        public override void OnLostOwnership()
        {
            OnLostOwnershipFired = true;
        }


        /// <inheritdoc/>
        public override void OnGainedOwnership()
        {
            OnGainedOwnershipFired = true;
        }


        /// <inheritdoc/>
        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            Assert.True(previous != current, $"[{nameof(OnOwnershipChanged)}][Invalid Parameters] Invoked and the previous ({previous}) equals the current ({current})!");
            base.OnOwnershipChanged(previous, current);
        }

        public void ResetFlags()
        {
            OnLostOwnershipFired = false;
            OnGainedOwnershipFired = false;
        }

        [Rpc(SendTo.Server)]
        internal void ChangeOwnershipRpc(RpcParams rpcParams = default)
        {
            NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
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

        public enum OwnershipChecks
        {
            Change,
            Remove
        }

        protected override void OnServerAndClientsCreated()
        {
            m_OwnershipPrefab = CreateNetworkObjectPrefab("OnwershipPrefab");
            m_OwnershipPrefab.AddComponent<NetworkObjectOwnershipComponent>();
            m_OwnershipPrefab.AddComponent<NetworkTransform>();
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
        public IEnumerator TestOwnershipCallbacks([Values] OwnershipChecks ownershipChecks)
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

            if (ownershipChecks == OwnershipChecks.Change)
            {
                // Validates that when ownership is changed back to the server it will get an OnGainedOwnership notification
                serverObject.ChangeOwnership(NetworkManager.ServerClientId);
            }
            else
            {
                // Validates that when ownership is removed the server gets an OnGainedOwnership notification
                serverObject.RemoveOwnership();
            }

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
        public IEnumerator TestOwnershipCallbacksSeveralClients([Values] OwnershipChecks ownershipChecks)
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

            if (ownershipChecks == OwnershipChecks.Change)
            {
                // Validates that when ownership is changed back to the server it will get an OnGainedOwnership notification
                serverObject.ChangeOwnership(NetworkManager.ServerClientId);
            }
            else
            {
                // Validates that when ownership is removed the server gets an OnGainedOwnership notification
                serverObject.RemoveOwnership();
            }

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

        private const int k_NumberOfSpawnedObjects = 5;

        private bool AllClientsHaveCorrectObjectCount()
        {

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                if (clientNetworkManager.LocalClient.OwnedObjects.Count < k_NumberOfSpawnedObjects)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ServerHasCorrectClientOwnedObjectCount()
        {
            // Only check when we are the host
            if (m_ServerNetworkManager.IsHost)
            {
                if (m_ServerNetworkManager.LocalClient.OwnedObjects.Count < k_NumberOfSpawnedObjects)
                {
                    return false;
                }
            }

            foreach (var connectedClient in m_ServerNetworkManager.ConnectedClients)
            {
                if (connectedClient.Value.OwnedObjects.Count < k_NumberOfSpawnedObjects)
                {
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator TestOwnedObjectCounts()
        {
            if (m_ServerNetworkManager.IsHost)
            {
                for (int i = 0; i < 5; i++)
                {
                    SpawnObject(m_OwnershipPrefab, m_ServerNetworkManager);
                }
            }

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                for (int i = 0; i < 5; i++)
                {
                    SpawnObject(m_OwnershipPrefab, clientNetworkManager);
                }
            }

            yield return WaitForConditionOrTimeOut(AllClientsHaveCorrectObjectCount);
            AssertOnTimeout($"Not all clients spawned {k_NumberOfSpawnedObjects} {nameof(NetworkObject)}s!");

            yield return WaitForConditionOrTimeOut(ServerHasCorrectClientOwnedObjectCount);
            AssertOnTimeout($"Server does not have the correct count for all clients spawned {k_NumberOfSpawnedObjects} {nameof(NetworkObject)}s!");

        }

        /// <summary>
        /// Validates that when changing ownership NetworkTransform does not enter into a bad state
        /// because the previous and current owner identifiers are the same. For client-server this
        /// ends up always being the server, but for distributed authority the authority changes when
        /// ownership changes.
        /// </summary>
        /// <returns><see cref="IEnumerator"/></returns>
        [UnityTest]
        public IEnumerator TestAuthorityChangingOwnership()
        {
            var authorityManager = m_ServerNetworkManager; ;
            var allNetworkManagers = m_ClientNetworkManagers.ToList();
            allNetworkManagers.Add(m_ServerNetworkManager);

            m_OwnershipObject = SpawnObject(m_OwnershipPrefab, m_ServerNetworkManager);
            m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();
            var ownershipNetworkObjectId = m_OwnershipNetworkObject.NetworkObjectId;
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
            AssertOnTimeout($"Timed out waiting for all clients to spawn the {m_OwnershipNetworkObject.name} {nameof(NetworkObject)} instance!");

            var currentTargetOwner = (ulong)0;
            bool WaitForAllInstancesToChangeOwnership()
            {
                foreach (var clientNetworkManager in m_ClientNetworkManagers)
                {
                    if (!clientNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(ownershipNetworkObjectId))
                    {
                        return false;
                    }
                    if (clientNetworkManager.SpawnManager.SpawnedObjects[ownershipNetworkObjectId].OwnerClientId != currentTargetOwner)
                    {
                        return false;
                    }
                }
                return true;
            }

            // Change ownership a few times and as long as the previous and current owners are not the same when
            // OnOwnershipChanged is invoked then the test passed.
            foreach (var networkManager in allNetworkManagers)
            {
                if (networkManager == authorityManager)
                {
                    continue;
                }
                var clonedObject = networkManager.SpawnManager.SpawnedObjects[ownershipNetworkObjectId];

                if (clonedObject.OwnerClientId == networkManager.LocalClientId)
                {
                    continue;
                }

                var testComponent = clonedObject.GetComponent<NetworkObjectOwnershipComponent>();
                testComponent.ChangeOwnershipRpc();
                yield return WaitForAllInstancesToChangeOwnership();
                AssertOnTimeout($"Timed out waiting for all instances to change ownership to Client-{networkManager.LocalClientId}!");
            }
        }
    }
}
