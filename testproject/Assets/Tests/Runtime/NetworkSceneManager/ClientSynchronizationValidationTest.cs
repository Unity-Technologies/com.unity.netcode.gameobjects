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
    public class ClientSynchronizationValidationTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private const string k_FirstSceneToLoad = "UnitTestBaseScene";
        private const string k_SecondSceneToSkip = "InSceneNetworkObject";
        private const string k_ThirdSceneToLoad = "EmptyScene";
        private List<ClientSceneVerificationHandler> m_ClientSceneVerifiers = new List<ClientSceneVerificationHandler>();

        protected override void OnOneTimeSetup()
        {
            // Pre-load some scenes (i.e. server will tell clients to synchronize to these scenes)
            SceneManager.LoadSceneAsync(k_FirstSceneToLoad, LoadSceneMode.Additive);
            SceneManager.LoadSceneAsync(k_SecondSceneToSkip, LoadSceneMode.Additive);
            SceneManager.LoadSceneAsync(k_ThirdSceneToLoad, LoadSceneMode.Additive);
            base.OnOneTimeSetup();
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
            // If we made it here it means that all clients finished synchronizing
            // Now check to make sure only the two scenes were loaded and one
            // completely skipped.
            foreach (var clientSceneVerifier in m_ClientSceneVerifiers)
            {
                clientSceneVerifier.ValidateScenesLoaded();
            }
            yield return null;
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
                Assert.IsTrue(m_ValidSceneEventCount[k_FirstSceneToLoad] == 2, $"Client did not load and process the validated scene {k_FirstSceneToLoad}!");
                Assert.IsTrue(m_ValidSceneEventCount[k_ThirdSceneToLoad] == 2, $"Client did not  load and process the validated scene {k_ThirdSceneToLoad}!");
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
