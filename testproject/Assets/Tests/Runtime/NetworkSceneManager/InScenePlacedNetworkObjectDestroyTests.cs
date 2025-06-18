using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.DistributedAuthority, DespawnMode.Despawn)]
    [TestFixture(NetworkTopologyTypes.DistributedAuthority, DespawnMode.DeferDespawn)]
    [TestFixture(NetworkTopologyTypes.ClientServer, DespawnMode.Despawn)]
    public class InScenePlacedNetworkObjectDestroyTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 2;

        private const string k_SceneToLoad = "InSceneNetworkObject";
        private Scene m_ServerSideSceneLoaded;

        // private string m_SceneLoading = k_SceneToLoad;
        private readonly DespawnMode m_DespawnMode;

        public InScenePlacedNetworkObjectDestroyTests(NetworkTopologyTypes networkTopologyType, DespawnMode despawnMode) : base(networkTopologyType)
        {
            m_DespawnMode = despawnMode;
        }

        protected override IEnumerator OnSetup()
        {
            NetworkObjectTestComponent.VerboseDebug = m_EnableVerboseDebug;
            return base.OnSetup();
        }

        /// <summary>
        /// Very important to always have a backup "unloading" catch
        /// in the event your test fails it could not potentially unload
        /// a scene and the proceeding tests could be impacted by this!
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator OnTearDown()
        {
            NetworkObjectTestComponent.Reset();
            yield return CleanUpLoadedScene();
        }

        public enum DespawnMode
        {
            Despawn,
            DeferDespawn,
        }

        private enum DestroyMode
        {
            DestroyGameObject,
            DontDestroyGameObject,
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

            authority.SceneManager.OnSceneEvent += Server_OnSceneEvent;
            VerboseDebug("Loading scene");
            var status = authority.SceneManager.LoadScene(k_SceneToLoad, LoadSceneMode.Additive);
            Assert.IsTrue(status == SceneEventProgressStatus.Started, $"When attempting to load scene {k_SceneToLoad} was returned the following progress status: {status}");

            // This verifies the scene loaded and the in-scene placed NetworkObjects spawned.
            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == TotalClients);
            AssertOnTimeout($"Timed out waiting for total spawned in-scene placed NetworkObjects to reach a count of {TotalClients} and is currently {NetworkObjectTestComponent.SpawnedInstances.Count}");

            yield return WaitForConditionOrTimeOut(() => m_ServerSideSceneLoaded.IsValid() && m_ServerSideSceneLoaded.isLoaded);
            AssertOnTimeout($"Timed out waiting for server to finish loading scene {k_SceneToLoad}!");

            // Get the server-side instance of the in-scene NetworkObject
            Assert.True(s_GlobalNetworkObjects.ContainsKey(authority.LocalClientId), "Could not find server instance of the test in-scene NetworkObject!");
            var serverObject = NetworkObjectTestComponent.ServerNetworkObjectInstance;
            var serverObjectId = serverObject.NetworkObjectId;
            var spawnedObjects = NetworkObjectTestComponent.SpawnedObjects;
            Assert.IsNotNull(serverObject, "Could not find server-side in-scene placed NetworkObject!");
            Assert.IsTrue(serverObject.IsSpawned, $"{serverObject.name} is not spawned!");

            VerboseDebug("Doing despawn");
            // Despawn the in-scene placed NetworkObject
            if (m_DespawnMode == DespawnMode.Despawn)
            {
                serverObject.Despawn(destroyGameObject);
            }
            else
            {
                serverObject.DeferDespawn(1, destroyGameObject);
            }

            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == 0);
            AssertOnTimeout($"Timed out waiting for all in-scene instances to be despawned!  Current spawned count: {NetworkObjectTestComponent.SpawnedInstances.Count()}");


            foreach (var manager in m_NetworkManagers)
            {
                Assert.False(manager.SpawnManager.SpawnedObjects.ContainsKey(serverObjectId));
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
            yield return LoadSceneAndDespawnObject(DestroyMode.DontDestroyGameObject);

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

            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == TotalClients);
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

            yield return WaitForConditionOrTimeOut(() => NetworkObjectTestComponent.SpawnedInstances.Count == TotalClients);
            AssertOnTimeout($"[NetworkShow] Timed out waiting for Client-{firstClientId} to spawn the in-scene placed NetworkObject! Current spawned count: {NetworkObjectTestComponent.SpawnedInstances.Count()} | Expected spawn count: {TotalClients}");

            yield return CleanUpLoadedScene();
        }

        private NetworkObject m_JoinedClientDespawnedNetworkObject;

        private void OnInSceneObjectDespawned(NetworkObject networkObject)
        {
            m_JoinedClientDespawnedNetworkObject = networkObject;
            NetworkObjectTestComponent.OnInSceneObjectDespawned -= OnInSceneObjectDespawned;
        }

        private void Server_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.ClientId == GetAuthorityNetworkManager().LocalClientId && sceneEvent.SceneEventType == SceneEventType.LoadComplete
                                                                                  && sceneEvent.Scene.IsValid() && sceneEvent.Scene.isLoaded)
            {
                m_ServerSideSceneLoaded = sceneEvent.Scene;
                GetAuthorityNetworkManager().SceneManager.OnSceneEvent -= Server_OnSceneEvent;
            }
        }

        private IEnumerator CleanUpLoadedScene()
        {
            if (m_ServerSideSceneLoaded.IsValid() && m_ServerSideSceneLoaded.isLoaded)
            {
                GetAuthorityNetworkManager().SceneManager.UnloadScene(m_ServerSideSceneLoaded);
                yield return WaitForConditionOrTimeOut(() => m_ClientNetworkManagers.Any(c => c.IsListening));
                AssertOnTimeout($"[CleanUpLoadedScene] Timed out waiting for all in-scene instances to be despawned!  Current spawned count: {m_ClientNetworkManagers.Count(c => !c.IsListening)}");
            }
        }
    }
}
