using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class ClientSynchronizationValidationTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;
        private const string k_FirstSceneToLoad = "UnitTestBaseScene";
        private const string k_SecondSceneToLoad = "InSceneNetworkObject";
        private const string k_ThirdSceneToSkip = "EmptyScene";

        private List<ClientSceneVerificationHandler> m_ClientSceneVerifiers = new List<ClientSceneVerificationHandler>();

        protected override void OnNewClientStarted(NetworkManager networkManager)
        {
            m_ClientSceneVerifiers.Add(new ClientSceneVerificationHandler(networkManager));
            base.OnNewClientStarted(networkManager);
        }

        [UnityTest]
        public IEnumerator ClientVerifySceneBeforeLoading()
        {
            var scenesToLoad = new List<string>() { k_FirstSceneToLoad, k_SecondSceneToLoad, k_ThirdSceneToSkip };
            m_ServerNetworkManager.SceneManager.OnLoadComplete += OnLoadComplete;
            foreach (var sceneToLoad in scenesToLoad)
            {
                m_SceneBeingLoadedIsLoaded = false;
                m_SceneBeingLoaded = sceneToLoad;
                m_ServerNetworkManager.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);

                yield return WaitForConditionOrTimeOut(() => m_SceneBeingLoadedIsLoaded);
                AssertOnTimeout($"Timed out waiting for scene {m_SceneBeingLoaded} to finish loading!");
            }

            yield return CreateAndStartNewClient();

            yield return WaitForConditionOrTimeOut(m_ClientSceneVerifiers[0].HasLoadedExpectedScenes);
            AssertOnTimeout($"Timed out waiting for the client to have loaded the expected scenes");

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
                m_ValidSceneEventCount.Add(k_SecondSceneToLoad, 0);
                m_ValidSceneEventCount.Add(k_ThirdSceneToSkip, 0);
            }

            public bool HasLoadedExpectedScenes()
            {
                return m_ValidSceneEventCount[k_FirstSceneToLoad] == 2 && m_ValidSceneEventCount[k_SecondSceneToLoad] == 2;
            }

            public void ValidateScenesLoaded()
            {
                Assert.IsTrue(m_ValidSceneEventCount[k_ThirdSceneToSkip] == 0, $"Client still loaded the invalidated scene {k_ThirdSceneToSkip}!");
                Assert.IsTrue(m_ValidSceneEventCount[k_FirstSceneToLoad] == 2, $"Client did not load and process the validated scene {k_FirstSceneToLoad}! Expected (1) but was ({m_ValidSceneEventCount[k_FirstSceneToLoad]})");
                Assert.IsTrue(m_ValidSceneEventCount[k_SecondSceneToLoad] == 2, $"Client did not load and process the validated scene {k_SecondSceneToLoad}! Expected (1) but was ({m_ValidSceneEventCount[k_SecondSceneToLoad]})");
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
                if (sceneName == k_ThirdSceneToSkip)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
