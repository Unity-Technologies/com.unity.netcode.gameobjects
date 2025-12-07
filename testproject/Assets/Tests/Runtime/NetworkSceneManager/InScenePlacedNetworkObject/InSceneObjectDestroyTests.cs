using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// These tests extend off InScenePlacedNetworkObjectTests.
    /// Extending allows the DeferDespawn tests to not be created for client-server topology.
    /// These tests specifically test destroying and despawning in-scene placed network objects.
    /// </summary>
    [TestFixture(NetworkTopologyTypes.DistributedAuthority, DespawnMode.Despawn)]
    [TestFixture(NetworkTopologyTypes.DistributedAuthority, DespawnMode.DeferDespawn)]
    [TestFixture(NetworkTopologyTypes.ClientServer, DespawnMode.Despawn)]
    public class InSceneObjectDestroyTests : InSceneObjectBase
    {
        protected override int NumberOfClients => 2;

        private readonly DespawnMode m_DespawnMode;

        public InSceneObjectDestroyTests(NetworkTopologyTypes networkTopologyType, DespawnMode despawnMode) : base(networkTopologyType)
        {
            m_DespawnMode = despawnMode;
        }

        public enum DespawnMode
        {
            Despawn,
            DeferDespawn,
        }

        private enum DestroyMode
        {
            DestroyGameObject,
            DespawnGameObject,
        }

        private NetworkObject m_JoinedClientDespawnedNetworkObject;
        private void OnInSceneObjectDespawned(NetworkObject networkObject)
        {
            m_JoinedClientDespawnedNetworkObject = networkObject;
            NetworkObjectTestComponent.OnInSceneObjectDespawned -= OnInSceneObjectDespawned;
        }

        /// <summary>
        /// This verifies that in-scene placed NetworkObjects are properly handled when they are called with NetworkObject.Despawn(false)
        /// </summary>
        [UnityTest]
        public IEnumerator InSceneNetworkObjectDestroy()
        {
            yield return LoadSceneAndDespawnObject(DestroyMode.DestroyGameObject);

            // Late joining a client when destroying a game object is not a supported pattern.
        }

        /// <summary>
        /// This verifies NetworkObject.Despawn() works as expected with the given option for destroyGameObject
        /// Used by other tests to test specific use cases.
        /// </summary>
        private IEnumerator LoadSceneAndDespawnObject(DestroyMode destroyMode)
        {
            var authority = GetAuthorityNetworkManager();
            var destroyGameObject = destroyMode == DestroyMode.DestroyGameObject;

            VerboseDebug("Loading scene");
            var status = authority.SceneManager.LoadScene(SceneToLoad, LoadSceneMode.Additive);
            Assert.IsTrue(status == SceneEventProgressStatus.Started, $"When attempting to load scene {SceneToLoad} was returned the following progress status: {status}");

            // This verifies the scene loaded and the in-scene placed NetworkObjects spawned.
            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawnedInSceneObject);
            AssertOnTimeout($"Timed out waiting for total spawned in-scene placed NetworkObjects to reach a count of {TotalClients} and is currently {NetworkObjectTestComponent.SpawnedInstances.Count}");

            yield return WaitForConditionOrTimeOut(HaveAllClientsLoadedScene);
            AssertOnTimeout($"Timed out waiting for all clients to finish loading scene {SceneToLoad}!");

            // Get the server-side instance of the in-scene NetworkObject
            Assert.True(s_GlobalNetworkObjects.ContainsKey(authority.LocalClientId), "Could not find server instance of the test in-scene NetworkObject!");
            var serverObject = NetworkObjectTestComponent.ServerNetworkObjectInstance;
            var serverObjectId = serverObject.NetworkObjectId;
            var spawnedObjects = NetworkObjectTestComponent.SpawnedObjects;
            Assert.IsNotNull(serverObject, "Could not find server-side in-scene placed NetworkObject!");
            Assert.IsTrue(serverObject.IsSpawned, $"{serverObject.name} is not spawned!");

            VerboseDebug($"Doing despawn. destroyGameObject: {destroyGameObject}");
            // Despawn the in-scene placed NetworkObject
            if (m_DespawnMode == DespawnMode.Despawn)
            {
                serverObject.Despawn(destroyGameObject);
            }
            else
            {
                serverObject.DeferDespawn(1, destroyGameObject);
            }

            const string expectedLog = "[Netcode] Destroying in-scene network objects can lead to unexpected behavior. It is recommended to use NetworkObject.Despawn(false) instead.";
            if (destroyGameObject)
            {
                NetcodeLogAssert.LogWasReceived(LogType.Warning, expectedLog);
            }
            else
            {
                NetcodeLogAssert.LogWasNotReceived(LogType.Warning, expectedLog);
            }

            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == 0);
            AssertOnTimeout($"Timed out waiting for all in-scene instances to be despawned!  Current spawned count: {NetworkObjectTestComponent.SpawnedInstances.Count()}");

            foreach (var manager in m_NetworkManagers)
            {
                Assert.False(manager.SpawnManager.SpawnedObjects.ContainsKey(serverObjectId), $"Client-{manager.LocalClientId} still has in-scene instance spawned!");
            }

            foreach (var spawnedObject in spawnedObjects)
            {
                if (destroyMode == DestroyMode.DestroyGameObject)
                {
                    Assert.True(spawnedObject == null, "Expected game object to be destroyed!");
                }
                else
                {
                    Assert.False(spawnedObject == null, "Expected game object to still exist!");
                }
            }
        }

        /// <summary>
        /// This verifies that in-scene placed NetworkObjects will be properly
        /// synchronized if:
        /// 1.) Despawned prior to a client late-joining
        /// 2.) Re-spawned after having been despawned without registering the in-scene
        /// NetworkObject as a NetworkPrefab
        /// </summary>
        [UnityTest]
        public IEnumerator InSceneNetworkObjectDespawnSyncAndSpawn()
        {
            yield return LoadSceneAndDespawnObject(DestroyMode.DespawnGameObject);

            var serverObject = NetworkObjectTestComponent.ServerNetworkObjectInstance;

            Assert.IsNotNull(serverObject, "Could not find server-side in-scene placed NetworkObject!");

            VerboseDebug("Late joining client");
            // Now late join a client
            NetworkObjectTestComponent.OnInSceneObjectDespawned += OnInSceneObjectDespawned;

            var lateJoinClient = CreateNewClient();
            yield return StartClient(lateJoinClient);

            // Make sure the late-joining client's in-scene placed NetworkObject received the despawn notification during synchronization
            Assert.IsNotNull(m_JoinedClientDespawnedNetworkObject, $"{lateJoinClient.name} did not despawn the in-scene placed NetworkObject when connecting and synchronizing!");

            // We should still have no spawned in-scene placed NetworkObjects at this point
            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == 0);
            AssertOnTimeout($"{lateJoinClient.name} spawned in-scene placed NetworkObject!");

            VerboseDebug("Respawn despawned object");
            // Now test that the despawned in-scene placed NetworkObject can be re-spawned (without having been registered as a NetworkPrefab)
            serverObject.Spawn();

            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawnedInSceneObject);
            AssertOnTimeout($"Timed out waiting for all in-scene instances to be spawned!  Current spawned count: {NetworkObjectTestComponent.SpawnedInstances.Count()} | Expected spawn count: {TotalClients}");

            VerboseDebug("Network hiding object on first client");

            // Test NetworkHide on the first client
            var firstClientId = GetNonAuthorityNetworkManager(0).LocalClientId;

            serverObject.NetworkHide(firstClientId);
            var visibleCount = TotalClients - 1;

            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == visibleCount);
            AssertOnTimeout($"[NetworkHide] Timed out waiting for Client-{firstClientId} to despawn the in-scene placed NetworkObject! Current spawned count: {NetworkObjectTestComponent.SpawnedInstances.Count()} | Expected spawn count: {visibleCount}");

            VerboseDebug("Network showing object on first client");
            // Validate that the first client can spawn the "netcode hidden" in-scene placed NetworkObject
            serverObject.NetworkShow(firstClientId);

            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawnedInSceneObject);
            AssertOnTimeout($"[NetworkShow] Timed out waiting for Client-{firstClientId} to spawn the in-scene placed NetworkObject! Current spawned count: {NetworkObjectTestComponent.SpawnedInstances.Count()} | Expected spawn count: {TotalClients}");
        }
    }
}
