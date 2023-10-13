using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectSpawnManyObjectsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        // "many" in this case means enough to exceed a ushort_max message size written in the header
        // 1500 is not a magic number except that it's big enough to trigger a failure
        private const int k_SpawnedObjects = 1500;

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

        [UnityTest]
        // When this test fails it does so without an exception and will wait the default ~6 minutes
        [Timeout(10000)]
        public IEnumerator WhenManyObjectsAreSpawnedAtOnce_AllAreReceived()
        {
            for (int x = 0; x < k_SpawnedObjects; x++)
            {
                NetworkObject serverObject = Object.Instantiate(m_PrefabToSpawn.Prefab).GetComponent<NetworkObject>();
                serverObject.NetworkManagerOwner = m_ServerNetworkManager;
                serverObject.Spawn();
            }
            // ensure all objects are replicated
            yield return WaitForConditionOrTimeOut(() => SpawnObjecTrackingComponent.SpawnedObjects == k_SpawnedObjects);
            AssertOnTimeout($"Timed out waiting for the client to spawn {k_SpawnedObjects} objects!");
        }


        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.Prefabs.Add(m_PrefabToSpawn);

            base.OnNewClientCreated(networkManager);
        }

        private const int k_SpawnObjectCount = 300;
        private const string k_FirstSceneToLoad = "EmptyScene";

        [UnityTest]
        public IEnumerator WhenManyObjectsAreSpawnedDuringSynchronization_AllAreReceived()
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
            Assert.IsTrue(m_ClientNetworkManagers[0].SceneManager.DeferredObjectCreationCount > 0, "Did not defer CreateObjectMessage during spawn while synchronizing test!");
        }

        private const int k_ScenesToLoad = 3;
        private int m_ScenesLoaded;
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
