using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class NetworkSceneManagerEventNotifications : NetcodeIntegrationTest
    {
        private const string k_InvalidSceneName = "SomeInvalidSceneName";
        private const string k_SceneToLoad = "EmptyScene";
        private const string k_BaseUnitTestSceneName = "UnitTestBaseScene";
        private const string k_InSceneNetworkObject = "InSceneNetworkObject";
        protected override int NumberOfClients => 4;
        private string m_CurrentSceneName;
        private List<string> m_ScenesLoaded = new List<string>();
        private Scene m_CurrentScene;
        private LoadSceneMode m_LoadSceneMode;
        private bool m_CanStartServerOrClients = false;
        private bool m_LoadEventCompleted = false;
        private NetworkManager m_ClientToTestLoading;

        internal class SceneTestInfo
        {
            public bool ShouldWait;
            public bool ProcessedEvent;
            public ulong ClientId;
        }

        private List<SceneTestInfo> m_ShouldWaitList = new List<SceneTestInfo>();
        private List<ulong> m_ServerReceivedSynchronize = new List<ulong>();
        private List<ulong> m_ClientsReceivedSynchronize = new List<ulong>();

        public NetworkSceneManagerEventNotifications(HostOrServer hostOrServer) : base(hostOrServer) { }

        private Scene m_OriginalActiveScene;

        protected override void OnOneTimeSetup()
        {
            m_OriginalActiveScene = SceneManager.GetActiveScene();
            base.OnOneTimeSetup();
        }

        protected override bool OnSetVerboseDebug()
        {
            return false;
        }

        protected override IEnumerator OnSetup()
        {
            m_ScenesLoaded.Clear();
            m_CanStartServerOrClients = false;
            m_ServerReceivedSynchronize.Clear();
            m_ClientsReceivedSynchronize.Clear();
            m_ShouldWaitList.Clear();
            return base.OnSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            if (m_OriginalActiveScene.IsValid() && m_OriginalActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(m_OriginalActiveScene);
            }
            return base.OnTearDown();
        }

        protected override void OnServerAndClientsCreated()
        {
            var authority = GetAuthorityNetworkManager();

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnStartedServerAndClients()
        {
            foreach (var manager in m_NetworkManagers)
            {
                if (manager.IsServer || manager.LocalClient.IsSessionOwner)
                {
                    manager.SceneManager.OnSceneEvent += ServerSceneManager_OnSceneEvent;
                    continue;
                }

                manager.SceneManager.ClientSynchronizationMode = m_LoadSceneMode;
                manager.SceneManager.OnSceneEvent += ClientSceneManager_OnSceneEvent;
            }
            return base.OnStartedServerAndClients();
        }

        private void ClientSceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                // Validates that we sent the proper number of synchronize events to the clients
                case SceneEventType.Synchronize:
                    {
                        m_ClientsReceivedSynchronize.Add(sceneEvent.ClientId);
                        break;
                    }
                // Validate that the clients finish synchronization and they used the proper synchronization mode
                case SceneEventType.SynchronizeComplete:
                    {
                        var authority = GetAuthorityNetworkManager();
                        var matchedClient = m_ClientNetworkManagers.FirstOrDefault(c => c.LocalClientId == sceneEvent.ClientId);
                        Assert.That(matchedClient, Is.Not.Null, $"Found no client {nameof(NetworkManager)}s that had a {nameof(NetworkManager.LocalClientId)} of {sceneEvent.ClientId}");
                        Assert.AreEqual(matchedClient.SceneManager.ClientSynchronizationMode, authority.SceneManager.ClientSynchronizationMode);
                        break;
                    }
            }
        }

        private void ServerSceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            var authority = GetAuthorityNetworkManager();
            VerboseLog($"[SceneEvent] ClientId:{sceneEvent.ClientId} | EventType: {sceneEvent.SceneEventType}");
            switch (sceneEvent.SceneEventType)
            {
                // Validates that we sent the proper number of synchronize events to the clients
                case SceneEventType.Synchronize:
                    {
                        m_ServerReceivedSynchronize.Add(sceneEvent.ClientId);
                        break;
                    }
                case SceneEventType.Load:
                case SceneEventType.Unload:
                    {
                        if (sceneEvent.SceneEventType == SceneEventType.Load)
                        {
                            Assert.AreEqual(sceneEvent.SceneName, m_CurrentSceneName);
                        }

                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId));
                        break;
                    }
                case SceneEventType.LoadComplete:
                    {
                        if (sceneEvent.ClientId == authority.LocalClientId)
                        {
                            var scene = sceneEvent.Scene;
                            m_CurrentScene = scene;
                        }
                        if (sceneEvent.ClientId == m_ClientToTestLoading.LocalClientId)
                        {
                            if (!m_ScenesLoaded.Contains(sceneEvent.SceneName))
                            {
                                VerboseLog($"Loaded {sceneEvent.SceneName}");
                                m_ScenesLoaded.Add(sceneEvent.SceneName);
                            }
                        }
                        Assert.AreEqual(sceneEvent.SceneName, m_CurrentSceneName);
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId));
                        SetClientProcessedEvent(sceneEvent.ClientId);
                        break;
                    }
                case SceneEventType.UnloadComplete:
                    {
                        if (!m_ScenesLoaded.Contains(sceneEvent.SceneName))
                        {
                            Assert.AreEqual(sceneEvent.SceneName, m_CurrentSceneName);
                        }
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
                        if (!authority.IsHost && sceneEvent.ClientId == authority.LocalClientId &&
                            !sceneEvent.ClientsThatCompleted.Contains(authority.LocalClientId))
                        {
                            sceneEvent.ClientsThatCompleted.Add(authority.LocalClientId);
                        }
                        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
                        {
                            m_LoadEventCompleted = true;
                        }
                        Assert.IsTrue(ContainsAllClients(sceneEvent.ClientsThatCompleted));
                        SetClientWaitDone(sceneEvent.ClientsThatCompleted);
                        m_LoadEventCompleted = true;
                        break;
                    }
            }
        }
        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            VerboseLog($"[SceneEvent] ClientId:{sceneEvent.ClientId} | EventType: {sceneEvent.SceneEventType}");
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        if (sceneEvent.ClientId == m_ClientToTestLoading.LocalClientId)
                        {
                            if (!m_ScenesLoaded.Contains(sceneEvent.SceneName))
                            {
                                VerboseLog($"Loaded {sceneEvent.SceneName}");
                                m_ScenesLoaded.Add(sceneEvent.SceneName);
                            }
                        }
                        break;
                    }

                case SceneEventType.UnloadComplete:
                    {
                        if (sceneEvent.ClientId == m_ClientToTestLoading.LocalClientId)
                        {
                            if (m_ScenesLoaded.Contains(sceneEvent.SceneName))
                            {
                                VerboseLog($"Unloaded {sceneEvent.SceneName}");
                                // We check here for single mode because the final scene event
                                // will be SceneEventType.LoadEventCompleted  (easier to trap for it here)
                                m_ScenesLoaded.Remove(sceneEvent.SceneName);
                            }
                        }
                        break;
                    }

            }
        }

        protected override bool CanStartServerAndClients()
        {
            return m_CanStartServerOrClients;
        }

        /// <summary>
        /// Tests the different types of NetworkSceneManager notifications (including exceptions) generated
        /// Also tests invalid loading scenarios (i.e. client trying to load a scene)
        /// </summary>
        [UnityTest]
        public IEnumerator SceneLoadingAndNotifications([Values] LoadSceneMode loadSceneMode)
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();
            m_ClientToTestLoading = nonAuthority;

            m_LoadSceneMode = loadSceneMode;
            m_CurrentSceneName = k_SceneToLoad;
            m_CanStartServerOrClients = true;
            yield return StartServerAndClients();

            yield return WaitForConditionOrTimeOut(() => m_ServerReceivedSynchronize.Count == NumberOfClients);
            AssertOnTimeout($"Timed out waiting for the authority to receive synchronization events for all clients! Received: {m_ServerReceivedSynchronize.Count} | Expected: {NumberOfClients}");
            yield return WaitForConditionOrTimeOut(() => m_ClientsReceivedSynchronize.Count == NumberOfClients);
            AssertOnTimeout($"Timed out waiting for all clients to receive synchronization event! Received: {m_ClientsReceivedSynchronize.Count} | Expected: {NumberOfClients}");

            if (loadSceneMode == LoadSceneMode.Single)
            {
                m_ClientToTestLoading.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            }
            // Now prepare for the scene testing
            InitializeSceneTestInfo();

            // Test loading scenes and the associated event messaging and notification pipelines
            ResetWait();
            Assert.AreEqual(authority.SceneManager.LoadScene(m_CurrentSceneName, loadSceneMode), SceneEventProgressStatus.Started);
            // Check error status for trying to load during an already in progress scene event
            Assert.AreEqual(authority.SceneManager.LoadScene(m_CurrentSceneName, loadSceneMode), SceneEventProgressStatus.SceneEventInProgress);

            // Wait for all clients to load the scene
            yield return WaitForConditionOrTimeOut(ConditionPassed);
            AssertOnTimeout($"Timed out waiting for all clients to load {m_CurrentSceneName}!");

            // For single load mode (since we do this now) we need to set the first scene loaded as our active scene (i.e. we don't want unload the test runner scene),
            // then load the scene in single mode.
            // This will (for clients and server):
            // - Execute all of the LoadSceneMode.Single specific code paths
            // - All scenes loaded will still be loaded additively within the IntegrationTestSceneHandler
            if (loadSceneMode == LoadSceneMode.Single)
            {
                SceneManager.SetActiveScene(m_CurrentScene);

                m_CurrentSceneName = k_InSceneNetworkObject;
                ResetWait();
                Assert.AreEqual(authority.SceneManager.LoadScene(k_InSceneNetworkObject, LoadSceneMode.Additive), SceneEventProgressStatus.Started);

                // Wait for all clients to additively load this additional scene
                yield return WaitForConditionOrTimeOut(ConditionPassed);
                AssertOnTimeout($"Timed out waiting for all clients to switch to scene {m_CurrentSceneName}!");


                // Now single mode load a new scene (i.e. "scene switch")
                m_CurrentSceneName = k_BaseUnitTestSceneName;
                ResetWait();
                Assert.AreEqual(authority.SceneManager.LoadScene(m_CurrentSceneName, loadSceneMode), SceneEventProgressStatus.Started);
                // Wait for all clients to perform scene switch
                yield return WaitForConditionOrTimeOut(ConditionPassed);
                AssertOnTimeout($"Timed out waiting for all clients to switch to scene {m_CurrentSceneName}!");
                // Make sure the server scene is the active scene
                SceneManager.SetActiveScene(m_CurrentScene);

                var helper = new TimeoutHelper();
                yield return WaitForConditionOrTimeOut(() => !m_ScenesLoaded.Contains(k_SceneToLoad) && !m_ScenesLoaded.Contains(k_InSceneNetworkObject), helper);
                var additionalInfo = new StringBuilder();
                if (helper.TimedOut)
                {
                    additionalInfo.Append("Scenes currently loaded: [");
                    additionalInfo.AppendJoin(", ", m_ScenesLoaded);
                    additionalInfo.Append("]");
                }
                AssertOnTimeout($"{nameof(m_ScenesLoaded)} still contains some of the scenes that were expected to be unloaded!\n {additionalInfo}", helper);
            }

            // Test unloading additive scenes and the associated event messaging and notification pipelines
            ResetWait();
            Assert.AreEqual(authority.SceneManager.UnloadScene(m_CurrentScene), SceneEventProgressStatus.Started);

            yield return WaitForConditionOrTimeOut(ConditionPassed);
            AssertOnTimeout($"Timed out waiting for all clients to unload {m_CurrentSceneName}!");

            // Check error status for trying to unloading something not loaded
            ResetWait();
            Assert.AreEqual(authority.SceneManager.UnloadScene(m_CurrentScene), SceneEventProgressStatus.SceneNotLoaded);

            // Check error status for trying to load an invalid scene name
            LogAssert.Expect(LogType.Error, $"Scene '{k_InvalidSceneName}' couldn't be loaded because it has not been added to the build settings scenes in build list.");
            Assert.AreEqual(authority.SceneManager.LoadScene(k_InvalidSceneName, LoadSceneMode.Additive), SceneEventProgressStatus.InvalidSceneName);
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

            m_LoadEventCompleted = false;
        }

        /// <summary>
        /// Initializes the m_ShouldWaitList
        /// </summary>
        private void InitializeSceneTestInfo()
        {
            foreach (var manager in m_NetworkManagers)
            {
                m_ShouldWaitList.Add(new SceneTestInfo() { ClientId = manager.LocalClientId, ShouldWait = false });
            }
        }

        /// <summary>
        /// Wait until all clients have processed the event and the server has determined the event is completed
        /// Will bail if it takes too long via m_TimeOutMarker
        /// </summary>
        private bool ConditionPassed()
        {
            if (m_LoadSceneMode == LoadSceneMode.Single && !m_LoadEventCompleted)
            {
                return false;
            }

            foreach (var client in m_ShouldWaitList)
            {
                if (!client.ProcessedEvent || client.ShouldWait)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if the clientId is valid
        /// </summary>
        private bool ContainsClient(ulong clientId)
        {
            foreach (var client in m_ShouldWaitList)
            {
                if (client.ClientId == clientId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the specific clientId entry as having processed the current event
        /// </summary>
        private void SetClientProcessedEvent(ulong clientId)
        {
            foreach (var client in m_ShouldWaitList)
            {
                if (client.ClientId == clientId)
                {
                    client.ProcessedEvent = true;
                }
            }
        }

        /// <summary>
        /// Sets all known clients' ShouldWait value to false
        /// </summary>
        private void SetClientWaitDone(List<ulong> clients)
        {
            var lookup = clients.ToHashSet();
            foreach (var client in m_ShouldWaitList)
            {
                if (lookup.Contains(client.ClientId))
                {
                    client.ShouldWait = false;
                }
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

        private void VerboseLog(string message)
        {
            if (OnSetVerboseDebug())
            {
                Debug.Log(message);
            }
        }
    }
}
