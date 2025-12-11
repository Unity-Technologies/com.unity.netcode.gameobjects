using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal enum SceneManagementSetting
    {
        EnableSceneManagement,
        DisableSceneManagement
    }

    [TestFixture(SceneManagementSetting.EnableSceneManagement)]
    [TestFixture(SceneManagementSetting.DisableSceneManagement)]
    internal class ExtendedNetworkShowAndHideTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 3;
        private readonly bool m_EnableSceneManagement;
        private GameObject m_ObjectToSpawn;
        private List<NetworkObject> m_SpawnedObjects = new();
        private Dictionary<NetworkManager, NetworkObject> m_ObjectHiddenFromClient = new();

        public ExtendedNetworkShowAndHideTests(SceneManagementSetting sceneManagement) : base(HostOrServer.DAHost)
        {
            m_EnableSceneManagement = sceneManagement == SceneManagementSetting.EnableSceneManagement;
        }

        protected override void OnServerAndClientsCreated()
        {
            foreach (var client in m_NetworkManagers)
            {
                client.NetworkConfig.EnableSceneManagement = m_EnableSceneManagement;
            }

            m_ObjectToSpawn = CreateNetworkObjectPrefab("TestObject");
            m_ObjectToSpawn.AddComponent<EmptyBehaviour>();
            m_ObjectToSpawn.SetActive(false);

            base.OnServerAndClientsCreated();
        }

        private bool AllObjectsHiddenFromTheirClient(StringBuilder errorLog)
        {
            var allHidden = true;
            foreach (var (clientToHideFrom, objectToHide) in m_ObjectHiddenFromClient)
            {
                if (clientToHideFrom.SpawnManager.SpawnedObjects.ContainsKey(objectToHide.NetworkObjectId))
                {
                    errorLog.AppendLine($"Object-{objectToHide.NetworkObjectId} is still visible to Client-{clientToHideFrom.LocalClientId}");
                    allHidden = false;
                }
            }

            return allHidden;
        }

        private bool IsClientPromotedToSessionOwner(StringBuilder errorLog)
        {
            var valid = true;
            var currentSessionOwner = GetAuthorityNetworkManager();
            foreach (var client in m_NetworkManagers)
            {
                if (currentSessionOwner == client)
                {
                    if (!client.LocalClient.IsSessionOwner)
                    {
                        errorLog.AppendLine($"Session owner is Client-{client.LocalClientId} but they are not marked as session owner.");
                        valid = false;
                    }
                }
                else if (client.LocalClient.IsSessionOwner)
                {
                    errorLog.AppendLine($"Client-{client.LocalClientId} is incorrectly marked as Session Owner.");
                    valid = false;
                }

                if (client.CurrentSessionOwner != currentSessionOwner.LocalClientId)
                {
                    errorLog.AppendLine($"Client-{client.LocalClientId} has the incorrect session owner id (Has: {client.CurrentSessionOwner}, expected: {currentSessionOwner.LocalClientId}).");
                    valid = false;
                }
            }
            return valid;
        }

        private bool AllObjectsSpawnedExceptForHidden(StringBuilder errorLog)
        {
            var valid = true;
            foreach (var client in m_NetworkManagers)
            {
                foreach (var spawnedObject in m_SpawnedObjects)
                {
                    var isVisible = client.SpawnManager.SpawnedObjects.ContainsKey(spawnedObject.NetworkObjectId);

                    if (m_ObjectHiddenFromClient.TryGetValue(client, out var hiddenObject) && hiddenObject == spawnedObject)
                    {
                        if (isVisible)
                        {
                            errorLog.AppendLine($"Client-{client.LocalClientId} can see object-{spawnedObject.NetworkObjectId} that should be hidden.");
                            valid = false;
                            continue;
                        }
                    }
                    else if (!isVisible)
                    {
                        errorLog.AppendLine($"Client-{client.LocalClientId} hasn't spawned object-{spawnedObject.NetworkObjectId}");
                        valid = false;
                    }

                    if (isVisible)
                    {
                        var clientObject = client.SpawnManager.SpawnedObjects[spawnedObject.NetworkObjectId];
                        var behaviour = clientObject.GetComponent<EmptyBehaviour>();
                        if (behaviour.IsSessionOwner != client.LocalClient.IsSessionOwner)
                        {
                            errorLog.AppendLine($"Client-{client.LocalClientId} network behaviour has incorrect session owner value. Should be {client.LocalClient.IsSessionOwner}, is {behaviour.IsSessionOwner}");
                            valid = false;
                        }
                    }
                }
            }
            return valid;
        }

        /// <summary>
        /// This test validates the following NetworkShow - NetworkHide issue:
        /// - During a session, a spawned object is hidden from a client.
        /// - The current session owner disconnects and the client the object is hidden from is prommoted to the session owner.
        /// - A new client joins and the newly promoted session owner synchronizes the newly joined client with only objects visible to it.
        /// - Any already connected non-session owner client should "NetworkShow" the object to the newly connected client
        /// (but only if the hidden object has SpawnWithObservers enabled)
        /// </summary>
        [UnityTest]
        public IEnumerator HiddenObjectPromotedSessionOwnerNewClientSynchronizes()
        {
            // Get the test relative session owner
            var sessionOwner = GetAuthorityNetworkManager();

            // Spawn objects for the non-session owner clients
            m_ObjectToSpawn.SetActive(true);
            foreach (var client in m_NetworkManagers)
            {
                if (client == sessionOwner)
                {
                    Assert.IsTrue(client.LocalClient.IsSessionOwner);
                    continue;
                }

                var instance = SpawnObject(m_ObjectToSpawn, client).GetComponent<NetworkObject>();
                m_SpawnedObjects.Add(instance);
            }

            yield return WaitForSpawnedOnAllOrTimeOut(m_SpawnedObjects);
            AssertOnTimeout("[InitialSpawn] Not all clients spawned all objects");

            // Hide one spawned object from each client
            var setOfInstances = m_SpawnedObjects.ToHashSet();
            foreach (var clientToHideFrom in m_NetworkManagers)
            {
                // Session owner doesn't need to have an object hidden from them
                if (clientToHideFrom == sessionOwner)
                {
                    continue;
                }

                // Find an object that this client doesn't own that isn't already hidden from another client
                var toHide = setOfInstances.Last(obj => obj.OwnerClientId != clientToHideFrom.LocalClientId);
                toHide.NetworkHide(clientToHideFrom.LocalClientId);
                setOfInstances.Remove(toHide);
                m_ObjectHiddenFromClient.Add(clientToHideFrom, toHide);

                Assert.IsFalse(toHide.GetComponent<EmptyBehaviour>().IsSessionOwner, "No object should have been spawned owned by the session owner");
            }

            Assert.That(m_ObjectHiddenFromClient.Count, Is.EqualTo(NumberOfClients), "Test should hide one object per non-authority client");
            Assert.That(setOfInstances, Is.Empty, "Not all objects have been hidden frm someone.");

            yield return WaitForConditionOrTimeOut(AllObjectsHiddenFromTheirClient);
            AssertOnTimeout("Not all objects have been hidden from someone!");

            // Promoted a new session owner (DAHost promotes while CMB Session we disconnect the current session owner)
            if (!m_UseCmbService)
            {
                var nonAuthority = GetNonAuthorityNetworkManager();
                m_ServerNetworkManager.PromoteSessionOwner(nonAuthority.LocalClientId);
                yield return s_DefaultWaitForTick;
            }
            else
            {
                yield return StopOneClient(sessionOwner);
            }

            // Wait for the new session owner to be promoted and for all clients to acknowledge the promotion
            yield return WaitForConditionOrTimeOut(IsClientPromotedToSessionOwner);
            AssertOnTimeout($"No client was promoted as session owner on all client instances!");

            var newSessionOwner = GetAuthorityNetworkManager();
            VerboseDebug($"Client-{newSessionOwner.LocalClientId} was promoted as session owner on all client instances!");
            Assert.That(newSessionOwner, Is.Not.EqualTo(sessionOwner), "The current session owner be different from the original session owner.");
            Assert.That(m_ObjectHiddenFromClient, Does.ContainKey(newSessionOwner), "An object should be hidden from the newly promoted session owner");

            // Connect a new client instance
            var newClient = CreateNewClient();
            newClient.NetworkConfig.EnableSceneManagement = m_EnableSceneManagement;
            yield return StartClient(newClient);

            // Assure the newly connected client is synchronized with the NetworkObject hidden from the newly promoted session owner
            yield return WaitForConditionOrTimeOut(AllObjectsSpawnedExceptForHidden);
            AssertOnTimeout("[LateJoinClient] Not all objects spawned correctly");
        }

        protected override IEnumerator OnTearDown()
        {
            m_SpawnedObjects.Clear();
            m_ObjectHiddenFromClient.Clear();
            return base.OnTearDown();
        }

        private class EmptyBehaviour : NetworkBehaviour
        {
        }
    }
}
