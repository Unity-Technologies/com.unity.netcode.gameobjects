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
    public class NetworkSceneManagerEventNotifications : NetcodeIntegrationTest
    {
        private const string k_InvalidSceneName = "SomeInvalidSceneName";
        private const string k_SceneToLoad = "EmptyScene";
        protected override int NumberOfClients => 4;
        private string m_CurrentSceneName;
        private Scene m_CurrentScene;
        private LoadSceneMode m_LoadSceneMode;
        private bool m_CanStartServerOrClients = false;

        internal class SceneTestInfo
        {
            public bool ShouldWait;
            public bool ProcessedEvent;
            public ulong ClientId;
        }

        private List<SceneTestInfo> m_ShouldWaitList = new List<SceneTestInfo>();
        private List<ulong> m_ClientsReceivedSynchronize = new List<ulong>();

        public NetworkSceneManagerEventNotifications(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override IEnumerator OnSetup()
        {
            m_CanStartServerOrClients = false;
            m_ClientsReceivedSynchronize.Clear();
            m_ShouldWaitList.Clear();
            return base.OnSetup();
        }

        protected override IEnumerator OnStartedServerAndClients()
        {
            m_ServerNetworkManager.SceneManager.OnSceneEvent += ServerSceneManager_OnSceneEvent;
            foreach (var client in m_ClientNetworkManagers)
            {
                client.SceneManager.ClientSynchronizationMode = m_LoadSceneMode;
                client.SceneManager.OnSceneEvent += ClientSceneManager_OnSceneEvent;
            }
            return base.OnStartedServerAndClients();
        }

        private void ClientSceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                // Validate that the clients finish synchronization and they used the proper synchronization mode
                case SceneEventType.SynchronizeComplete:
                    {
                        var matchedClient = m_ClientNetworkManagers.Where(c => c.LocalClientId == sceneEvent.ClientId);
                        Assert.True(matchedClient.Count() > 0, $"Found no client {nameof(NetworkManager)}s that had a {nameof(NetworkManager.LocalClientId)} of {sceneEvent.ClientId}");
                        Assert.AreEqual(matchedClient.First().SceneManager.ClientSynchronizationMode, m_LoadSceneMode);
                        break;
                    }
            }
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
                        }
                        Assert.AreEqual(sceneEvent.SceneName, m_CurrentSceneName);
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId));
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
        /// Tests the different types of NetworkSceneManager notifications (including exceptions) generated
        /// Also tests invalid loading scenarios (i.e. client trying to load a scene)
        /// </summary>
        [UnityTest]
        public IEnumerator SceneLoadingAndNotifications([Values] LoadSceneMode loadSceneMode)
        {
            m_LoadSceneMode = loadSceneMode;
            m_CurrentSceneName = k_SceneToLoad;
            m_CanStartServerOrClients = true;
            yield return StartServerAndClients();

            yield return WaitForConditionOrTimeOut(() => m_ClientsReceivedSynchronize.Count == (m_ClientNetworkManagers.Length));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive synchronization event! Received: {m_ClientsReceivedSynchronize.Count} | Expected: {m_ClientNetworkManagers.Length}");

            // Now prepare for the loading and unloading additive scene testing
            InitializeSceneTestInfo(loadSceneMode);

            // Test loading additive scenes and the associated event messaging and notification pipelines
            ResetWait();
            Assert.AreEqual(m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive), SceneEventProgressStatus.Started);
            // Check error status for trying to load during an already in progress scene event
            Assert.AreEqual(m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive), SceneEventProgressStatus.SceneEventInProgress);

            // Wait for all clients to load the scene
            yield return WaitForConditionOrTimeOut(ConditionPassed);
            AssertOnTimeout($"Timed out waiting for all clients to load {m_CurrentSceneName}!");

            // Test unloading additive scenes and the associated event messaging and notification pipelines
            ResetWait();
            Assert.AreEqual(m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene), SceneEventProgressStatus.Started);

            yield return WaitForConditionOrTimeOut(ConditionPassed);
            AssertOnTimeout($"Timed out waiting for all clients to unload {m_CurrentSceneName}!");

            // Check error status for trying to unloading something not loaded
            ResetWait();
            Assert.AreEqual(m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene), SceneEventProgressStatus.SceneNotLoaded);

            // Check error status for trying to load an invalid scene name
            LogAssert.Expect(LogType.Error, $"Scene '{k_InvalidSceneName}' couldn't be loaded because it has not been added to the build settings scenes in build list.");
            Assert.AreEqual(m_ServerNetworkManager.SceneManager.LoadScene(k_InvalidSceneName, LoadSceneMode.Additive), SceneEventProgressStatus.InvalidSceneName);
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

            foreach (var manager in m_ClientNetworkManagers)
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
    }
}
