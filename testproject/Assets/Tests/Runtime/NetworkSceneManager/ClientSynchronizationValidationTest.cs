using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;

namespace TestProject.RuntimeTests
{
    public class ClientSynchronizationValidationTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private const string k_FirstSceneToLoad = "UnitTestBaseScene";
        private const string k_SecondSceneToSkip = "InSceneNetworkObject";
        private const string k_ThirdSceneToLoad = "EmptyScene";
        private bool m_CanStartServerAndClients;
        private List<ClientSceneVerificationHandler> m_ClientSceneVerifiers = new List<ClientSceneVerificationHandler>();

        protected override bool CanStartServerAndClients()
        {
            return m_CanStartServerAndClients;
        }

        protected override IEnumerator OnStartedServerAndClients()
        {
            // Create ClientSceneVerificationHandlers for each client
            foreach (var client in m_ClientNetworkManagers)
            {
                m_ClientSceneVerifiers.Add(new ClientSceneVerificationHandler(client));
            }
            return base.OnStartedServerAndClients();
        }

        [UnityTest]
        public IEnumerator ClientVerifySceneBeforeLoading()
        {
            // Because despawning a client will cause it to shutdown and clean everything in the
            // scene hierarchy, we have to prevent one of the clients from spawning initially before
            // we test synchronizing late joining clients.
            // So, we prevent the automatic starting of the server and clients, remove the client we
            // will be targeting to join late from the m_ClientNetworkManagers array, start the server
            // and the remaining client, despawn the in-scene NetworkObject, and then start and synchronize
            // the clientToTest.
            var clientToTest = m_ClientNetworkManagers[0];
            var clients = m_ClientNetworkManagers.ToList();
            clients.Remove(clientToTest);
            m_ClientNetworkManagers = clients.ToArray();
            m_CanStartServerAndClients = true;
            yield return StartServerAndClients();
            clients.Add(clientToTest);
            m_ClientNetworkManagers = clients.ToArray();

            var scenesToLoad = new List<string>() { k_FirstSceneToLoad, k_SecondSceneToSkip, k_ThirdSceneToLoad };
            m_ServerNetworkManager.SceneManager.OnLoadComplete += OnLoadComplete;
            foreach (var sceneToLoad in scenesToLoad)
            {
                m_SceneBeingLoadedIsLoaded = false;
                m_SceneBeingLoaded = sceneToLoad;
                m_ServerNetworkManager.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);

                yield return WaitForConditionOrTimeOut(() => m_SceneBeingLoadedIsLoaded);
                AssertOnTimeout($"Timed out waiting for scene {m_SceneBeingLoaded} to finish loading!");
            }

            // Now late join a client to make sure the client synchronizes to 2 of the 3 scenes loaded
            NetcodeIntegrationTestHelpers.StartOneClient(clientToTest);
            yield return WaitForConditionOrTimeOut(() => (clientToTest.IsConnectedClient && clientToTest.IsListening));
            AssertOnTimeout($"Timed out waiting for {clientToTest.name} to reconnect!");

            yield return s_DefaultWaitForTick;

            // Update the newly joined client information
            ClientNetworkManagerPostStartInit();

            // Check to make sure only the two scenes were loaded and one
            // completely skipped.
            foreach (var clientSceneVerifier in m_ClientSceneVerifiers)
            {
                clientSceneVerifier.ValidateScenesLoaded();
            }
        }

        private string m_SceneBeingLoaded;
        private bool m_SceneBeingLoadedIsLoaded;

        private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (m_SceneBeingLoaded == sceneName)
            {
                m_SceneBeingLoadedIsLoaded = true;
            }
        }


        /// <summary>
        /// Determines if all clients loaded only two of the 3 scenes
        /// </summary>
        private class ClientSceneVerificationHandler
        {
            private NetworkManager m_NetworkManager;
            private Dictionary<string, int> m_ValidSceneEventCount = new Dictionary<string, int>();
            public ClientSceneVerificationHandler(NetworkManager networkManager)
            {
                m_NetworkManager = networkManager;
                m_NetworkManager.SceneManager.DisableValidationWarnings(true);
                m_NetworkManager.SceneManager.VerifySceneBeforeLoading = VerifySceneBeforeLoading;
                m_NetworkManager.SceneManager.OnLoad += ClientSceneManager_OnLoad;
                m_NetworkManager.SceneManager.OnLoadComplete += ClientSceneManager_OnLoadComplete;
                m_ValidSceneEventCount.Add(k_FirstSceneToLoad, 0);
                m_ValidSceneEventCount.Add(k_SecondSceneToSkip, 0);
                m_ValidSceneEventCount.Add(k_ThirdSceneToLoad, 0);
            }

            public void ValidateScenesLoaded()
            {
                Assert.IsFalse(m_ValidSceneEventCount[k_SecondSceneToSkip] > 0, $"Client still loaded the invalidated scene {k_SecondSceneToSkip}!");
                Assert.IsTrue(m_ValidSceneEventCount[k_FirstSceneToLoad] == 1, $"Client did not load and process the validated scene {k_FirstSceneToLoad}! Expected (1) but was ({m_ValidSceneEventCount[k_FirstSceneToLoad]})");
                Assert.IsTrue(m_ValidSceneEventCount[k_ThirdSceneToLoad] == 1, $"Client did not load and process the validated scene {k_ThirdSceneToLoad}! Expected (1) but was ({m_ValidSceneEventCount[k_ThirdSceneToLoad]})");
            }

            private void ClientSceneManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
            {
                if (m_ValidSceneEventCount.ContainsKey(sceneName))
                {
                    m_ValidSceneEventCount[sceneName]++;
                }
            }

            private void ClientSceneManager_OnLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
            {
                if (m_ValidSceneEventCount.ContainsKey(sceneName))
                {
                    m_ValidSceneEventCount[sceneName]++;
                }
            }

            private bool VerifySceneBeforeLoading(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
            {
                if (sceneName == k_SecondSceneToSkip)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
