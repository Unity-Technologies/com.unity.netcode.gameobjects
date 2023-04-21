using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <see cref="MigrateIntoNewSceneTest"/>
    /// <see cref="ActiveSceneSynchronizationTest"/>
    /// </summary>
    public class NetworkObjectSceneMigrationTests : NetcodeIntegrationTest
    {
        private List<string> m_TestScenes = new List<string>() { "EmptyScene1", "EmptyScene2", "EmptyScene3" };
        protected override int NumberOfClients => 2;
        private GameObject m_TestPrefab;
        private GameObject m_TestPrefabAutoSynchActiveScene;
        private GameObject m_TestPrefabDestroyWithScene;
        private Scene m_OriginalActiveScene;

        private bool m_ClientsLoadedScene;
        private bool m_ClientsUnloadedScene;
        private Scene m_SceneLoaded;
        private List<NetworkObject> m_ServerSpawnedPrefabInstances = new List<NetworkObject>();
        private List<NetworkObject> m_ServerSpawnedDestroyWithSceneInstances = new List<NetworkObject>();
        private List<Scene> m_ScenesLoaded = new List<Scene>();
        private string m_CurrentSceneLoading;
        private string m_CurrentSceneUnloading;


        protected override IEnumerator OnSetup()
        {
            m_OriginalActiveScene = SceneManager.GetActiveScene();
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            // Synchronize Scene Changes (default) Test Network Prefab
            m_TestPrefab = CreateNetworkObjectPrefab("TestObject");

            // Auto Synchronize Active Scene Changes Test Network Prefab
            m_TestPrefabAutoSynchActiveScene = CreateNetworkObjectPrefab("ASASObject");
            m_TestPrefabAutoSynchActiveScene.GetComponent<NetworkObject>().ActiveSceneSynchronization = true;

            // Destroy With Scene Test Network Prefab
            m_TestPrefabDestroyWithScene = CreateNetworkObjectPrefab("DWSObject");
            m_TestPrefabDestroyWithScene.AddComponent<DestroyWithSceneInstancesTestHelper>();

            DestroyWithSceneInstancesTestHelper.ShouldNeverSpawn = m_TestPrefabDestroyWithScene;

            base.OnServerAndClientsCreated();
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            foreach (var networkPrfab in m_ServerNetworkManager.NetworkConfig.Prefabs.Prefabs)
            {
                if (networkPrfab.Prefab == null)
                {
                    continue;
                }
                networkManager.NetworkConfig.Prefabs.Add(networkPrfab);
            }
            base.OnNewClientCreated(networkManager);
        }

        private bool DidClientsSpawnInstance(NetworkObject serverObject, bool checkDestroyWithScene = false)
        {
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                if (!s_GlobalNetworkObjects.ContainsKey(networkManager.LocalClientId))
                {
                    return false;
                }
                var clientNetworkObjects = s_GlobalNetworkObjects[networkManager.LocalClientId];
                if (!clientNetworkObjects.ContainsKey(serverObject.NetworkObjectId))
                {
                    return false;
                }

                if (checkDestroyWithScene)
                {
                    if (serverObject.DestroyWithScene != clientNetworkObjects[serverObject.NetworkObjectId])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool VerifyAllClientsSpawnedInstances()
        {
            foreach (var serverObject in m_ServerSpawnedPrefabInstances)
            {
                if (!DidClientsSpawnInstance(serverObject))
                {
                    return false;
                }
            }

            foreach (var serverObject in m_ServerSpawnedDestroyWithSceneInstances)
            {
                if (!DidClientsSpawnInstance(serverObject, true))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreClientInstancesInTheRightScene(NetworkObject serverObject)
        {
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                var clientNetworkObjects = s_GlobalNetworkObjects[networkManager.LocalClientId];
                if (clientNetworkObjects == null)
                {
                    continue;
                }
                // If a networkObject is null then it was destroyed
                if (clientNetworkObjects[serverObject.NetworkObjectId].gameObject.scene.name != serverObject.gameObject.scene.name)
                {
                    return false;
                }
            }
            return true;
        }

        private bool VerifySpawnedObjectsMigrated()
        {
            foreach (var serverObject in m_ServerSpawnedPrefabInstances)
            {
                if (!AreClientInstancesInTheRightScene(serverObject))
                {
                    return false;
                }
            }

            foreach (var serverObject in m_ServerSpawnedDestroyWithSceneInstances)
            {
                if (!AreClientInstancesInTheRightScene(serverObject))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Integration test to verify that migrating NetworkObjects
        /// into different scenes (in the same frame) is synchronized
        /// with connected clients and synchronizes with late joining
        /// clients.
        /// </summary>
        [UnityTest]
        public IEnumerator MigrateIntoNewSceneTest()
        {
            // Spawn 9 NetworkObject instances
            for (int i = 0; i < 9; i++)
            {
                var serverInstance = Object.Instantiate(m_TestPrefab);
                var serverNetworkObject = serverInstance.GetComponent<NetworkObject>();
                serverNetworkObject.Spawn();
                m_ServerSpawnedPrefabInstances.Add(serverNetworkObject);
            }
            yield return WaitForConditionOrTimeOut(VerifyAllClientsSpawnedInstances);
            AssertOnTimeout($"Timed out waiting for all clients to spawn {nameof(NetworkObject)}s!");

            // Now load three scenes to migrate the newly spawned NetworkObjects into
            m_ServerNetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            for (int i = 0; i < 3; i++)
            {
                m_ClientsLoadedScene = false;
                m_CurrentSceneLoading = m_TestScenes[i];
                var status = m_ServerNetworkManager.SceneManager.LoadScene(m_TestScenes[i], LoadSceneMode.Additive);
                Assert.True(status == SceneEventProgressStatus.Started, $"Failed to start loading scene {m_CurrentSceneLoading}! Return status: {status}");
                yield return WaitForConditionOrTimeOut(() => m_ClientsLoadedScene);
                AssertOnTimeout($"Timed out waiting for all clients to load scene {m_CurrentSceneLoading}!");
            }

            var objectCount = 0;
            // Migrate each networkObject into one of the three scenes.
            // There will be 3 networkObjects per newly loaded scenes when done.
            foreach (var scene in m_ScenesLoaded)
            {
                // Now migrate the NetworkObject
                SceneManager.MoveGameObjectToScene(m_ServerSpawnedPrefabInstances[objectCount].gameObject, scene);
                SceneManager.MoveGameObjectToScene(m_ServerSpawnedPrefabInstances[objectCount + 1].gameObject, scene);
                SceneManager.MoveGameObjectToScene(m_ServerSpawnedPrefabInstances[objectCount + 2].gameObject, scene);
                objectCount += 3;
            }

            yield return WaitForConditionOrTimeOut(VerifySpawnedObjectsMigrated);
            AssertOnTimeout($"Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Verify that a late joining client synchronizes properly
            yield return CreateAndStartNewClient();
            yield return WaitForConditionOrTimeOut(VerifySpawnedObjectsMigrated);
            AssertOnTimeout($"[Late Joined Client] Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");
        }

        /// <summary>
        /// Integration test to verify changing the currently active scene
        /// will migrate NetworkObjects with ActiveSceneSynchronization set
        /// to true.
        /// </summary>
        [UnityTest]
        public IEnumerator ActiveSceneSynchronizationTest()
        {
            // Disable resynchronization for this test to avoid issues with trying
            // to synchronize them.
            NetworkSceneManager.DisableReSynchronization = true;

            // Spawn 3 NetworkObject instances that auto synchronize to active scene changes
            for (int i = 0; i < 3; i++)
            {
                var serverInstance = Object.Instantiate(m_TestPrefabAutoSynchActiveScene);
                var serverNetworkObject = serverInstance.GetComponent<NetworkObject>();
                // We are also testing that objects marked to synchronize with changes to
                // the active scene and marked to destroy with scene =are destroyed= if
                // the scene being unloaded is currently the active scene and the scene that
                // the NetworkObjects reside within.
                serverNetworkObject.Spawn(true);
                m_ServerSpawnedPrefabInstances.Add(serverNetworkObject);
            }

            // Spawn 3 NetworkObject instances that do not auto synchronize to active scene changes
            // and ==should not be== destroyed with the scene (these should be the only remaining
            // instances)
            for (int i = 0; i < 3; i++)
            {
                var serverInstance = Object.Instantiate(m_TestPrefab);
                var serverNetworkObject = serverInstance.GetComponent<NetworkObject>();
                // This set of NetworkObjects will be used to verify that NetworkObjets
                // spawned with DestroyWithScene set to false will migrate into the current
                // active scene if the scene they currently reside within is destroyed and
                // is not the currently active scene.
                serverNetworkObject.Spawn();
                m_ServerSpawnedPrefabInstances.Add(serverNetworkObject);
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

            yield return WaitForConditionOrTimeOut(VerifyAllClientsSpawnedInstances);
            AssertOnTimeout($"Timed out waiting for all clients to spawn {nameof(NetworkObject)}s!");

            // Now load three scenes
            m_ServerNetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            for (int i = 0; i < 3; i++)
            {
                m_ClientsLoadedScene = false;
                m_CurrentSceneLoading = m_TestScenes[i];
                var loadStatus = m_ServerNetworkManager.SceneManager.LoadScene(m_TestScenes[i], LoadSceneMode.Additive);
                Assert.True(loadStatus == SceneEventProgressStatus.Started, $"Failed to start loading scene {m_CurrentSceneLoading}! Return status: {loadStatus}");
                yield return WaitForConditionOrTimeOut(() => m_ClientsLoadedScene);
                AssertOnTimeout($"Timed out waiting for all clients to load scene {m_CurrentSceneLoading}!");
            }

            // Migrate the instances that don't synchronize with active scene changes into the 3rd loaded scene
            // (We are making sure these stay in the same scene they are migrated into)
            for (int i = 3; i < m_ServerSpawnedPrefabInstances.Count; i++)
            {
                SceneManager.MoveGameObjectToScene(m_ServerSpawnedPrefabInstances[i].gameObject, m_ScenesLoaded[2]);
            }

            // Migrate the instances that don't synchronize with active scene changes and are destroyed with the
            // scene unloading into the 3rd loaded scene
            // (We are making sure these get destroyed when the scene is unloaded)
            for (int i = 0; i < m_ServerSpawnedDestroyWithSceneInstances.Count; i++)
            {
                SceneManager.MoveGameObjectToScene(m_ServerSpawnedDestroyWithSceneInstances[i].gameObject, m_ScenesLoaded[2]);
            }

            // Make sure they migrated to the proper scene
            yield return WaitForConditionOrTimeOut(VerifySpawnedObjectsMigrated);
            AssertOnTimeout($"Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Now change the active scene
            SceneManager.SetActiveScene(m_ScenesLoaded[1]);
            // We have to do this
            Object.DontDestroyOnLoad(m_TestPrefabAutoSynchActiveScene);

            // First, make sure server-side scenes and client side scenes match
            yield return WaitForConditionOrTimeOut(VerifySpawnedObjectsMigrated);
            AssertOnTimeout($"Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Verify that the auto-active-scene synchronization NetworkObjects migrated to the newly
            // assigned active scene
            for (int i = 0; i < 3; i++)
            {
                Assert.True(m_ServerSpawnedPrefabInstances[i].gameObject.scene == m_ScenesLoaded[1],
                    $"{m_ServerSpawnedPrefabInstances[i].gameObject.name} did not migrate into scene {m_ScenesLoaded[1].name}!");
            }

            // Verify that the other NetworkObjects that don't synchronize with active scene changes did
            // not migrate into the active scene.
            for (int i = 3; i < m_ServerSpawnedPrefabInstances.Count; i++)
            {
                Assert.False(m_ServerSpawnedPrefabInstances[i].gameObject.scene == m_ScenesLoaded[1],
                    $"{m_ServerSpawnedPrefabInstances[i].gameObject.name} migrated into scene {m_ScenesLoaded[1].name}!");
            }

            for (int i = 0; i < 3; i++)
            {
                Assert.False(m_ServerSpawnedDestroyWithSceneInstances[i].gameObject.scene == m_ScenesLoaded[1],
                    $"{m_ServerSpawnedDestroyWithSceneInstances[i].gameObject.name} migrated into scene {m_ScenesLoaded[1].name}!");
            }

            // Verify that a late joining client synchronizes properly and destroys the appropriate NetworkObjects
            yield return CreateAndStartNewClient();
            yield return WaitForConditionOrTimeOut(VerifySpawnedObjectsMigrated);
            AssertOnTimeout($"[Late Joined Client #1] Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Now, unload the scene containing the NetworkObjects that don't synchronize with active scene changes
            DestroyWithSceneInstancesTestHelper.NetworkObjectDestroyed += OnNonActiveSynchDestroyWithSceneNetworkObjectDestroyed;
            m_ClientsUnloadedScene = false;
            m_CurrentSceneUnloading = m_ScenesLoaded[2].name;
            var status = m_ServerNetworkManager.SceneManager.UnloadScene(m_ScenesLoaded[2]);
            Assert.True(status == SceneEventProgressStatus.Started, $"Failed to start unloading scene {m_ScenesLoaded[2].name} with status {status}!");
            yield return WaitForConditionOrTimeOut(() => m_ClientsUnloadedScene);

            // Clean up any destroyed NetworkObjects
            for (int i = m_ServerSpawnedPrefabInstances.Count - 1; i >= 0; i--)
            {
                if (m_ServerSpawnedPrefabInstances[i] == null)
                {
                    m_ServerSpawnedPrefabInstances.RemoveAt(i);
                }
            }

            AssertOnTimeout($"Timed out waiting for all clients to unload scene {m_CurrentSceneUnloading}!");
            yield return WaitForConditionOrTimeOut(VerifySpawnedObjectsMigrated);
            AssertOnTimeout($"Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // Verify that the NetworkObjects that don't synchronize with active scene changes but marked to not
            // destroy with the scene are migrated into the current active scene
            for (int i = 3; i < m_ServerSpawnedPrefabInstances.Count; i++)
            {
                Assert.True(m_ServerSpawnedPrefabInstances[i].gameObject.scene == m_ScenesLoaded[1],
                    $"{m_ServerSpawnedPrefabInstances[i].gameObject.name} did not migrate into scene {m_ScenesLoaded[1].name} but are in scene {m_ServerSpawnedPrefabInstances[i].gameObject.scene.name}!");
            }

            // Verify all NetworkObjects that should have been destroyed with the scene unloaded were destroyed
            yield return WaitForConditionOrTimeOut(() => DestroyWithSceneInstancesTestHelper.ObjectRelativeInstances.Count == 0);
            DestroyWithSceneInstancesTestHelper.NetworkObjectDestroyed -= OnNonActiveSynchDestroyWithSceneNetworkObjectDestroyed;
            AssertOnTimeout($"Timed out waiting for all client instances marked to destroy when the scene unloaded to be despawned and destroyed.");

            // Now unload the active scene to verify all remaining NetworkObjects are migrated into the SceneManager
            // assigned active scene
            m_ClientsUnloadedScene = false;
            m_CurrentSceneUnloading = m_ScenesLoaded[1].name;
            m_ServerNetworkManager.SceneManager.UnloadScene(m_ScenesLoaded[1]);
            yield return WaitForConditionOrTimeOut(() => m_ClientsUnloadedScene);
            AssertOnTimeout($"Timed out waiting for all clients to unload scene {m_CurrentSceneUnloading}!");

            // Clean up any destroyed NetworkObjects
            for (int i = m_ServerSpawnedPrefabInstances.Count - 1; i >= 0; i--)
            {
                if (m_ServerSpawnedPrefabInstances[i] == null)
                {
                    m_ServerSpawnedPrefabInstances.RemoveAt(i);
                }
            }

            // Verify a late joining client will synchronize properly with the end result
            yield return CreateAndStartNewClient();

            // Verify the late joining client spawns all instances
            yield return WaitForConditionOrTimeOut(VerifyAllClientsSpawnedInstances);
            AssertOnTimeout($"Timed out waiting for all clients to spawn {nameof(NetworkObject)}s!");

            // Verify the instances are in the correct scenes
            yield return WaitForConditionOrTimeOut(VerifySpawnedObjectsMigrated);
            AssertOnTimeout($"[Late Joined Client #2] Timed out waiting for all clients to migrate all NetworkObjects into the appropriate scenes!");

            // All but 3 instances should be destroyed
            Assert.True(m_ServerSpawnedPrefabInstances.Count == 3, $"{nameof(m_ServerSpawnedPrefabInstances)} still has a count of {m_ServerSpawnedPrefabInstances.Count} " +
                $"NetworkObject instances!");
            Assert.True(m_ServerSpawnedDestroyWithSceneInstances.Count == 0, $"{nameof(m_ServerSpawnedDestroyWithSceneInstances)} still has a count of " +
                $"{m_ServerSpawnedDestroyWithSceneInstances.Count} NetworkObject instances!");
            for (int i = 0; i < 3; i++)
            {
                Assert.True(m_ServerSpawnedPrefabInstances[i].gameObject.name.Contains(m_TestPrefab.gameObject.name), $"Expected {m_ServerSpawnedPrefabInstances[i].gameObject.name} to contain {m_TestPrefab.gameObject.name}!");
            }
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
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        if (sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId)
                        {
                            m_SceneLoaded = sceneEvent.Scene;
                            m_ScenesLoaded.Add(sceneEvent.Scene);
                        }
                        break;
                    }
                case SceneEventType.LoadEventCompleted:
                    {
                        Assert.IsTrue(sceneEvent.ClientsThatTimedOut.Count == 0, $"{sceneEvent.ClientsThatTimedOut.Count} clients timed out while trying to load scene {m_CurrentSceneLoading}!");
                        m_ClientsLoadedScene = true;
                        break;
                    }
                case SceneEventType.UnloadEventCompleted:
                    {
                        if (sceneEvent.SceneName == m_CurrentSceneUnloading)
                        {
                            m_ClientsUnloadedScene = true;
                        }
                        break;
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
    }

    /// <summary>
    /// Helper NetworkBehaviour Component
    /// For test: <see cref="NetworkObjectSceneMigrationTests.ActiveSceneSynchronizationTest"/>
    /// </summary>
    internal class DestroyWithSceneInstancesTestHelper : NetworkBehaviour
    {
        public static GameObject ShouldNeverSpawn;

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
            if (NetworkManager != null)
            {
                if (NetworkManager.LocalClientId == NetworkManager.ServerClientId)
                {
                    NetworkObjectDestroyed?.Invoke(NetworkObject);
                }
            }
            base.OnDestroy();
        }
    }

}
