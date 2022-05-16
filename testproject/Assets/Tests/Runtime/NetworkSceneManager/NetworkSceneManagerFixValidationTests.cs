using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.UTP;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Use this test group for validating NetworkSceneManager fixes.
    /// </summary>
    public class NetworkSceneManagerFixValidationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private bool m_CanStart;

        protected override bool CanStartServerAndClients()
        {
            return m_CanStart;
        }

        /// <summary>
        /// This validation test verifies that the NetworkSceneManager will not crash if
        /// the SpawnManager.SpawnedObjectsList contains destroyed and invalid NetworkObjects.
        /// </summary>
        [Test]
        public void DDOLPopulateWithNullNetworkObjectsValidation([Values] bool useHost)
        {
            var gameObject = new GameObject();
            var networkObject = gameObject.AddComponent<NetworkObject>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = gameObject });

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = gameObject });
            }

            // Start the host and clients
            if (!NetcodeIntegrationTestHelpers.Start(useHost, m_ServerNetworkManager, new NetworkManager[] { }))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // Spawn some NetworkObjects
            var spawnedNetworkObjects = new List<GameObject>();
            for (int i = 0; i < 10; i++)
            {
                var instance = Object.Instantiate(gameObject);
                var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                instanceNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
                instanceNetworkObject.Spawn();
                spawnedNetworkObjects.Add(instance);
            }

            // Add a bogus entry to the SpawnManager
            m_ServerNetworkManager.SpawnManager.SpawnedObjectsList.Add(null);

            // Verify moving all NetworkObjects into the DDOL when some might be invalid will not crash
            m_ServerNetworkManager.SceneManager.MoveObjectsToDontDestroyOnLoad();

            // Verify moving all NetworkObjects from DDOL back into the active scene will not crash even if some are invalid
            m_ServerNetworkManager.SceneManager.MoveObjectsFromDontDestroyOnLoadToScene(SceneManager.GetActiveScene());

            // Now remove the invalid object
            m_ServerNetworkManager.SpawnManager.SpawnedObjectsList.Remove(null);

            // As long as there are no exceptions this test passes
        }

        private const string k_SceneToLoad = "UnitTestBaseScene";

        protected override void OnCreatePlayerPrefab()
        {
            base.OnCreatePlayerPrefab();
        }

        protected override void OnServerAndClientsCreated()
        {
            var serverTransport = m_ServerNetworkManager.GetComponent<UnityTransport>();
            serverTransport.SetDebugSimulatorParameters(500, 0, 0);

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                var clientTransport = m_ServerNetworkManager.GetComponent<UnityTransport>();
                clientTransport.SetDebugSimulatorParameters(500, 0, 0);
            }

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_ServerNetworkManager.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
            return base.OnServerAndClientsConnected();
        }

        private void SceneManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (sceneName == k_SceneToLoad)
            {
                m_SceneLoadEventCompleted = true;
            }
        }

        private bool m_SceneLoadEventCompleted;
        private bool m_ClientDisconnectedOnLoad;
        private ulong m_FirstClientId;

        [UnityTest]
        public IEnumerator ClientDisconnectsDuringSeneLoadingValidation()
        {
            m_CanStart = true;
            m_ServerNetworkManager.OnClientDisconnectCallback += M_ServerNetworkManager_OnClientDisconnectCallback;
            yield return StartServerAndClients();
            m_FirstClientId = m_ClientNetworkManagers[0].LocalClientId;
            if (m_ClientNetworkManagers[0].SceneManager.VerifySceneBeforeLoading != null)
            {
                m_OriginalVerifyScene = m_ClientNetworkManagers[0].SceneManager.VerifySceneBeforeLoading;
            }
            m_ClientNetworkManagers[0].SceneManager.VerifySceneBeforeLoading = VerifySceneBeforeLoading;
            var timeEventStarted = Time.realtimeSinceStartup;
            var disconnectedClientName = m_ClientNetworkManagers[0].name;

            m_ServerNetworkManager.SceneManager.LoadScene(k_SceneToLoad, LoadSceneMode.Additive);

            yield return WaitForConditionOrTimeOut(() => m_SceneLoadEventCompleted);

            AssertOnTimeout("Timed out waiting for the load scene event to be completed!");
            Assert.True(m_ClientDisconnectedOnLoad, $"{disconnectedClientName} did not disconnect!");
            var timeToComplete = Time.realtimeSinceStartup - timeEventStarted;
            Assert.True(timeToComplete < m_ServerNetworkManager.NetworkConfig.LoadSceneTimeOut, "Server scene loading event timed out!");
        }

        private NetworkSceneManager.VerifySceneBeforeLoadingDelegateHandler m_OriginalVerifyScene;

        private bool VerifySceneBeforeLoading(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName == k_SceneToLoad)
            {
                m_ClientDisconnectedOnLoad = true;
                NetcodeIntegrationTestHelpers.StopOneClient(m_ClientNetworkManagers[0]);
                return false;
            }
            if (m_OriginalVerifyScene != null)
            {
                return m_OriginalVerifyScene.Invoke(sceneIndex, sceneName, loadSceneMode);
            }
            return true;
        }

        private void M_ServerNetworkManager_OnClientDisconnectCallback(ulong clientId)
        {
            if (clientId == m_FirstClientId)
            {
                IntegrationTestSceneHandler.NetworkManagers.Remove(m_ClientNetworkManagers[0]);
            }
        }

        protected override IEnumerator OnTearDown()
        {
            m_CanStart = false;
            return base.OnTearDown();
        }
    }
}
