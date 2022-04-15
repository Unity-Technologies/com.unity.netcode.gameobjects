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
        protected override int NumberOfClients => 9;
        public NetworkSceneManagerEventDataPoolTest(HostOrServer hostOrServer) : base(hostOrServer) { }

        private const string k_BaseUnitTestSceneName = "UnitTestBaseScene";
        private const string k_MultiInstanceTestScenename = "AdditiveSceneMultiInstance";
        private const string k_DifferentSceneToLoad = "EmptyScene";

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
            m_UnloadOnTearDown = true;
            m_CanStartServerOrClients = false;
            m_ClientsReceivedSynchronize.Clear();
            m_ShouldWaitList.Clear();
            m_ScenesLoaded.Clear();
            m_CreateServerFirst = false;
            return base.OnSetup();
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
                        Assert.AreEqual(sceneEvent.SceneName, m_CurrentSceneName, $"{sceneEvent.SceneName} differs from expected scene {m_CurrentSceneName} " +
                            $"during {nameof(SceneEventType)}.{sceneEvent.SceneEventType}");
                        Assert.IsTrue(ContainsClient(sceneEvent.ClientId), $"{nameof(ContainsClient)} failed for client-{sceneEvent.ClientId} during {nameof(SceneEventType)}.{sceneEvent.SceneEventType}");
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
            Assert.True(result == SceneEventProgressStatus.Started);

            // Wait for all clients to load the scene
            yield return WaitForConditionOrTimeOut(ConditionPassed);

            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for clients to load the scene!");
        }

        private IEnumerator UnloadScene(Scene scene)
        {
            // Unload the scene
            ResetWait();

            m_CurrentSceneName = scene.name;

            var result = m_ServerNetworkManager.SceneManager.UnloadScene(scene);
            Assert.True(result == SceneEventProgressStatus.Started);

            // Wait for all clients to unload the scene
            yield return WaitForConditionOrTimeOut(ConditionPassed);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for clients to unload the scene!");
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
        public IEnumerator SceneEventDataPoolSceneLoadingTest([Values(LoadSceneMode.Single, LoadSceneMode.Additive)] LoadSceneMode clientSynchronizationMode, [Values(1, 2, 4, 8, 16)] int numberOfScenesToLoad)
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

            SceneManager.SetActiveScene(m_CurrentScene);
            // Now load the scene(s)
            for (int i = 0; i < numberOfScenesToLoad; i++)
            {
                yield return LoadScene(k_MultiInstanceTestScenename);
            }

            yield return s_DefaultWaitForTick;

            yield return UnloadAllScenes();
        }

        private bool m_ReconnectUnloadedScene;
        private bool m_ReconnectLoadedNewScene;
        private int m_TestClientSceneLoadNotifications;
        private NetworkManager m_ClientToTest;
        private Dictionary<int, Scene> m_NetworkSceneTableState = new Dictionary<int, Scene>();

        /// <summary>
        /// Tests: <see cref="NetworkSceneManager.GetNetworkSceneTableState"/> and <see cref="NetworkSceneManager.SetNetworkSceneTableState(Dictionary{int, Scene}, bool)"/>
        /// This verifies that when the client saves the NetworkSceneTable state upon being disconnected and upon attempting to reconnect sets the
        /// NetworkSceneTable state (from the one saved when disconnected) that the client does not attempt to re-load the already loaded scenes.
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkSceneTableStateTest()
        {
            m_LoadSceneMode = LoadSceneMode.Additive;
            m_CanStartServerOrClients = true;
            yield return StartServerAndClients();

            yield return WaitForConditionOrTimeOut(() => m_ClientsReceivedSynchronize.Count == (m_ClientNetworkManagers.Length));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for all clients to receive synchronization event! Received: {m_ClientsReceivedSynchronize.Count} | Expected: {m_ClientNetworkManagers.Length}");

            // Now prepare for the loading and unloading additive scene testing
            InitializeSceneTestInfo(LoadSceneMode.Additive, true);

            m_OriginalActiveScene = SceneManager.GetActiveScene();

            yield return LoadScene(k_BaseUnitTestSceneName);

            SceneManager.SetActiveScene(m_CurrentScene);
            m_UnloadOnTearDown = false;
            var clientToTest = m_ClientToTest = m_ClientNetworkManagers[4];
            var originalClientId = clientToTest.LocalClientId;
            var numberOfScenesToLoad = 5;
            m_TestClientSceneLoadNotifications = 0;
            clientToTest.SceneManager.OnLoad += ClientToTestPreloadingOnLoadNotifications;
            // Now load the scene(s)
            for (int i = 0; i < numberOfScenesToLoad; i++)
            {
                yield return LoadScene(k_MultiInstanceTestScenename);
            }
            Assert.True(m_TestClientSceneLoadNotifications == numberOfScenesToLoad, $"Client to test only had {m_TestClientSceneLoadNotifications} load event notifications and expected {numberOfScenesToLoad}!");
            // We verified the client receives loading notifications, so we can unsubscribe from it now
            clientToTest.SceneManager.OnLoad -= ClientToTestPreloadingOnLoadNotifications;

            // We want to get the NetworkSceneTable state when the client-side receives the notification it has been disconnected.
            clientToTest.OnClientDisconnectCallback += ClientToTest_OnClientDisconnectCallback;
            // Remove this client from the m_ShouldWaitList
            m_ShouldWaitList.Remove(m_ShouldWaitList.Where((c) => c.ClientId == originalClientId).First());
            m_ServerNetworkManager.DisconnectClient(originalClientId);

            // Wait for the client to be fully disconnected
            yield return WaitForConditionOrTimeOut(() => (!clientToTest.IsConnectedClient && !clientToTest.IsListening && m_NetworkSceneTableState.Count() > 0));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {clientToTest.name} to disconnect!");

            // We can unsubscribe to this event now
            clientToTest.OnClientDisconnectCallback -= ClientToTest_OnClientDisconnectCallback;

            // Now unload one of the scenes before the client reconnects to change the NetworkSceneTable state
            var lastScene = m_ScenesLoaded.Last();
            m_ScenesLoaded.Remove(lastScene);
            yield return UnloadScene(lastScene);

            // Now,  change the NetworkSceneTable state one more time by loading a different scene to make sure
            // that the client does try to load this scene when it reconnects
            yield return LoadScene(k_DifferentSceneToLoad);

            // At this point we don't need to track scene events to determine when all scenes are loaded
            // and we subscribe to the clientToTest's NetworkSceneManager.OnLoad (disconnect/reconnect)
            // to make sure it does not fully process (i.e. synchronization scene loading when reconnecting)
            m_ServerNetworkManager.SceneManager.OnSceneEvent -= ServerSceneManager_OnSceneEvent;

            // Start reconnecting the disconnected client while also setting the
            NetcodeIntegrationTestHelpers.StartOneClient(clientToTest);
            clientToTest.SceneManager.VerifySceneBeforeLoading = m_ClientVerificationAction;
            clientToTest.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
            clientToTest.SceneManager.ClientSynchronizationMode = m_LoadSceneMode;
            clientToTest.SceneManager.DisableValidationWarnings(true);
            clientToTest.SceneManager.OnLoad += ClientToTestReconnectOnLoadNotifications;
            clientToTest.SceneManager.SetNetworkSceneTableState(m_NetworkSceneTableState);

            yield return WaitForConditionOrTimeOut(() => (clientToTest.IsConnectedClient && clientToTest.IsListening));
            m_ShouldWaitList.Add(new SceneTestInfo() { ClientId = clientToTest.LocalClientId, ShouldWait = false });
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {clientToTest.name} to connect!");
            clientToTest.SceneManager.OnLoad -= ClientToTestReconnectOnLoadNotifications;
            Assert.True(m_ReconnectLoadedNewScene, $"Client did not attempt to load the scene {k_DifferentSceneToLoad} when reconnecting!");
        }

        /// <summary>
        /// The disconnected callback handler is the recommended place for users to obtain the
        /// NetworkSceneTable state if they want to have disconnected clients reconnect without
        /// having to reload any currently loaded scenes.
        /// </summary>
        private void ClientToTest_OnClientDisconnectCallback(ulong obj)
        {
            // When disconnected, the client saves its relative network scene state (i.e. relative to the Network/Server)
            m_NetworkSceneTableState = m_ClientToTest.SceneManager.GetNetworkSceneTableState();
        }

        /// <summary>
        /// The client that is disconnected and reconnect is the only subscriber to this event.
        /// If it is invoked when the client is reconnecting then the test fails
        /// </summary>
        private void ClientToTestPreloadingOnLoadNotifications(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            if (m_ClientToTest.LocalClientId == clientId)
            {
                m_TestClientSceneLoadNotifications++;
            }
        }

        /// <summary>
        /// The client that is disconnected and reconnect is the only subscriber to this event.
        /// If it is invoked when the client is reconnecting then the test fails unless it is
        /// loading the scene that was loaded by the server after the client disconnected
        /// </summary>
        private void ClientToTestReconnectOnLoadNotifications(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            if (sceneName == k_DifferentSceneToLoad)
            {
                m_ReconnectLoadedNewScene = true;
            }
            else
            {
                Assert.Fail($"Client-{clientId} still processed the {SceneEventType.Load} event for scene {sceneName} when the network scene table state was set!\n");
            }
        }

        private IEnumerator UnloadAllScenes()
        {
            if (m_ScenesLoaded.Count > 0)
            {
                // Reverse how we unload the scenes
                m_ScenesLoaded.Reverse();

                // Now unload the scene(s)
                foreach (var scene in m_ScenesLoaded)
                {
                    yield return UnloadScene(scene);
                }

                SceneManager.SetActiveScene(m_OriginalActiveScene);

                m_ScenesLoaded.Clear();
            }
        }

        private bool m_UnloadOnTearDown = true;
        protected override IEnumerator OnTearDown()
        {
            if (m_UnloadOnTearDown)
            {
                yield return UnloadAllScenes();
            }
        }

        protected override IEnumerator OnPostTearDown()
        {
            if (!m_UnloadOnTearDown && m_ScenesLoaded.Count() > 0)
            {
                // Reverse how we unload the scenes
                m_ScenesLoaded.Reverse();
                var asyncOperations = new List<AsyncOperation>();
                foreach (var scene in m_ScenesLoaded)
                {
                    if (scene.name == k_BaseUnitTestSceneName || scene.name == k_MultiInstanceTestScenename || scene.name == k_DifferentSceneToLoad && scene != m_OriginalActiveScene)
                    {
                        Debug.Log($"Unloading scene {scene.name}");
                        asyncOperations.Add(SceneManager.UnloadSceneAsync(scene));
                    }
                }
                yield return WaitForConditionOrTimeOut(() => asyncOperations.Where((c) => c.isDone).Count() == asyncOperations.Count());
                SceneManager.SetActiveScene(m_OriginalActiveScene);
                Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out unloading scenes during tear down!");
            }
            yield return base.OnPostTearDown();
        }
    }
}
