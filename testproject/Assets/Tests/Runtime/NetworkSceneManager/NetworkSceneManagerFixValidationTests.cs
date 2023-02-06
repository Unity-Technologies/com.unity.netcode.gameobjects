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

            m_ServerNetworkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab() { Prefab = gameObject });

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab() { Prefab = gameObject });
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
            // Apply a 500ms latency on packets (primarily for ClientDisconnectsDuringSeneLoadingValidation)
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
                m_ServerNetworkManager.SceneManager.OnLoadEventCompleted -= SceneManager_OnLoadEventCompleted;
                m_SceneLoadEventCompleted = true;

                // Verify that the disconnected client is in the clients that timed out list
                Assert.IsTrue(clientsTimedOut.Contains(m_FirstClientId), $"Client-id({m_FirstClientId}) was not found in the clients that timed out list that has a count of ({clientsTimedOut.Count}) entries!");
            }
        }

        private bool m_SceneLoadEventCompleted;
        private bool m_ClientDisconnectedOnLoad;
        private ulong m_FirstClientId;

        [UnityTest]
        public IEnumerator ClientDisconnectsDuringSeneLoadingValidation()
        {
            m_CanStart = true;
            yield return StartServerAndClients();

            // Do some preparation for the client we will be disconnecting.
            m_FirstClientId = m_ClientNetworkManagers[0].LocalClientId;
            if (m_ClientNetworkManagers[0].SceneManager.VerifySceneBeforeLoading != null)
            {
                m_OriginalVerifyScene = m_ClientNetworkManagers[0].SceneManager.VerifySceneBeforeLoading;
            }
            m_ClientNetworkManagers[0].SceneManager.VerifySceneBeforeLoading = VerifySceneBeforeLoading;

            // We use this to verify that the loading scene event doesn't take NetworkConfig.LoadSceneTimeOut to complete
            var timeEventStarted = Time.realtimeSinceStartup;
            var disconnectedClientName = m_ClientNetworkManagers[0].name;

            // Start to load the scene
            m_ServerNetworkManager.SceneManager.LoadScene(k_SceneToLoad, LoadSceneMode.Additive);
            yield return WaitForConditionOrTimeOut(() => m_SceneLoadEventCompleted);
            AssertOnTimeout("Timed out waiting for the load scene event to be completed!");

            // Verify the client disconnected when it was just about to start the load event
            Assert.True(m_ClientDisconnectedOnLoad, $"{disconnectedClientName} did not disconnect!");

            // Verify it didn't take as long as the NetworkConfig.LoadSceneTimeOut period to complete the event
            var timeToComplete = Time.realtimeSinceStartup - timeEventStarted;
            Assert.True(timeToComplete < m_ServerNetworkManager.NetworkConfig.LoadSceneTimeOut, "Server scene loading event timed out!");

            // Verification that the disconnected client was in the timeout list is done in SceneManager_OnLoadEventCompleted
        }

        private NetworkSceneManager.VerifySceneBeforeLoadingDelegateHandler m_OriginalVerifyScene;

        /// <summary>
        /// The client that disconnects during the scene event will have this override that will:
        /// - Set m_ClientDisconnectedOnLoad
        /// - Stop/Disconnect the client
        /// - Return false (i.e. don't load this scene) as we are simulating the client disconnected
        /// right as the scene event started
        /// </summary>
        private bool VerifySceneBeforeLoading(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName == k_SceneToLoad)
            {
                m_ClientDisconnectedOnLoad = true;
                NetcodeIntegrationTestHelpers.StopOneClient(m_ClientNetworkManagers[0]);
                IntegrationTestSceneHandler.NetworkManagers.Remove(m_ClientNetworkManagers[0]);
                return false;
            }
            if (m_OriginalVerifyScene != null)
            {
                return m_OriginalVerifyScene.Invoke(sceneIndex, sceneName, loadSceneMode);
            }
            return true;
        }

        protected override IEnumerator OnTearDown()
        {
            m_CanStart = false;
            return base.OnTearDown();
        }
    }
}
