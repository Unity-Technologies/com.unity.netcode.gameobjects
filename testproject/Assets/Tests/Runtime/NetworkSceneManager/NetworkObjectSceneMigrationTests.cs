using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// NetworkObject Scene Migration Integration Tests
    /// </summary>
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkObjectSceneMigrationTests : NetcodeIntegrationTest
    {
        private readonly List<string> m_TestScenes = new() { "EmptyScene1", "EmptyScene2", "EmptyScene3" };
        protected override int NumberOfClients => 2;

        private GameObject m_TestPrefab;
        private GameObject m_TestPrefabAutoSynchActiveScene;
        private GameObject m_TestPrefabDestroyWithScene;
        private Scene m_OriginalActiveScene;

        private List<NetworkObject> m_ServerSpawnedPrefabInstances = new List<NetworkObject>();
        private List<NetworkObject> m_ServerSpawnedDestroyWithSceneInstances = new List<NetworkObject>();
        private readonly List<Scene> m_ScenesLoaded = new List<Scene>();

        public NetworkObjectSceneMigrationTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        // TODO: [MTT-15430] Fix automatic scene object migration between clients
        protected override bool UseCMBService()
        {
            return false;
        }

        protected override IEnumerator OnSetup()
        {
            m_OriginalActiveScene = SceneManager.GetActiveScene();
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            Object.DontDestroyOnLoad(m_PlayerPrefab);
            m_PlayerPrefab.GetComponent<NetworkObject>().ActiveSceneSynchronization = true;
            base.OnCreatePlayerPrefab();
        }

        protected override void OnServerAndClientsCreated()
        {
            // Synchronize Scene Changes (default) Test Network Prefab
            m_TestPrefab = CreateNetworkObjectPrefab("TestObject");
            m_TestPrefab.AddComponent<SceneOriginTracker>();

            // Auto Synchronize Active Scene Changes Test Network Prefab
            m_TestPrefabAutoSynchActiveScene = CreateNetworkObjectPrefab("ActiveSceneSynchronizationObject");
            m_TestPrefabAutoSynchActiveScene.GetComponent<NetworkObject>().ActiveSceneSynchronization = true;
            m_TestPrefabAutoSynchActiveScene.AddComponent<SceneOriginTracker>();

            // Destroy With Scene Test Network Prefab
            m_TestPrefabDestroyWithScene = CreateNetworkObjectPrefab("DestroyWithSceneObject");
            m_TestPrefabDestroyWithScene.AddComponent<DestroyWithSceneInstancesTestHelper>();
            m_TestPrefabDestroyWithScene.AddComponent<SceneOriginTracker>();

            var neverSpawnObj = CreateNetworkObjectPrefab("ShouldNeverSpawn");
            var shouldNeverSpawn = neverSpawnObj.AddComponent<ShouldNeverSpawn>();
            DestroyWithSceneInstancesTestHelper.ShouldNeverSpawn = shouldNeverSpawn;

            var authority = GetAuthorityNetworkManager();
            authority.OnServerStarted += OnServerStarted;

            base.OnServerAndClientsCreated();
        }


        private void OnServerStarted()
        {
            var authority = GetAuthorityNetworkManager();
            authority.OnServerStarted -= OnServerStarted;
            authority.SceneManager.ActiveSceneSynchronizationEnabled = true;
        }

        private enum ExpectedLoadType
        {
            Loaded,
            Unloaded
        }

        private bool ValidateSceneOnAllClients(StringBuilder errorLog, string sceneName, ExpectedLoadType loadType)
        {
            var allValid = true;
            foreach (var networkManager in m_NetworkManagers)
            {
                var sceneLoaded = false;
                foreach (var scene in networkManager.SceneManager.ScenesLoaded.Values)
                {
                    if (scene.name == sceneName)
                    {
                        sceneLoaded = true;
                        break;
                    }
                }
                if (!sceneLoaded && loadType == ExpectedLoadType.Loaded)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] scene {sceneName} wasn't loaded on this client!");
                    allValid = false;
                }
                else if (sceneLoaded && loadType == ExpectedLoadType.Unloaded)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] scene {sceneName} was still loaded on this client!");
                    allValid = false;
                }
            }

            return allValid;
        }

        private bool VerifyAllScenesMatch(StringBuilder errorLog, List<NetworkObject> authorityInstances)
        {
            foreach (var authorityInstance in authorityInstances)
            {
                foreach (var networkManager in m_NetworkManagers)
                {
                    if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityInstance.NetworkObjectId, out var instance))
                    {
                        errorLog.AppendLine($"[{authorityInstance.name}] Client-{networkManager.LocalClientId} doesn't have a local version of network object {authorityInstance.name} with id {authorityInstance.NetworkObjectId}");
                        return false;
                    }

                    if (instance.gameObject.scene.name != authorityInstance.gameObject.scene.name)
                    {
                        errorLog.AppendLine($"[{instance.name}] NetworkObject-{authorityInstance.NetworkObjectId} is in the wrong scene! Expected: {authorityInstance.gameObject.scene.name}, Actual: {instance.gameObject.scene.name}");
                        return false;
                    }

                    // The SceneOrigin should never change
                    var originalSceneTracker = instance.GetComponent<SceneOriginTracker>();
                    Assert.AreEqual(originalSceneTracker.SceneWhereAwakeHappened, (NetworkSceneHandle)instance.SceneOrigin.handle, "The SceneOrigin of an object should never change!");
                }
            }

            return true;
        }

        private const int k_MaxObjectsToSpawn = 9;
        /// <summary>
        /// Integration test to verify that migrating NetworkObjects
        /// into different scenes (in the same frame) is synchronized
        /// with connected clients and synchronizes with late joining
        /// clients.
        /// </summary>
        [UnityTest]
        public IEnumerator MigrateIntoNewSceneTest()
        {
            var authority = GetAuthorityNetworkManager();

            var authoritySpawnedInstances = new List<NetworkObject>();
            // Spawn 9 NetworkObject instances
            for (int i = 0; i < k_MaxObjectsToSpawn; i++)
            {
                var instance = SpawnObject(m_TestPrefab, authority);
                var spawnedObject = instance.GetComponent<NetworkObject>();
                authoritySpawnedInstances.Add(spawnedObject);
            }

            yield return WaitForSpawnedOnAllOrTimeOut(authoritySpawnedInstances);
            AssertOnTimeout($"Timed out waiting for all clients to spawn {nameof(NetworkObject)}s!");

            // Now load three scenes to migrate the newly spawned NetworkObjects into
            authority.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            foreach (var sceneToLoad in m_TestScenes)
            {
                var status = authority.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
                Assert.True(status == SceneEventProgressStatus.Started, $"Failed to start loading scene {sceneToLoad}! Return status: {status}");
                yield return WaitForConditionOrTimeOut(errorLog => ValidateSceneOnAllClients(errorLog, sceneToLoad, ExpectedLoadType.Loaded));
                AssertOnTimeout($"Timed out waiting for all clients to load scene {sceneToLoad}!");
            }
            authority.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;
            Assert.AreEqual(m_TestScenes.Count, m_ScenesLoaded.Count, "Not all the test scenes were loaded!");

            yield return WaitForConditionOrTimeOut(errorLog => VerifyAllScenesMatch(errorLog, authoritySpawnedInstances));
            AssertOnTimeout("[After spawn] Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            var objectCount = 0;
            // Migrate each networkObject into one of the three scenes.
            // There will be 3 networkObjects per newly loaded scenes when done.
            foreach (var scene in m_ScenesLoaded)
            {
                // Now migrate the NetworkObject
                SceneManager.MoveGameObjectToScene(authoritySpawnedInstances[objectCount].gameObject, scene);
                SceneManager.MoveGameObjectToScene(authoritySpawnedInstances[objectCount + 1].gameObject, scene);
                SceneManager.MoveGameObjectToScene(authoritySpawnedInstances[objectCount + 2].gameObject, scene);
                objectCount += 3;
            }

            yield return WaitForConditionOrTimeOut(errorLog => VerifyAllScenesMatch(errorLog, authoritySpawnedInstances));
            AssertOnTimeout($"Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Register for the server-side client synchronization so we can send an object scene migration event at the same time
            // the new client begins to synchronize
            m_ServerSpawnedPrefabInstances = authoritySpawnedInstances;
            authority.SceneManager.OnSynchronize += MigrateObjects_OnSynchronize;

            // Verify that a late joining client synchronizes properly even while new scene migrations occur
            // during its synchronization
            yield return CreateAndStartNewClient();
            yield return WaitForConditionOrTimeOut(errorLog => VerifyAllScenesMatch(errorLog, authoritySpawnedInstances));

            AssertOnTimeout($"[Late Joined Client] Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Verify that a late joining client synchronizes properly even if we migrate
            // during its synchronization and despawn some of the NetworkObjects migrated.
            authority.SceneManager.OnSynchronize += MigrateAndDespawnObjects_OnSynchronize;
            yield return CreateAndStartNewClient();
            yield return WaitForConditionOrTimeOut(errorLog => VerifyAllScenesMatch(errorLog, authoritySpawnedInstances));

            AssertOnTimeout($"[Late Joined Client] Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");
        }

        /// <summary>
        /// Part of NetworkObject scene migration tests to verify that a NetworkObject
        /// migrated to a scene and then despawned will be handled properly for clients
        /// in the middle of synchronization.
        /// </summary>
        private void MigrateAndDespawnObjects_OnSynchronize(ulong clientId)
        {
            var authority = GetAuthorityNetworkManager();
            // Migrate the NetworkObjects into different scenes than they originally were migrated into
            for (int i = m_ServerSpawnedPrefabInstances.Count - 1; i >= 0; i--)
            {
                var scene = m_ScenesLoaded[i % m_ScenesLoaded.Count];
                var obj = m_ServerSpawnedPrefabInstances[i];
                if (m_DistributedAuthority)
                {
                    // When the new client joins, authority will be distributed.
                    // Ensure we have the authority instance.
                    obj = GetAuthorityInstance(obj);
                }
                SceneManager.MoveGameObjectToScene(obj.gameObject, scene);
                // De-spawn every-other object
                if (i % 2 == 0)
                {
                    obj.Despawn();
                    m_ServerSpawnedPrefabInstances.RemoveAt(i);
                }
            }
            authority.SceneManager.OnSynchronize -= MigrateObjects_OnSynchronize;
        }

        /// <summary>
        /// Migrate objects into other scenes when a client begins synchronization
        /// </summary>
        /// <param name="clientId"></param>
        private void MigrateObjects_OnSynchronize(ulong clientId)
        {
            var objectCount = k_MaxObjectsToSpawn - 1;

            // Migrate the NetworkObjects into different scenes than they originally were migrated into
            foreach (var scene in m_ScenesLoaded)
            {
                SceneManager.MoveGameObjectToScene(m_ServerSpawnedPrefabInstances[objectCount].gameObject, scene);
                SceneManager.MoveGameObjectToScene(m_ServerSpawnedPrefabInstances[objectCount - 1].gameObject, scene);
                SceneManager.MoveGameObjectToScene(m_ServerSpawnedPrefabInstances[objectCount - 2].gameObject, scene);
                objectCount -= 3;
            }

            // Unsubscribe to this event for this part of the test
            GetAuthorityNetworkManager().SceneManager.OnSynchronize -= MigrateObjects_OnSynchronize;
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            var authority = GetAuthorityNetworkManager();
            foreach (var prefab in authority.NetworkConfig.Prefabs.Prefabs)
            {
                networkManager.NetworkConfig.Prefabs.Add(prefab);
            }
            networkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            base.OnNewClientCreated(networkManager);
        }

        private void SetActiveScene(Scene scene)
        {
            Debug.Log($"[Previous = {SceneManager.GetActiveScene().name}][New = {scene.name}] Changing the active scene!");
            SceneManager.SetActiveScene(scene);
        }

        /// <summary>
        /// Integration test to verify changing the currently active scene
        /// will migrate NetworkObjects with ActiveSceneSynchronization set
        /// to true.
        /// </summary>
        [UnityTest]
        public IEnumerator ActiveSceneSynchronizationTest()
        {
            var authority = GetAuthorityNetworkManager();
            // Disable resynchronization for this test to avoid issues with trying
            // to synchronize them.
            NetworkSceneManager.DisableReSynchronization = true;

            // Load three scenes first
            authority.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            foreach (var sceneName in m_TestScenes)
            {
                var loadStatus = authority.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                Assert.True(loadStatus == SceneEventProgressStatus.Started, $"Failed to start loading scene {sceneName}! Return status: {loadStatus}");
                yield return WaitForConditionOrTimeOut(errorLog => ValidateSceneOnAllClients(errorLog, sceneName, ExpectedLoadType.Loaded));
                AssertOnTimeout($"Timed out waiting for all clients to load scene {sceneName}!");
            }
            authority.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;

            // Set the active scene to be the 1st scene loaded so we don't instantiate within the test runner scene.
            SetActiveScene(m_ScenesLoaded[0]);

            var autoSyncActive = new List<NetworkObject>();
            // Spawn 3 NetworkObject instances that auto synchronize to active scene changes
            for (int i = 0; i < 3; i++)
            {
                // We are also testing that objects marked to synchronize with changes to
                // the active scene and marked to destroy with scene =are destroyed= if
                // the scene being unloaded is currently the active scene and the scene that
                // the NetworkObjects reside within.
                var serverInstance = SpawnObject(m_TestPrefabAutoSynchActiveScene, authority, true);
                var serverNetworkObject = serverInstance.GetComponent<NetworkObject>();
                autoSyncActive.Add(serverNetworkObject);
            }
            // Spawn 3 NetworkObject instances that do not auto synchronize to active scene changes
            // and ==should not be== destroyed with the scene (these should be the only remaining
            // instances)
            var autoSyncInactive = new List<NetworkObject>();
            for (int i = 0; i < 3; i++)
            {
                // This set of NetworkObjects will be used to verify that NetworkObjets
                // spawned with DestroyWithScene set to false will migrate into the current
                // active scene if the scene they currently reside within is destroyed and
                // is not the currently active scene.
                var serverInstance = SpawnObject(m_TestPrefab, authority);
                var serverNetworkObject = serverInstance.GetComponent<NetworkObject>();
                autoSyncInactive.Add(serverNetworkObject);
            }

            // Spawn 3 NetworkObject instances that do not auto synchronize to active scene changes
            // and ==should be== destroyed with the scene when it is unloaded
            for (int i = 0; i < 3; i++)
            {
                var serverInstance = Object.Instantiate(m_TestPrefabDestroyWithScene);
                var serverNetworkObject = serverInstance.GetComponent<NetworkObject>();
                // This set of NetworkObjects will be used to verify that NetworkObjets
                // spawned with DestroyWithScene == true will get destroyed when the scene
                // is unloaded
                serverNetworkObject.Spawn(true);
                m_ServerSpawnedDestroyWithSceneInstances.Add(serverNetworkObject);
            }
            var authoritySpawnedInstances = new List<NetworkObject>();
            authoritySpawnedInstances.AddRange(autoSyncActive);
            authoritySpawnedInstances.AddRange(autoSyncInactive);
            authoritySpawnedInstances.AddRange(m_ServerSpawnedDestroyWithSceneInstances);

            yield return WaitForSpawnedOnAllOrTimeOut(authoritySpawnedInstances);
            AssertOnTimeout($"Timed out waiting for all clients to spawn {nameof(NetworkObject)}s!");




            var sceneToMigrateTo = m_ScenesLoaded[2];
            // Migrate the instances that don't synchronize with active scene changes into the 3rd loaded scene
            // (We are making sure these stay in the same scene they are migrated into)
            foreach (var spawnedObject in autoSyncInactive)
            {
                SceneManager.MoveGameObjectToScene(spawnedObject.gameObject, sceneToMigrateTo);
            }

            // Migrate the instances that don't synchronize with active scene changes and are destroyed with the
            // scene unloading into the 3rd loaded scene
            // (We are making sure these get destroyed when the scene is unloaded)
            foreach (var spawnedObject in m_ServerSpawnedDestroyWithSceneInstances)
            {
                SceneManager.MoveGameObjectToScene(spawnedObject.gameObject, sceneToMigrateTo);
            }

            // Make sure they migrated to the proper scene
            yield return WaitForConditionOrTimeOut(errorLog => VerifyAllScenesMatch(errorLog, authoritySpawnedInstances));
            AssertOnTimeout($"Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Now change the active scene
            var newActiveScene = m_ScenesLoaded[1];
            SetActiveScene(newActiveScene);

            // We have to do this
            //Object.DontDestroyOnLoad(m_TestPrefabAutoSynchActiveScene);

            // First, make sure server-side scenes and client side scenes match
            yield return WaitForConditionOrTimeOut(errorLog => VerifyAllScenesMatch(errorLog, authoritySpawnedInstances));
            AssertOnTimeout($"Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Verify that the auto-active-scene synchronization NetworkObjects migrated to the newly
            // assigned active scene
            foreach (var obj in autoSyncActive)
            {
                Assert.AreEqual(newActiveScene, obj.gameObject.scene, $"{obj.gameObject.name} did not migrate into scene {newActiveScene.name}!");
            }

            // Verify that the other NetworkObjects that don't synchronize with active scene changes did
            // not migrate into the active scene.
            foreach (var obj in autoSyncInactive)
            {
                Assert.AreNotEqual(newActiveScene, obj.gameObject.scene, $"{obj.gameObject.name} migrated into scene {newActiveScene.name}!");
            }

            foreach (var obj in m_ServerSpawnedDestroyWithSceneInstances)
            {
                Assert.AreNotEqual(newActiveScene, obj.gameObject.scene, $"{obj.gameObject.name} migrated into scene {newActiveScene.name}!");
            }

            // Verify that a late joining client synchronizes properly and destroys the appropriate NetworkObjects
            yield return CreateAndStartNewClient();
            AssertOnTimeout("Failed to start or create a new client!");
            yield return WaitForConditionOrTimeOut(errorLog => VerifyAllScenesMatch(errorLog, authoritySpawnedInstances));
            AssertOnTimeout($"[Late Joined Client #1] Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Now, unload the scene containing the NetworkObjects that don't synchronize with active scene changes
            DestroyWithSceneInstancesTestHelper.NetworkObjectDestroyed += OnNonActiveSynchDestroyWithSceneNetworkObjectDestroyed;
            var status = authority.SceneManager.UnloadScene(sceneToMigrateTo);
            Assert.True(status == SceneEventProgressStatus.Started, $"Failed to start unloading scene {sceneToMigrateTo.name} with status {status}!");
            yield return WaitForConditionOrTimeOut(log => ValidateSceneOnAllClients(log, sceneToMigrateTo.name, ExpectedLoadType.Unloaded));

            // Clean up any destroyed NetworkObjects
            for (int i = authoritySpawnedInstances.Count - 1; i >= 0; i--)
            {
                if (authoritySpawnedInstances[i] == null)
                {
                    authoritySpawnedInstances.RemoveAt(i);
                }
            }

            AssertOnTimeout($"Timed out waiting for all clients to unload scene {sceneToMigrateTo.name}!");
            yield return WaitForConditionOrTimeOut(errorLog => VerifyAllScenesMatch(errorLog, authoritySpawnedInstances));
            AssertOnTimeout($"Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Verify that the NetworkObjects that don't synchronize with active scene changes but marked to not
            // destroy with the scene are migrated into the current active scene
            foreach (var obj in autoSyncInactive)
            {
                Assert.True(obj.gameObject.scene == newActiveScene, $"{obj.gameObject.name} did not migrate into scene {newActiveScene.name} but are in scene {obj.gameObject.scene.name}!");
            }

            // Verify all NetworkObjects that should have been destroyed with the scene unloaded were destroyed
            yield return WaitForConditionOrTimeOut(() => DestroyWithSceneInstancesTestHelper.ObjectRelativeInstances.Count == 0);
            DestroyWithSceneInstancesTestHelper.NetworkObjectDestroyed -= OnNonActiveSynchDestroyWithSceneNetworkObjectDestroyed;
            AssertOnTimeout($"Timed out waiting for all client instances marked to destroy when the scene unloaded to be despawned and destroyed.");

            // Now unload the active scene to verify all remaining NetworkObjects are migrated into the SceneManager
            // assigned active scene
            m_UnloadEventCompleted = false;
            authority.SceneManager.OnUnloadEventCompleted += OnUnloadEventCompleted;
            authority.SceneManager.UnloadScene(newActiveScene);

            // Always first: make sure the scene event has completed.
            yield return WaitForConditionOrTimeOut(() => m_UnloadEventCompleted);
            AssertOnTimeout($"Timed out waiting for all clients to unload scene {newActiveScene.name}!");

            // Always second: make sure all spawned objects are in the correct scene
            yield return WaitForConditionOrTimeOut(log => ValidateSceneOnAllClients(log, sceneToMigrateTo.name, ExpectedLoadType.Unloaded));
            AssertOnTimeout($"Timed out waiting for all clients to validate the correct scenes for spawned objects!");

            // Clean up any destroyed NetworkObjects
            for (int i = authoritySpawnedInstances.Count - 1; i >= 0; i--)
            {
                if (authoritySpawnedInstances[i] == null)
                {
                    authoritySpawnedInstances.RemoveAt(i);
                }
            }

            // Verify a late joining client will synchronize properly with the end result
            yield return CreateAndStartNewClient();

            // Verify the late joining client spawns all instances
            yield return WaitForSpawnedOnAllOrTimeOut(authoritySpawnedInstances);
            AssertOnTimeout($"[Late Joined Client #2] Timed out waiting for all clients to spawn {nameof(NetworkObject)}s!");

            // Verify the instances are in the correct scenes
            yield return WaitForConditionOrTimeOut(errorLog => VerifyAllScenesMatch(errorLog, authoritySpawnedInstances));
            AssertOnTimeout($"[Late Joined Client #2] Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // All but 3 instances should be destroyed
            Assert.IsEmpty(autoSyncActive.Where(obj => obj != null), $"All the NetworkObjects with {nameof(NetworkObject.ActiveSceneSynchronization)}=true should have been destroyed when the active scene was unloaded!");
            Assert.IsEmpty(m_ServerSpawnedDestroyWithSceneInstances.Where(obj => obj != null), $"All the NetworkObjects with {nameof(NetworkObject.DestroyWithScene)} should have been destroyed when the active scene was unloaded!");
            Assert.AreEqual(3, autoSyncInactive.Count(obj => obj != null), $"All the NetworkObjects with {nameof(NetworkObject.ActiveSceneSynchronization)}=false should have survived the active scene change!");
        }


        private bool m_UnloadEventCompleted;
        private void OnUnloadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            m_UnloadEventCompleted = true;
        }

        /// <summary>
        /// Callback invoked when a test prefab, with the <see cref="DestroyWithSceneInstancesTestHelper"/>
        /// component attached, is destroyed.
        /// </summary>
        private void OnNonActiveSynchDestroyWithSceneNetworkObjectDestroyed(NetworkObject networkObject)
        {
            m_ServerSpawnedDestroyWithSceneInstances.Remove(networkObject);
        }

        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            var authority = GetAuthorityNetworkManager();
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        if (sceneEvent.ClientId == authority.LocalClientId)
                        {
                            m_ScenesLoaded.Add(sceneEvent.Scene);
                        }
                        return;
                    }
            }
        }

        protected override IEnumerator OnTearDown()
        {
            m_TestPrefab = null;
            m_TestPrefabAutoSynchActiveScene = null;
            m_TestPrefabDestroyWithScene = null;
            SceneManager.SetActiveScene(m_OriginalActiveScene);
            m_ServerSpawnedDestroyWithSceneInstances.Clear();
            m_ServerSpawnedPrefabInstances.Clear();
            m_ScenesLoaded.Clear();
            yield return base.OnTearDown();
        }

        private NetworkObject GetAuthorityInstance(NetworkObject instance)
        {
            if (instance.IsOwner)
            {
                return instance;
            }

            var owner = instance.OwnerClientId;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.LocalClientId == owner)
                {
                    networkManager.SpawnManager.SpawnedObjects.TryGetValue(instance.NetworkObjectId, out var networkObject);
                    return networkObject;
                }
            }

            return null;
        }
    }

    internal class SceneOriginTracker : NetworkBehaviour
    {
        public NetworkSceneHandle SceneWhereAwakeHappened;
        private void Awake()
        {
            SceneWhereAwakeHappened = gameObject.scene.handle;
        }
    }

    internal class ShouldNeverSpawn : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            Assert.Fail("Should never spawn!");
        }
    }

    /// <summary>
    /// Helper NetworkBehaviour Component
    /// For test: <see cref="NetworkObjectSceneMigrationTests.ActiveSceneSynchronizationTest"/>
    /// </summary>
    internal class DestroyWithSceneInstancesTestHelper : NetworkBehaviour
    {
        public static ShouldNeverSpawn ShouldNeverSpawn;

        public static Dictionary<ulong, Dictionary<ulong, NetworkObject>> ObjectRelativeInstances = new Dictionary<ulong, Dictionary<ulong, NetworkObject>>();

        public static Action<NetworkObject> NetworkObjectDestroyed;

        /// <summary>
        /// Called when destroyed
        /// Passes the client ID and the NetworkObject instance
        /// </summary>
        public Action<ulong, NetworkObject> ObjectDestroyed;

        public override void OnNetworkSpawn()
        {
            if (!ObjectRelativeInstances.ContainsKey(NetworkManager.LocalClientId))
            {
                ObjectRelativeInstances.Add(NetworkManager.LocalClientId, new Dictionary<ulong, NetworkObject>());
            }

            ObjectRelativeInstances[NetworkManager.LocalClientId].Add(NetworkObjectId, NetworkObject);
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            ObjectRelativeInstances[NetworkManager.LocalClientId].Remove(NetworkObjectId);
            if (ObjectRelativeInstances[NetworkManager.LocalClientId].Count == 0)
            {
                ObjectRelativeInstances.Remove(NetworkManager.LocalClientId);
            }
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (IsSpawned && HasAuthority)
            {
                NetworkObjectDestroyed?.Invoke(NetworkObject);
            }
            base.OnDestroy();
        }
    }

}
