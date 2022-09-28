using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using Random = UnityEngine.Random;

namespace TestProject.RuntimeTests
{
    public class SceneEventProgressTests : NetcodeIntegrationTest
    {
        private const string k_SceneUsedToGetAsyncOperation = "EmptyScene";
        protected override int NumberOfClients => 4;

        private bool m_SceneEventProgressCompleted;
        private SceneEventProgress m_CurrentSceneEventProgress;

        private List<ulong> m_ClientThatShouldNotHaveCompleted = new List<ulong>();
        private List<ulong> m_ClientThatShouldHaveCompleted = new List<ulong>();

        private bool SceneEventProgressComplete(SceneEventProgress sceneEventProgress)
        {
            m_SceneEventProgressCompleted = true;
            return true;
        }

        private void SetClientFinished(ulong clientId, bool finished)
        {
            if (finished)
            {
                m_ClientThatShouldHaveCompleted.Add(clientId);
                m_CurrentSceneEventProgress.ClientFinishedSceneEvent(clientId);
            }
            else
            {
                m_ClientThatShouldNotHaveCompleted.Add(clientId);
            }
        }

        private void StartNewSceneEventProgress()
        {
            m_SceneEventProgressCompleted = false;
            m_ClientThatShouldNotHaveCompleted.Clear();
            m_ClientThatShouldHaveCompleted.Clear();
            m_CurrentSceneEventProgress = new SceneEventProgress(m_ServerNetworkManager, SceneEventProgressStatus.Started);
            m_CurrentSceneEventProgress.OnComplete = SceneEventProgressComplete;
            SceneManager.sceneLoaded += MockServerLoadedSene;
            m_CurrentSceneEventProgress.SetSceneLoadOperation(SceneManager.LoadSceneAsync(k_SceneUsedToGetAsyncOperation, LoadSceneMode.Additive));
        }

        private void MockServerLoadedSene(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == k_SceneUsedToGetAsyncOperation)
            {
                SetClientFinished(NetworkManager.ServerClientId, true);
                SceneManager.sceneLoaded -= MockServerLoadedSene;
            }
        }

        private void VerifyClientsThatCompleted()
        {
            var clientsDidComplete = m_CurrentSceneEventProgress.GetClientsWithStatus(true);
            var clientsDidNotComplete = m_CurrentSceneEventProgress.GetClientsWithStatus(false);

            foreach (var clientId in clientsDidComplete)
            {
                Assert.IsTrue(m_ClientThatShouldHaveCompleted.Contains(clientId), $"Client-{clientId} was not in the SceneEventProgress completed list!");
            }

            foreach (var clientId in clientsDidNotComplete)
            {
                Assert.IsTrue(m_ClientThatShouldNotHaveCompleted.Contains(clientId), $"Client-{clientId} was not in the SceneEventProgress did not complete list!");
            }
        }

        /// <summary>
        /// This verifies when all clients finish, the SceneEventProgress
        /// completes as well.
        /// </summary>
        [UnityTest]
        public IEnumerator AllClientsFinish()
        {
            StartNewSceneEventProgress();

            for (int i = 0; i < NumberOfClients; i++)
            {
                var currentClientNetworkManager = m_ClientNetworkManagers[i];
                SetClientFinished(currentClientNetworkManager.LocalClientId, true);

                // stagger when the clients mock finish loading by 1 network tick
                yield return s_DefaultWaitForTick;
            }

            yield return WaitForConditionOrTimeOut(() => m_SceneEventProgressCompleted);
            AssertOnTimeout($"Timed out waiting for SceneEventProgress to time out!");

            VerifyClientsThatCompleted();
        }

        /// <summary>
        /// This verifies that SceneEventProgress will still invoke the
        /// OnComplete delegate handler when it times out
        /// </summary>
        [UnityTest]
        public IEnumerator CompletesWhenTimedOut()
        {
            // Adjust the timeout for 2 seconds for this test
            m_ServerNetworkManager.NetworkConfig.LoadSceneTimeOut = 2;
            StartNewSceneEventProgress();

            for (int i = 0; i < NumberOfClients; i++)
            {
                var shouldFinish = Random.Range(0, 100);
                var currentClientNetworkManager = m_ClientNetworkManagers[i];
                // Only let two clients finish
                var clientFinished = shouldFinish >= 50 && m_ClientThatShouldNotHaveCompleted.Count < 2;
                SetClientFinished(currentClientNetworkManager.LocalClientId, clientFinished);
            }

            yield return WaitForConditionOrTimeOut(() => m_SceneEventProgressCompleted);
            AssertOnTimeout($"Timed out waiting for SceneEventProgress to time out!");

            VerifyClientsThatCompleted();
        }

        /// <summary>
        /// This verifies that SceneEventProgress will still complete
        /// even when some of the originally connected clients disconnect
        /// during a SceneEventProgress.
        /// </summary>
        [UnityTest]
        public IEnumerator ClientsDisconnectDuring()
        {
            StartNewSceneEventProgress();

            for (int i = 0; i < NumberOfClients; i++)
            {
                var shouldDisconnect = Random.Range(0, 100);
                var currentClientNetworkManager = m_ClientNetworkManagers[i];
                // Two clients will disconnect
                var clientFinished = shouldDisconnect >= 50 && m_ClientThatShouldNotHaveCompleted.Count < 2;
                SetClientFinished(currentClientNetworkManager.LocalClientId, clientFinished);

                if (!clientFinished)
                {
                    currentClientNetworkManager.Shutdown();
                }
                // wait anywhere from 100-500ms until processing next client
                var randomWaitPeriod = Random.Range(0.1f, 0.5f);
                yield return new WaitForSeconds(randomWaitPeriod);
            }
            yield return WaitForConditionOrTimeOut(() => m_SceneEventProgressCompleted);
            AssertOnTimeout($"Timed out waiting for SceneEventProgress to time out!");
            VerifyClientsThatCompleted();
        }

        /// <summary>
        /// This verifies that SceneEventProgress will still complete
        /// even when clients late join.
        /// </summary>
        [UnityTest]
        public IEnumerator ClientsLateJoinDuring()
        {
            StartNewSceneEventProgress();

            for (int i = 0; i < NumberOfClients; i++)
            {
                var shouldNewClientJoin = Random.Range(0, 100);
                var currentClientNetworkManager = m_ClientNetworkManagers[i];
                // Two clients will connect during a SceneEventProgress
                var joinNewClient = shouldNewClientJoin >= 50 && m_ClientThatShouldNotHaveCompleted.Count < 2;

                // All connected clients will finish their SceneEventProgress
                SetClientFinished(currentClientNetworkManager.LocalClientId, true);

                if (joinNewClient)
                {
                    yield return CreateAndStartNewClient();
                }
                // wait anywhere from 100-500ms until processing next client
                var randomWaitPeriod = Random.Range(0.1f, 0.5f);
                yield return new WaitForSeconds(randomWaitPeriod);
            }

            yield return WaitForConditionOrTimeOut(() => m_SceneEventProgressCompleted);
            AssertOnTimeout($"Timed out waiting for SceneEventProgress to finish!");

            VerifyClientsThatCompleted();
        }
    }
}
