using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class SpawnNetworkObjectsDuringSceneEventsTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private const int k_ScenesToLoad = 3;
        private const int k_SpawnObjectCount = 300;
        private const string k_FirstSceneToLoad = "EmptyScene";

        private int m_ScenesLoaded;
        private NetworkPrefab m_PrefabToSpawn;

        // Using this component assures we will know precisely how many prefabs were spawned on the client
        public class SpawnObjecTrackingComponent : NetworkBehaviour
        {
            public static int SpawnedObjects;
            public override void OnNetworkSpawn()
            {
                if (!IsServer)
                {
                    SpawnedObjects++;
                }
            }
        }

        protected override void OnServerAndClientsCreated()
        {
            SpawnObjecTrackingComponent.SpawnedObjects = 0;
            // create prefab
            var gameObject = new GameObject("TestObject");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);
            gameObject.AddComponent<SpawnObjecTrackingComponent>();

            m_PrefabToSpawn = new NetworkPrefab() { Prefab = gameObject };

            m_ServerNetworkManager.NetworkConfig.Prefabs.Add(m_PrefabToSpawn);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.Prefabs.Add(m_PrefabToSpawn);
            }
        }

        /// <summary>
        /// This test validates that object instantiation and spawning on the client
        /// side will be deferred until specific scene events have completed.
        /// </summary>
        /// <remarks>
        /// The conditions to defer instantiating and spawning are:
        ///   - Synchronizing while in client synchronization mode single --> Defer
        ///   - When not synchronizing but loading a scene in single mode --> Defer
        /// </remarks>
        [UnityTest]
        public IEnumerator SpawnNetworkObjectsDuringSceneEvents()
        {
            m_ScenesLoaded = 0;
            // Disconnect the client
            m_ClientNetworkManagers[0].Shutdown();
            // Let the disconnection complete
            yield return s_DefaultWaitForTick;

            // Load a few scenes to make client synchronization take a little while
            m_ServerNetworkManager.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
            m_ServerNetworkManager.SceneManager.LoadScene(k_FirstSceneToLoad, LoadSceneMode.Additive);

            // Wait unitl k_ScenesToLoad have been loaded 
            yield return WaitForConditionOrTimeOut(() => m_ScenesLoaded == k_ScenesToLoad);

            // Now, start the client again
            m_ClientNetworkManagers[0].StartClient();
            m_ClientNetworkManagers[0].SceneManager.DeferredObjectCreationCount = 0;

            // Wait one frame so connection packet makes it out
            yield return null;

            // Have the server start spawning a bunch of NetworkObjects
            for (int x = 0; x < k_SpawnObjectCount; x++)
            {
                NetworkObject serverObject = Object.Instantiate(m_PrefabToSpawn.Prefab).GetComponent<NetworkObject>();
                serverObject.NetworkManagerOwner = m_ServerNetworkManager;

                serverObject.Spawn();
                if (x % 5 == 0)
                {
                    yield return null;
                }
            }

            // Use increased time out helper
            var timeoutHelper = new TimeoutHelper(15);

            // ensure that client spawns the total number of objects expected
            yield return WaitForConditionOrTimeOut(() => SpawnObjecTrackingComponent.SpawnedObjects == k_SpawnObjectCount, timeoutHelper);
            AssertOnTimeout($"Timed out waiting for the client to spawn {k_SpawnObjectCount} objects! Client only spawned {SpawnObjecTrackingComponent.SpawnedObjects} objects so far.", timeoutHelper);

            // validate that some of those objects were deferred during the synchronization process
            Assert.IsTrue(m_ClientNetworkManagers[0].SceneManager.DeferredObjectCreationCount > 0, "Client did not defer CreateObjectMessage spawn handling while synchronizing!");

            // Decrement by one so we can load one more scene while the client is connected
            m_ServerNetworkManager.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;

            // For integration testing purposes, we have to set the LoadSceneMode to Additive since all scenes during integration testing
            // are loaded additively (thus the associated SceneEventData.LoadSceneMode will be LoadSceneMode.Additive even if we specify
            // LoadSceneMode.Single...it is still processed in NetworkSceneManager as a "single mode" load but is converted to additive later)
            m_ClientNetworkManagers[0].SceneManager.DeferLoadingFilter = LoadSceneMode.Additive;
            m_ScenesLoaded = 0;
            m_ClientNetworkManagers[0].SceneManager.DeferredObjectCreationCount = 0;

            // Begin scene loading event
            m_ServerNetworkManager.SceneManager.LoadScene($"{k_FirstSceneToLoad}{m_ScenesLoaded + 1}", LoadSceneMode.Additive);

            yield return null;

            var success = false;
            // Start spawning things
            for (int x = 0; x < k_SpawnObjectCount; x++)
            {
                NetworkObject serverObject = Object.Instantiate(m_PrefabToSpawn.Prefab).GetComponent<NetworkObject>();
                serverObject.NetworkManagerOwner = m_ServerNetworkManager;

                serverObject.Spawn();
                if (x % 5 == 0)
                {
                    yield return null;
                }
                // Go ahead and check to see if we deferred object creation 
                if (m_ClientNetworkManagers[0].SceneManager.DeferredObjectCreationCount > 0)
                {
                    // If so, we can stop spawning and finish out the test
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                yield return WaitForConditionOrTimeOut(() => m_ScenesLoaded == k_ScenesToLoad);
            }

            // validate that some of those objects were deferred while the client was loading a scene.
            Assert.IsTrue(m_ClientNetworkManagers[0].SceneManager.DeferredObjectCreationCount > 0, "Client did not defer CreateObjectMessage spawn handling while loading a scene!");
        }

        private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
        {
            if (sceneName == $"{k_FirstSceneToLoad}{m_ScenesLoaded + 1}")
            {
                if (m_ScenesLoaded < k_ScenesToLoad)
                {
                    m_ScenesLoaded++;
                    m_ServerNetworkManager.SceneManager.LoadScene($"{k_FirstSceneToLoad}{m_ScenesLoaded + 1}", LoadSceneMode.Additive);
                }
                else
                {
                    m_ServerNetworkManager.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
                }
            }
        }

        private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (clientId == m_ServerNetworkManager.LocalClientId && sceneName == k_FirstSceneToLoad)
            {
                if (m_ScenesLoaded < k_ScenesToLoad)
                {
                    m_ScenesLoaded++;
                    m_ServerNetworkManager.SceneManager.LoadScene(k_FirstSceneToLoad, LoadSceneMode.Additive);
                }
                else
                {
                    m_ServerNetworkManager.SceneManager.OnLoadComplete -= SceneManager_OnLoadComplete;
                }
            }
        }
    }
}
