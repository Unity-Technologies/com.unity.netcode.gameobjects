using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;

namespace TestProject.RuntimeTests
{
    public class NetworkObjectSceneMigrationTests : NetcodeIntegrationTest
    {
        private List<string> m_TestScenes = new List<string>() { "EmptyScene1", "EmptyScene2", "EmptyScene3" };
        protected override int NumberOfClients => 2;
        private GameObject m_TestPrefab;

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("TestObject");
            base.OnServerAndClientsCreated();
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            foreach (var networkPrfab in m_ServerNetworkManager.NetworkConfig.Prefabs.Prefabs)
            {
                networkManager.NetworkConfig.Prefabs.Add(networkPrfab);
            }
            base.OnNewClientCreated(networkManager);
        }

        private bool VerifyAllClientsSpawnedInstances()
        {
            foreach (var serverObject in m_ServerSpawnedPrefabInstances)
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
                }
            }
            return true;
        }

        private bool VerifySpawnedObjectsMigrated()
        {
            foreach (var serverObject in m_ServerSpawnedPrefabInstances)
            {
                foreach (var networkManager in m_ClientNetworkManagers)
                {
                    var clientNetworkObjects = s_GlobalNetworkObjects[networkManager.LocalClientId];
                    if (clientNetworkObjects[serverObject.NetworkObjectId].gameObject.scene.name != serverObject.gameObject.scene.name)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool m_ClientsLoadedScene;
        private Scene m_SceneLoaded;
        private List<NetworkObject> m_ServerSpawnedPrefabInstances = new List<NetworkObject>();
        private List<Scene> m_ScenesLoaded = new List<Scene>();
        private string m_CurrentSceneLoading;

        /// <summary>
        /// Integration test to verify that migrating NetworkObjects
        /// into different scenes (in the same frame) is synchronized
        /// with connected clients and synchronizes with late joining
        /// clients.
        /// </summary>
        [UnityTest]
        public IEnumerator MigrateIntoNewSceneTest()
        {
            // Spawn 10 NetworkObject instances
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
            // WIP
            yield return null;
        }

        public enum MigrateUnloadType
        {
            ActiveSynch,
            DestroyWithScene
        }

        /// <summary>
        /// Integration test to verify that unloading a scene that a NetworkObject
        /// with ActiveSceneSynchronization (true) and DestroyWithScene (false) will
        /// migrate the NetworkObject into the next active scene when their current
        /// one is unloaded.
        /// </summary>
        [UnityTest]
        public IEnumerator MigrateOnUnloadSceneTest([Values] MigrateUnloadType migrateUnloadType)
        {
            // WIP
            yield return null;
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
            }
        }
    }
}
