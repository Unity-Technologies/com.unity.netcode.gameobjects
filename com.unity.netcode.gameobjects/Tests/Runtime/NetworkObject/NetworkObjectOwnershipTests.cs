using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkObjectOwnershipComponent : NetworkBehaviour
    {
        public static Dictionary<ulong, NetworkObjectOwnershipComponent> SpawnedInstances = new Dictionary<ulong, NetworkObjectOwnershipComponent>();

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

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            Assert.True(previous != current, $"[{nameof(OnOwnershipChanged)}][Invalid Parameters] Invoked and the previous ({previous}) equals the current ({current})!");
            base.OnOwnershipChanged(previous, current);
        }

        public override void OnNetworkSpawn()
        {
            if (!SpawnedInstances.ContainsKey(NetworkManager.LocalClientId))
            {
                SpawnedInstances.Add(NetworkManager.LocalClientId, this);
            }
            base.OnNetworkSpawn();
        }

        public void ResetFlags()
        {
            OnLostOwnershipFired = false;
            OnGainedOwnershipFired = false;
        }

        [Rpc(SendTo.Authority)]
        public void ChangeOwnershipRpc(RpcParams rpcParams = default)
        {
            NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
        }
    }


    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class NetworkObjectOwnershipTests : NetcodeIntegrationTest
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

        protected override IEnumerator OnSetup()
        {
            NetworkObjectOwnershipComponent.SpawnedInstances.Clear();
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_OwnershipPrefab = CreateNetworkObjectPrefab("OnwershipPrefab");
            m_OwnershipPrefab.AddComponent<NetworkObjectOwnershipComponent>();
            m_OwnershipPrefab.AddComponent<NetworkTransform>();
            if (m_DistributedAuthority)
            {
                m_OwnershipPrefab.GetComponent<NetworkObject>().SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            }
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator TestPlayerIsOwned()
        {
            var clientOwnedObjects = m_ClientNetworkManagers[0].SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId);

            var clientPlayerObject = clientOwnedObjects.Where((c) => c.IsLocalPlayer).FirstOrDefault();
            Assert.NotNull(clientPlayerObject, $"Client Id {m_ClientNetworkManagers[0].LocalClientId} does not have its local player marked as an owned object!");

            clientPlayerObject = m_ClientNetworkManagers[0].LocalClient.OwnedObjects.Where((c) => c.IsLocalPlayer).FirstOrDefault();
            Assert.NotNull(clientPlayerObject, $"Client Id {m_ClientNetworkManagers[0].LocalClientId} does not have its local player marked as an owned object using local client!");
            yield return null;
        }

        private bool AllObjectsSpawnedOnClients()
        {
            foreach (var client in m_NetworkManagers)
            {
                if (!NetworkObjectOwnershipComponent.SpawnedInstances.ContainsKey(client.LocalClientId))
                {
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator TestOwnershipCallbacks([Values] OwnershipChecks ownershipChecks)
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();

            m_OwnershipObject = SpawnObject(m_OwnershipPrefab, authority);
            m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<CreateObjectMessage>(nonAuthority);

            yield return WaitForConditionOrTimeOut(AllObjectsSpawnedOnClients);
            AssertOnTimeout($"Timed out waiting for all clients to spawn the ownership object!");

            var ownershipNetworkObjectId = m_OwnershipNetworkObject.NetworkObjectId;
            Assert.That(ownershipNetworkObjectId, Is.GreaterThan(0));
            Assert.That(authority.SpawnManager.SpawnedObjects.ContainsKey(ownershipNetworkObjectId));
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                Assert.That(clientNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(ownershipNetworkObjectId));
            }

            // Verifies that removing the ownership when the default (server) is already set does not cause a Key Not Found Exception
            // Distributed authority does not allow remove ownership (users are instructed to use change ownership)
            if (!m_DistributedAuthority)
            {
                m_ServerNetworkManager.SpawnManager.RemoveOwnership(m_OwnershipNetworkObject);
            }

            var authorityObject = authority.SpawnManager.SpawnedObjects[ownershipNetworkObjectId];
            var clientObject = nonAuthority.SpawnManager.SpawnedObjects[ownershipNetworkObjectId];

            Assert.That(authorityObject, Is.Not.Null);
            Assert.That(clientObject, Is.Not.Null);

            var authorityComponent = authorityObject.GetComponent<NetworkObjectOwnershipComponent>();
            var clientComponent = clientObject.GetComponent<NetworkObjectOwnershipComponent>();
            Assert.That(authorityComponent, Is.Not.Null);
            Assert.That(clientComponent, Is.Not.Null);

            var expectedOwnerId = m_UseCmbService ? authority.LocalClientId : NetworkManager.ServerClientId;

            Assert.That(authorityObject.OwnerClientId, Is.EqualTo(expectedOwnerId));
            Assert.That(clientObject.OwnerClientId, Is.EqualTo(expectedOwnerId));

            Assert.That(authority.ConnectedClients.ContainsKey(nonAuthority.LocalClientId));

            authorityObject.ChangeOwnership(clientComponent.NetworkManager.LocalClientId);
            yield return s_DefaultWaitForTick;

            Assert.That(authorityComponent.OnLostOwnershipFired);
            Assert.That(authorityComponent.OwnerClientId, Is.EqualTo(nonAuthority.LocalClientId));

            yield return WaitForConditionOrTimeOut(() => clientComponent.OnGainedOwnershipFired);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client to gain ownership!");
            Assert.That(clientComponent.OnGainedOwnershipFired);
            Assert.That(clientComponent.OwnerClientId, Is.EqualTo(nonAuthority.LocalClientId));

            authorityComponent.ResetFlags();
            clientComponent.ResetFlags();

            if (ownershipChecks == OwnershipChecks.Change)
            {
                // Validates that when ownership is changed back to the server it will get an OnGainedOwnership notification
                authorityObject.ChangeOwnership(expectedOwnerId);
            }
            else
            {
                // Validates that when ownership is removed the server gets an OnGainedOwnership notification
                // In distributed authority mode, the current owner just rolls the ownership back over to the DAHost client (i.e. host mocking CMB Service)
                if (m_DistributedAuthority)
                {
                    clientObject.ChangeOwnership(expectedOwnerId);
                }
                else
                {
                    authorityObject.RemoveOwnership();
                }
            }

            yield return WaitForConditionOrTimeOut(() => authorityComponent.OnGainedOwnershipFired && authorityComponent.OwnerClientId == authority.LocalClientId);
            AssertOnTimeout($"Timed out waiting for ownership to be transferred back to the host instance!");

            yield return WaitForConditionOrTimeOut(() => clientComponent.OnLostOwnershipFired && clientComponent.OwnerClientId == authority.LocalClientId);
            AssertOnTimeout($"Timed out waiting for client-side lose ownership event to trigger or owner identifier to be equal to the host!");
        }

        /// <summary>
        /// Verifies that switching ownership between several clients works properly
        /// </summary>
        [UnityTest]
        public IEnumerator TestOwnershipCallbacksSeveralClients([Values] OwnershipChecks ownershipChecks)
        {
            var authority = GetAuthorityNetworkManager();

            // Build our message hook entries tables so we can determine if all clients received spawn or ownership messages
            var messageHookEntriesForSpawn = new List<MessageHookEntry>();
            var messageHookEntriesForOwnership = new List<MessageHookEntry>();
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                if (clientNetworkManager == authority)
                {
                    continue;
                }
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
            m_OwnershipObject = SpawnObject(m_OwnershipPrefab, authority);
            m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();

            // Wait for all clients to receive the CreateObjectMessage
            yield return WaitForConditionOrTimeOut(spawnMessageHooks);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive the {nameof(CreateObjectMessage)} message.");

            // Validate the NetworkObjectId and that the server and all clients have this NetworkObject
            var ownershipNetworkObjectId = m_OwnershipNetworkObject.NetworkObjectId;
            Assert.That(ownershipNetworkObjectId, Is.GreaterThan(0));
            Assert.That(authority.SpawnManager.SpawnedObjects.ContainsKey(ownershipNetworkObjectId));

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
            // Distributed authority does not allow remove ownership (users are instructed to use change ownership)
            if (!m_DistributedAuthority)
            {
                m_ServerNetworkManager.SpawnManager.RemoveOwnership(m_OwnershipNetworkObject);
            }

            var serverObject = authority.SpawnManager.SpawnedObjects[ownershipNetworkObjectId];
            Assert.That(serverObject, Is.Not.Null);
            var clientObject = (NetworkObject)null;
            var clientObjects = new List<NetworkObject>();
            for (int i = 0; i < NumberOfClients; i++)
            {
                clientObject = m_ClientNetworkManagers[i].SpawnManager.SpawnedObjects[ownershipNetworkObjectId];
                Assert.That(clientObject, Is.Not.Null);
                clientObjects.Add(clientObject);
            }

            // Verify the server side component
            var authorityId = m_UseCmbService ? authority.LocalClientId : NetworkManager.ServerClientId;
            var serverComponent = serverObject.GetComponent<NetworkObjectOwnershipComponent>();
            Assert.That(serverComponent, Is.Not.Null);
            Assert.That(serverObject.OwnerClientId, Is.EqualTo(authorityId));

            // Verify the clients components
            for (int i = 0; i < NumberOfClients; i++)
            {
                var clientComponent = clientObjects[i].GetComponent<NetworkObjectOwnershipComponent>();
                Assert.That(clientComponent.OwnerClientId, Is.EqualTo(authorityId));
                clientComponent.ResetFlags();
            }

            // After the 1st client has been given ownership to the object, this will be used to make sure each previous owner properly received the remove ownership message
            var previousClientComponent = (NetworkObjectOwnershipComponent)null;

            var networkManagersDAMode = new List<NetworkManager>();

            for (int clientIndex = 0; clientIndex < NumberOfClients; clientIndex++)
            {
                clientObject = clientObjects[clientIndex];
                var clientId = m_ClientNetworkManagers[clientIndex].LocalClientId;

                if (clientId == authority.LocalClientId)
                {
                    continue;
                }

                Assert.That(authority.ConnectedClients.ContainsKey(clientId));
                serverObject.ChangeOwnership(clientId);
                yield return WaitForConditionOrTimeOut(() => serverObject.OwnerClientId == clientId);

                Assert.That(serverComponent.OnLostOwnershipFired);
                Assert.That(serverComponent.OwnerClientId, Is.EqualTo(clientId));
                // Wait for all clients to receive the CreateObjectMessage
                yield return WaitForConditionOrTimeOut(ownershipMessageHooks);
                Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive the {nameof(ChangeOwnershipMessage)} message.");

                var previousNetworkManager = authority;
                if (previousClientComponent != null)
                {
                    // Once we have a previousClientComponent, we want to verify the server is keeping track for the removal of ownership in the OwnershipToObjectsTable
                    Assert.That(!authority.SpawnManager.OwnershipToObjectsTable[authority.LocalClientId].ContainsKey(serverObject.NetworkObjectId));
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
                serverObject.ChangeOwnership(authorityId);
            }
            else
            {
                // Validates that when ownership is removed the server gets an OnGainedOwnership notification
                // In distributed authority mode, the current owner just rolls the ownership back over to the DAHost client (i.e. host mocking CMB Service)
                if (m_DistributedAuthority)
                {
                    // In distributed authority, we have to clear out the NetworkManager instances as this changes relative to authority.
                    networkManagersDAMode.Clear();
                    foreach (var clientNetworkManager in m_NetworkManagers)
                    {
                        if (clientNetworkManager.LocalClientId == clientObject.OwnerClientId)
                        {
                            continue;
                        }
                        networkManagersDAMode.Add(clientNetworkManager);
                    }
                    clientObject.ChangeOwnership(authorityId);
                }
                else
                {
                    serverObject.RemoveOwnership();
                }
            }

            if (m_DistributedAuthority)
            {
                // We use an alternate method (other than message hooks) to verify each client received the ownership message since message hooks becomes problematic when you need
                // to make dynamic changes to your targets.
                yield return WaitForConditionOrTimeOut(() => OwnershipChangedOnAllTargetedClients(networkManagersDAMode, clientObject.NetworkObjectId, authorityId));
            }
            else
            {
                yield return WaitForConditionOrTimeOut(ownershipMessageHooks);
            }


            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive the {nameof(ChangeOwnershipMessage)} message (back to server).");

            Assert.That(serverComponent.OnGainedOwnershipFired);
            Assert.That(serverComponent.OwnerClientId, Is.EqualTo(authorityId));

            yield return WaitForConditionOrTimeOut(() => previousClientComponent.OnLostOwnershipFired);

            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {previousClientComponent.name} to lose ownership!");

            // Make sure all client-side versions of the object is once again owned by the server
            for (int i = 0; i < NumberOfClients; i++)
            {
                var clientComponent = clientObjects[i].GetComponent<NetworkObjectOwnershipComponent>();
                Assert.That(clientComponent, Is.Not.Null);
                Assert.That(clientComponent.OwnerClientId, Is.EqualTo(authorityId));
                clientComponent.ResetFlags();
            }
            serverComponent.ResetFlags();
        }

        private bool OwnershipChangedOnAllTargetedClients(List<NetworkManager> networkManagers, ulong networkObjectId, ulong expectedOwner)
        {
            foreach (var networkManager in networkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    return false;
                }
                if (networkManager.SpawnManager.SpawnedObjects[networkObjectId].OwnerClientId != expectedOwner)
                {
                    return false;
                }
            }
            return true;
        }

        private const int k_NumberOfSpawnedObjects = 5;

        private bool AllClientsHaveCorrectObjectCount()
        {
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                if (clientNetworkManager.LocalClient.OwnedObjects.Length < k_NumberOfSpawnedObjects)
                {
                    return false;
                }
            }

            return true;
        }

        private StringBuilder m_ErrorLog = new StringBuilder();

        private bool ServerHasCorrectClientOwnedObjectCount()
        {
            m_ErrorLog.Clear();
            var authority = GetAuthorityNetworkManager();
            // Only check when we are the host or session owner
            if (authority.IsHost || (!authority.IsServer && authority.LocalClient.IsSessionOwner))
            {
                if (authority.LocalClient.OwnedObjects.Length < k_NumberOfSpawnedObjects)
                {
                    m_ErrorLog.AppendLine($"[{authority.name}] Has only {authority.LocalClient.OwnedObjects.Length} spawned objects and expected is {k_NumberOfSpawnedObjects}");
                }
            }

            foreach (var connectedClient in authority.ConnectedClients)
            {
                if (connectedClient.Value.OwnedObjects.Length < k_NumberOfSpawnedObjects)
                {
                    m_ErrorLog.AppendLine($"[Client-{connectedClient.Key}] Has only {connectedClient.Value.OwnedObjects.Length} spawned objects and expected is {k_NumberOfSpawnedObjects}");
                }
            }
            return m_ErrorLog.Length == 0;
        }

        [UnityTest]
        public IEnumerator TestOwnedObjectCounts()
        {
            foreach (var manager in m_NetworkManagers)
            {
                for (int i = 0; i < k_NumberOfSpawnedObjects; i++)
                {
                    SpawnObject(m_OwnershipPrefab, manager);
                }
            }

            yield return WaitForConditionOrTimeOut(AllClientsHaveCorrectObjectCount);
            AssertOnTimeout($"Not all clients spawned {k_NumberOfSpawnedObjects} {nameof(NetworkObject)}s!");

            yield return WaitForConditionOrTimeOut(ServerHasCorrectClientOwnedObjectCount);
            AssertOnTimeout($"Server does not have the correct count for all clients spawned {k_NumberOfSpawnedObjects} {nameof(NetworkObject)}s!\n {m_ErrorLog}");
        }

        /// <summary>
        /// Validates that when changing ownership NetworkTransform does not enter into a bad state
        /// because the previous and current owner identifiers are the same. For client-server this
        /// ends up always being the server, but for distributed authority the authority changes when
        /// ownership changes.
        /// </summary>
        [UnityTest]
        public IEnumerator TestAuthorityChangingOwnership()
        {
            var authorityManager = (NetworkManager)null;

            if (m_DistributedAuthority)
            {
                var authorityId = Random.Range(1, TotalClients) - 1;
                authorityManager = m_ClientNetworkManagers[authorityId];
                m_OwnershipObject = SpawnObject(m_OwnershipPrefab, authorityManager);
                m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();
            }
            else
            {
                authorityManager = m_ServerNetworkManager;
                m_OwnershipObject = SpawnObject(m_OwnershipPrefab, m_ServerNetworkManager);
                m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();
            }
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
            foreach (var networkManager in m_NetworkManagers)
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
