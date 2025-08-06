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
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class NetworkObjectDontDestroyWithOwnerTests : NetcodeIntegrationTest
    {
        private const int k_NumberObjectsToSpawn = 16;
        protected override int NumberOfClients => 3;

        public enum ParentedPass
        {
            NoParent,
            HasParent
        }

        protected GameObject m_DestroyWithOwnerPrefab;
        protected GameObject m_DontDestroyWithOwnerPrefab;
        protected GameObject m_PrefabNoObserversSpawn;

        private ulong m_NonAuthorityClientId;

        private List<ulong> m_DontDestroyObjectIds = new List<ulong>();
        private List<ulong> m_DestroyObjectIds = new List<ulong>();

        public NetworkObjectDontDestroyWithOwnerTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnServerAndClientsCreated()
        {
            m_DontDestroyWithOwnerPrefab = CreateNetworkObjectPrefab("DontDestroyWith");
            m_DontDestroyWithOwnerPrefab.GetComponent<NetworkObject>().DontDestroyWithOwner = true;

            m_DestroyWithOwnerPrefab = CreateNetworkObjectPrefab("DestroyWith");

            m_PrefabNoObserversSpawn = CreateNetworkObjectPrefab("NoObserversObject");
            var prefabNoObserversNetworkObject = m_PrefabNoObserversSpawn.GetComponent<NetworkObject>();
            prefabNoObserversNetworkObject.SpawnWithObservers = false;
            prefabNoObserversNetworkObject.DontDestroyWithOwner = true;
        }

        /// <summary>
        /// Validates all instances of both prefab types have spawned on all clients.
        /// </summary>
        private bool HaveAllObjectInstancesSpawned(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var relativeSpawnedObjects = networkManager.SpawnManager.SpawnedObjects;
                for (int i = 0; i < k_NumberObjectsToSpawn; i++)
                {
                    var dontDestroyObjectId = m_DontDestroyObjectIds[i];
                    var destroyObjectId = m_DestroyObjectIds[i];
                    if (!relativeSpawnedObjects.ContainsKey(dontDestroyObjectId))
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][DontDestroyWithOwner] Has not spawned {nameof(NetworkObject)}-{dontDestroyObjectId}!");
                    }
                    if (!relativeSpawnedObjects.ContainsKey(destroyObjectId))
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][DestroyWithOwner] Has not spawned {nameof(NetworkObject)}-{destroyObjectId}!");
                    }
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// Helper method to spawn the two different sets of objects.
        /// Those that will destroy with the owner and those that will not.
        /// </summary>
        /// <param name="dontDestroyWithOwner">type of prefab to use for spawning</param>
        private void SpawnAllObjects(bool dontDestroyWithOwner)
        {
            var networkObjectIds = new List<ulong>();
            var nonAuthority = m_NetworkManagers.Where((c) => c.LocalClientId == m_NonAuthorityClientId).First();
            var objectToSpawn = dontDestroyWithOwner ? m_DontDestroyWithOwnerPrefab : m_DestroyWithOwnerPrefab;
            var spawnedObjects = SpawnObjects(objectToSpawn, nonAuthority, k_NumberObjectsToSpawn);
            foreach (var spawnedObject in spawnedObjects)
            {
                networkObjectIds.Add(spawnedObject.GetComponent<NetworkObject>().NetworkObjectId);
            }

            if (dontDestroyWithOwner)
            {
                m_DontDestroyObjectIds.Clear();
                m_DontDestroyObjectIds.AddRange(networkObjectIds);
            }
            else
            {
                m_DestroyObjectIds.Clear();
                m_DestroyObjectIds.AddRange(networkObjectIds);
            }
        }

        /// <summary>
        /// Validates that the dont destroy with owner object is parented under
        /// the destroy with owner object.
        /// </summary>
        private bool HaveAllObjectInstancesParented(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var relativeSpawnedObjects = networkManager.SpawnManager.SpawnedObjects;
                for (int i = 0; i < k_NumberObjectsToSpawn; i++)
                {
                    var dontDestroyObjectId = m_DontDestroyObjectIds[i];
                    var destroyObjectId = m_DestroyObjectIds[i];
                    var dontDestroyObject = (NetworkObject)null;
                    var destroyObject = (NetworkObject)null;
                    if (!relativeSpawnedObjects.ContainsKey(dontDestroyObjectId))
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][DontDestroyWithOwner] Has not spawned {nameof(NetworkObject)}-{dontDestroyObjectId}!");
                    }
                    else
                    {
                        dontDestroyObject = relativeSpawnedObjects[dontDestroyObjectId];
                    }
                    if (!relativeSpawnedObjects.ContainsKey(destroyObjectId))
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][DestroyWithOwner] Has not spawned {nameof(NetworkObject)}-{destroyObjectId}!");
                    }
                    else
                    {
                        destroyObject = relativeSpawnedObjects[destroyObjectId];
                    }

                    if (dontDestroyObject != null && destroyObject != null && dontDestroyObject.transform.parent != destroyObject.transform)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][Not Parented] {destroyObject.name} is not parented under {dontDestroyObject.name}!");
                    }
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// Parents the dont destroy with owner objects under the destroy with owner objects for the parenting portion of the test.
        /// </summary>
        private void ParentObjects()
        {
            var networkManager = !m_DistributedAuthority ? GetAuthorityNetworkManager() : m_NetworkManagers.Where((c) => c.LocalClientId == m_NonAuthorityClientId).First();
            for (int i = 0; i < k_NumberObjectsToSpawn; i++)
            {
                var dontDestroyObjectId = m_DontDestroyObjectIds[i];
                var destroyObjectId = m_DestroyObjectIds[i];
                var dontDestroyObject = networkManager.SpawnManager.SpawnedObjects[dontDestroyObjectId];
                var destroyObject = networkManager.SpawnManager.SpawnedObjects[destroyObjectId];
                Assert.IsTrue(dontDestroyObject.TrySetParent(destroyObject), $"[Client-{networkManager.LocalClientId}][Parent Failure] Could not parent {destroyObject.name} under {dontDestroyObject.name}!");
            }
        }

        /// <summary>
        /// Validates that the non-authority owner client disconnection
        /// was registered on all clients.
        /// </summary>
        private bool NonAuthorityHasDisconnected(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.IsListening)
                {
                    continue;
                }

                if (networkManager.ConnectedClientsIds.Contains(m_NonAuthorityClientId))
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][ClientDisconnect] Still thinks Client-{m_NonAuthorityClientId} is connected!");
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// The primary validation for the <see cref="DontDestroyWithOwnerTest"/>.
        /// This validates that:
        /// - Spawned objects that are set to destroy with the owner gets destroyed/despawned when the owning client disconnects.
        /// - Spawned objects that are set to not destroy with the owner are not destroyed/despawned when the owning client disconnects.
        /// </summary>
        private bool ValidateDontDestroyWithOwner(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var relativeSpawnedObjects = networkManager.SpawnManager.SpawnedObjects;
                for (int i = 0; i < k_NumberObjectsToSpawn; i++)
                {
                    var dontDestroyObjectId = m_DontDestroyObjectIds[i];
                    var destroyObjectId = m_DestroyObjectIds[i];
                    if (!relativeSpawnedObjects.ContainsKey(dontDestroyObjectId))
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][DontDestroyWithOwner][!Despawned!] {nameof(NetworkObject)}-{dontDestroyObjectId} should not despawn upon the owner disconnecting!");
                    }
                    if (relativeSpawnedObjects.ContainsKey(destroyObjectId))
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][DestroyWithOwner][!Not Despawned!] {nameof(NetworkObject)}-{destroyObjectId} should have despawned upon the owner disconnecting!");
                    }
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// The primary parented validation for the <see cref="DontDestroyWithOwnerTest"/>.
        /// This validates that:
        /// - Spawned objects that are set to destroy with the owner gets destroyed/despawned when the owning client disconnects.
        /// - Spawned objects that are set to not destroy with the owner and parented under a spawned object set to destroy with owner that
        /// the objects that are set to not destroy with the owner are not destroyed/despawned when the owning client disconnects.
        /// </summary>
        private bool ValidateParentedDontDestroyWithOwnerId(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                var relativeSpawnedObjects = networkManager.SpawnManager.SpawnedObjects;
                for (int i = 0; i < k_NumberObjectsToSpawn; i++)
                {
                    var dontDestroyObjectId = m_DontDestroyObjectIds[i];
                    var dontDestroyObjectOwnerId = relativeSpawnedObjects[dontDestroyObjectId].OwnerClientId;

                    if (dontDestroyObjectOwnerId == m_NonAuthorityClientId)
                    {
                        errorLog.AppendLine($"[Client-{networkManager.LocalClientId}][DontDestroyWithOwner][!Owner!] {nameof(NetworkObject)}-{dontDestroyObjectId} should not still belong to Client-{m_NonAuthorityClientId}!");
                    }
                }
            }
            return errorLog.Length == 0;
        }

        /// <summary>
        /// This validates that:
        /// - Spawned objects that are set to destroy with the owner gets destroyed/despawned when the owning client disconnects.
        /// - Spawned objects that are set to not destroy with the owner are not destroyed/despawned when the owning client disconnects.
        /// </summary>
        [UnityTest]
        public IEnumerator DontDestroyWithOwnerTest([Values] ParentedPass parentedPass)
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();
            m_NonAuthorityClientId = nonAuthority.LocalClientId;
            SpawnAllObjects(true);
            SpawnAllObjects(false);

            // This should never fail.
            Assert.IsTrue(m_DontDestroyObjectIds.Count == m_DestroyObjectIds.Count, $"Mismatch in spawn count! ({m_DontDestroyObjectIds.Count}) vs ({m_DestroyObjectIds.Count})");

            yield return WaitForConditionOrTimeOut(HaveAllObjectInstancesSpawned);
            AssertOnTimeout($"Timed out waiting for all clients to spawn objects!");

            if (parentedPass == ParentedPass.HasParent)
            {
                ParentObjects();
                yield return WaitForConditionOrTimeOut(HaveAllObjectInstancesParented);
                AssertOnTimeout($"Timed out waiting for all DontDestroy objects to be parented under the Destroy objects!");
            }

            yield return StopOneClient(nonAuthority);

            yield return WaitForConditionOrTimeOut(NonAuthorityHasDisconnected);
            AssertOnTimeout($"Timed out waiting for all clients to register that Client-{m_NonAuthorityClientId} has disconnected!");

            yield return WaitForConditionOrTimeOut(ValidateDontDestroyWithOwner);
            AssertOnTimeout($"Timed out while validating the base-line DontDestroyWithOwnerTest results!");

            if (parentedPass == ParentedPass.HasParent)
            {
                yield return WaitForConditionOrTimeOut(ValidateParentedDontDestroyWithOwnerId);
                AssertOnTimeout($"Timed out while validating the parented don't destroy objects do not still belong to disconnected Client-{m_NonAuthorityClientId}!");
            }
        }

        [UnityTest]
        public IEnumerator NetworkShowThenClientDisconnects()
        {
            var authorityManager = GetAuthorityNetworkManager();
            var networkObject = SpawnObject(m_PrefabNoObserversSpawn, authorityManager).GetComponent<NetworkObject>();
            var longWait = new WaitForSeconds(0.25f);
            // Wait long enough to assure that no client receives the spawn notification
            yield return longWait;

            foreach (var networkManager in m_NetworkManagers)
            {
                // Skip the authority as it will have an instance
                if (networkManager == authorityManager)
                {
                    continue;
                }
                Assert.False(networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObject.NetworkObjectId), $"[Client-{networkManager.LocalClientId}]" +
                    $" Spawned an instance of {networkObject.name} when it was spawned with no observers!");
            }

            // Get a non-authority client, show the spawned object to it, and then make that client the owner.
            var nonAuthorityManager = GetNonAuthorityNetworkManager();
            networkObject.NetworkShow(nonAuthorityManager.LocalClientId);
            // This validates that the change in ownership is not sent when making a NetworkObject visible and changing the ownership
            // within the same frame/callstack.
            networkObject.ChangeOwnership(nonAuthorityManager.LocalClientId);

            // Verifies the object was spawned on the non-authority client and that the non-authority client is the owner.
            yield return WaitForConditionOrTimeOut(() => nonAuthorityManager.SpawnManager.SpawnedObjects.ContainsKey(networkObject.NetworkObjectId)
            && nonAuthorityManager.SpawnManager.SpawnedObjects[networkObject.NetworkObjectId].OwnerClientId == nonAuthorityManager.LocalClientId);
            AssertOnTimeout($"[Client-{nonAuthorityManager.LocalClientId}] Failed to spawn {networkObject.name} when it was shown!");

            foreach (var networkManager in m_NetworkManagers)
            {
                // Skip the authority and the non-authority client as they should now both have instances
                if (networkManager == authorityManager || networkManager == nonAuthorityManager)
                {
                    continue;
                }

                // No other client should have an instance
                Assert.False(networkManager.SpawnManager.SpawnedObjects.ContainsKey(networkObject.NetworkObjectId), $"[Client-{networkManager.LocalClientId}]" +
                    $" Spawned an instance of {networkObject.name} when it was shown to Client-{nonAuthorityManager.LocalClientId}!");
            }

            // Wait a few frames
            yield return s_DefaultWaitForTick;

            // Shutdown the non-authority client to assure this does not cause the spawned object to despawn
            nonAuthorityManager.Shutdown();

            // Wait long enough to assure all messages generated from the client shutting down have been processed
            yield return longWait;

            // Validate the object is still spawned
            Assert.True(networkObject.IsSpawned, $"The spawned test prefab was despawned on the authority side when it shouldn't have been!");
        }
    }
}
