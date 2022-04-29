using System;
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
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class NetworkSceneManagerEventDataPoolTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 4;
        public NetworkSceneManagerEventDataPoolTest(HostOrServer hostOrServer) : base(hostOrServer) { }

        private const string k_BaseUnitTestSceneName = "UnitTestBaseScene";
        private const string k_MultiInstanceTestScenename = "AdditiveSceneMultiInstance";

        private string m_CurrentSceneName;
        private Scene m_CurrentScene;
        private LoadSceneMode m_LoadSceneMode;
        private List<Scene> m_ScenesLoaded = new List<Scene>();
        private bool m_CanStartServerOrClients = false;
        private Scene m_OriginalActiveScene;

        private NetworkSceneManager.VerifySceneBeforeLoadingDelegateHandler m_ClientVerificationAction;
        private NetworkSceneManager.VerifySceneBeforeLoadingDelegateHandler m_ServerVerificationAction;

        internal class SceneTestInfo
        {
            public bool ShouldWait;
            public bool ProcessedEvent;
            public ulong ClientId;
        }

        private List<SceneTestInfo> m_ShouldWaitList = new List<SceneTestInfo>();
        private List<ulong> m_ClientsReceivedSynchronize = new List<ulong>();

        protected override IEnumerator OnSetup()
        {
            m_CanStartServerOrClients = false;
            m_ClientsReceivedSynchronize.Clear();
            m_ShouldWaitList.Clear();
            m_ScenesLoaded.Clear();
            m_CreateServerFirst = false;
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnStartedServerAndClients()
        {
            m_ClientVerificationAction = DataPoolVerifySceneClient;
            m_ServerVerificationAction = DataPoolVerifySceneServer;
            m_ServerNetworkManager.SceneManager.OnSceneEvent += ServerSceneManager_OnSceneEvent;
            m_ServerNetworkManager.SceneManager.DisableValidationWarnings(true);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.SceneManager.ClientSynchronizationMode = m_LoadSceneMode;
                client.SceneManager.DisableValidationWarnings(true);
            }
            return base.OnStartedServerAndClients();
        }

        private void ServerSceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                // Validates that we sent the proper number of synchronize events to the clients
                case SceneEventType.Synchronize:
                    {
                        m_ClientsReceivedSynchronize.Add(sceneEvent.ClientId);
                        break;
                    }
                case SceneEventType.Load:
                case SceneEventType.Unload:
                    {
                        Assert.AreEqual(sceneEvent.SceneName, m_CurrentSceneName);
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId));
                        Assert.IsNotNull(sceneEvent.AsyncOperation);
                        break;
                    }
                case SceneEventType.LoadComplete:
                    {
                        if (sceneEvent.ClientId == NetworkManager.ServerClientId)
                        {
                            var scene = sceneEvent.Scene;
                            m_CurrentScene = scene;
                            m_ScenesLoaded.Add(scene);
                        }
                        Assert.AreEqual(sceneEvent.SceneName, m_CurrentSceneName);
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId), $"[{m_CurrentSceneName}]Client ID {sceneEvent.ClientId} is not in {nameof(m_ShouldWaitList)}");
                        SetClientProcessedEvent(sceneEvent.ClientId);
                        break;
                    }
                case SceneEventType.UnloadComplete:
                    {
                        Assert.AreEqual(sceneEvent.SceneName, m_CurrentSceneName);
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId));
                        SetClientProcessedEvent(sceneEvent.ClientId);
                        break;
                    }
                case SceneEventType.LoadEventCompleted:
                case SceneEventType.UnloadEventCompleted:
                    {
                        Assert.AreEqual(sceneEvent.SceneName, m_CurrentSceneName);
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId));

                        // If we are a server and this is being processed by the server, then add the server to the completed list
                        // to validate that the event completed on all clients (and the server).
                        if (!m_ServerNetworkManager.IsHost && sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId &&
                            !sceneEvent.ClientsThatCompleted.Contains(m_ServerNetworkManager.LocalClientId))
                        {
                            sceneEvent.ClientsThatCompleted.Add(m_ServerNetworkManager.LocalClientId);
                        }
                        Assert.IsTrue(ContainsAllClients(sceneEvent.ClientsThatCompleted));
                        SetClientWaitDone(sceneEvent.ClientsThatCompleted);
                        break;
                    }
            }
        }

        protected override bool CanStartServerAndClients()
        {
            return m_CanStartServerOrClients;
        }

        /// <summary>
        /// Resets each SceneTestInfo entry
        /// </summary>
        private void ResetWait()
        {
            foreach (var entry in m_ShouldWaitList)
            {
                entry.ShouldWait = true;
                entry.ProcessedEvent = false;
            }
        }

        /// <summary>
        /// Initializes the m_ShouldWaitList
        /// </summary>
        private void InitializeSceneTestInfo(LoadSceneMode clientSynchronizationMode, bool enableSceneVerification = false)
        {
            m_ShouldWaitList.Add(new SceneTestInfo() { ClientId = NetworkManager.ServerClientId, ShouldWait = false });
            m_ServerNetworkManager.SceneManager.VerifySceneBeforeLoading = m_ServerVerificationAction;
            m_ServerNetworkManager.SceneManager.SetClientSynchronizationMode(clientSynchronizationMode);
            m_ScenesLoaded.Clear();
            foreach (var manager in m_ClientNetworkManagers)
            {
                m_ShouldWaitList.Add(new SceneTestInfo() { ClientId = manager.LocalClientId, ShouldWait = false });
                manager.SceneManager.VerifySceneBeforeLoading = m_ClientVerificationAction;
                manager.SceneManager.SetClientSynchronizationMode(clientSynchronizationMode);
            }
        }

        /// <summary>
        /// Wait until all clients have processed the event and the server has determined the event is completed
        /// Will bail if it takes too long via m_TimeOutMarker
        /// </summary>
        private bool ConditionPassed()
        {
            return !(m_ShouldWaitList.Select(c => c).Where(c => c.ProcessedEvent != true && c.ShouldWait == true).Count() > 0);
        }

        /// <summary>
        /// Determines if the clientId is valid
        /// </summary>
        private bool ContainsClient(ulong clientId)
        {
            return m_ShouldWaitList.Select(c => c.ClientId).Where(c => c == clientId).Count() > 0;
        }

        /// <summary>
        /// Sets the specific clientId entry as having processed the current event
        /// </summary>
        private void SetClientProcessedEvent(ulong clientId)
        {
            m_ShouldWaitList.Select(c => c).Where(c => c.ClientId == clientId).First().ProcessedEvent = true;
        }

        /// <summary>
        /// Sets all known clients' ShouldWait value to false
        /// </summary>
        private void SetClientWaitDone(List<ulong> clients)
        {
            foreach (var clientId in clients)
            {
                m_ShouldWaitList.Select(c => c).Where(c => c.ClientId == clientId).First().ShouldWait = false;
            }
        }

        /// <summary>
        /// Makes sure the ClientsThatCompleted in the scene event complete notification match the known client identifiers
        /// </summary>
        /// <param name="clients">list of client identifiers (ClientsThatCompleted) </param>
        /// <returns>true or false</returns>
        private bool ContainsAllClients(List<ulong> clients)
        {
            // First, make sure we have the expected client count
            if (clients.Count != m_ShouldWaitList.Count)
            {
                return false;
            }

            // Next, make sure we have all client identifiers
            foreach (var sceneTestInfo in m_ShouldWaitList)
            {
                if (!clients.Contains(sceneTestInfo.ClientId))
                {
                    return false;
                }
            }
            return true;
        }

        private IEnumerator LoadScene(string sceneName)
        {
            m_CurrentSceneName = sceneName;
            // Test VerifySceneBeforeLoading with both server and client set to true
            ResetWait();
            var result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.Started, $"[{result}] Status receives when trying to load scene {sceneName}!");

            // Wait for all clients to load the scene
            yield return WaitForConditionOrTimeOut(ConditionPassed);

            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for clients to load the scene!");

            yield return s_DefaultWaitForTick;
        }

        private IEnumerator UnloadScene(Scene scene)
        {
            // Unload the scene
            ResetWait();

            m_CurrentSceneName = scene.name;

            var result = m_ServerNetworkManager.SceneManager.UnloadScene(scene);
            Assert.True(result == SceneEventProgressStatus.Started, $"UnloadScene did not start and returned {result}");

            // Wait for all clients to unload the scene
            yield return WaitForConditionOrTimeOut(ConditionPassed);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for clients to unload the scene!");
            yield return s_DefaultWaitForTick;
        }

        /// <summary>
        /// Server will only allow the base unit test scene to load once in SceneEventDataPoolTest
        /// since clients share the same scene space.
        /// </summary>
        private bool DataPoolVerifySceneServer(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName == k_BaseUnitTestSceneName)
            {
                return !SceneManager.GetSceneByBuildIndex(sceneIndex).isLoaded;
            }
            return true;
        }

        /// <summary>
        /// Clients always load whatever the server tells them to load for SceneEventDataPoolTest
        /// </summary>
        private bool DataPoolVerifySceneClient(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            return true;
        }

        /// <summary>
        /// Small to heavy scene loading scenario to test the dynamically generated SceneEventData objects under a load.
        /// Will load from 1 to 32 scenes in both single and additive ClientSynchronizationMode
        /// </summary>
        [UnityTest]
        public IEnumerator SceneEventDataPoolSceneLoadingTest([Values(LoadSceneMode.Single, LoadSceneMode.Additive)] LoadSceneMode clientSynchronizationMode, [Values(1, 2, 4, 6)] int numberOfScenesToLoad)
        {
            m_LoadSceneMode = clientSynchronizationMode;
            m_CanStartServerOrClients = true;

            yield return StartServerAndClients();

            yield return WaitForConditionOrTimeOut(() => m_ClientsReceivedSynchronize.Count == (m_ClientNetworkManagers.Length));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive synchronization event! Received: {m_ClientsReceivedSynchronize.Count} | Expected: {m_ClientNetworkManagers.Length}");

            // Now prepare for the loading and unloading additive scene testing
            InitializeSceneTestInfo(clientSynchronizationMode, true);

            m_OriginalActiveScene = SceneManager.GetActiveScene();

            yield return LoadScene(k_BaseUnitTestSceneName);

            var message = "<<<Scene Handle Tables>>>\n";
            foreach (var client in m_ClientNetworkManagers)
            {
                message += $"{client.name}'s server to local scene handle table:\n";


                foreach (var entry in client.SceneManager.ServerSceneHandleToClientSceneHandle)
                {
                    message += $"NetworkSceneHandle: {entry.Key} | ClientSceneHandle: {entry.Value}\n";
                }
            }
            Debug.Log($"{message}");

            SceneManager.SetActiveScene(m_CurrentScene);
            // Now load the scene(s)
            for (int i = 0; i < numberOfScenesToLoad; i++)
            {
                yield return LoadScene(k_MultiInstanceTestScenename);
            }

            Assert.True(m_ScenesLoaded.Count == (1 + numberOfScenesToLoad), $"Scene number mismatch! Expected {(1 + numberOfScenesToLoad)} but was {m_ScenesLoaded.Count}");

            foreach (var client in m_ClientNetworkManagers)
            {
                client.SceneManager.OnUnloadComplete += SceneManager_OnUnloadComplete;
            }

            yield return UnloadAllScenes(true);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.SceneManager.OnUnloadComplete -= SceneManager_OnUnloadComplete;
            }
            m_ServerNetworkManager.SceneManager.OnSceneEvent -= ServerSceneManager_OnSceneEvent;

            yield return s_DefaultWaitForTick;
        }

        private string m_SceneBeingUnloaded;
        private List<ulong> m_ClientsThatUnloadedCurrentScene = new List<ulong>();

        private void SceneManager_OnUnloadComplete(ulong clientId, string sceneName)
        {
            if (m_SceneBeingUnloaded.Contains(sceneName))
            {
                if (!m_ClientsThatUnloadedCurrentScene.Contains(clientId))
                {
                    m_ClientsThatUnloadedCurrentScene.Add(clientId);
                }
                else
                {
                    throw new Exception($"m_ClientsThatUnloadedCurrentScene already contains client id {clientId} when unloading scene {sceneName}!");
                }
            }
        }

        private IEnumerator UnloadAllScenes(bool shouldCheckClients = false)
        {
            if (m_ScenesLoaded.Count > 0)
            {
                // Reverse how we unload the scenes
                m_ScenesLoaded.Reverse();

                // Now unload the scene(s)
                foreach (var scene in m_ScenesLoaded)
                {
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        m_ClientsThatUnloadedCurrentScene.Clear();
                        m_SceneBeingUnloaded = scene.name;
                        SceneManager.UnloadSceneAsync(scene);
                    }
                }
                SceneManager.SetActiveScene(m_OriginalActiveScene);

                m_ScenesLoaded.Clear();
            }
            yield return null;
        }

        protected override IEnumerator OnTearDown()
        {
            yield return UnloadAllScenes();
            m_ServerNetworkManager.SceneManager.OnSceneEvent -= ServerSceneManager_OnSceneEvent;
        }
    }
}
