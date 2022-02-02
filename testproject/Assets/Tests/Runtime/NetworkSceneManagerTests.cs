using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode.RuntimeTests;
using Unity.Netcode;
using Object = UnityEngine.Object;

namespace TestProject.RuntimeTests
{
    public class NetworkSceneManagerTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 9;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            m_ShouldWaitList = new List<SceneTestInfo>();
            return base.Setup();
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            m_BypassStartAndWaitForClients = false;
            return base.Teardown();
        }

        private class SceneTestInfo
        {
            public bool ShouldWait;
            public bool ProcessedEvent;
            public ulong ClientId;
        }

        private float m_TimeOutMarker;
        private bool m_TimedOut;
        private bool m_MultiSceneTest;
        private string m_CurrentSceneName;
        private List<SceneTestInfo> m_ShouldWaitList;
        private Scene m_CurrentScene;
        private const string k_InvalidSceneName = "SomeInvalidSceneName";
        private const string k_AdditiveScene1 = "AdditiveScene1";
        private const string k_AdditiveScene2 = "AdditiveScene1";

        private List<Scene> m_ScenesLoaded = new List<Scene>();


        private NetworkSceneManager.VerifySceneBeforeLoadingDelegateHandler m_ClientVerificationAction;
        private NetworkSceneManager.VerifySceneBeforeLoadingDelegateHandler m_ServerVerificationAction;

        public enum ServerType
        {
            Host,
            Server
        }

        /// <summary>
        /// Tests the different types of NetworkSceneManager notifications (including exceptions) generated
        /// Also tests invalid loading scenarios (i.e. client trying to load a scene)
        /// </summary>
        [UnityTest]
        public IEnumerator SceneLoadingAndNotifications([Values(LoadSceneMode.Single, LoadSceneMode.Additive)] LoadSceneMode clientSynchronizationMode, [Values(ServerType.Host, ServerType.Server)] ServerType serverType)
        {
            // First we disconnect and shutdown because we want to verify the synchronize events
            yield return Teardown();

            // Give a little time for handling clean-up and the like
            yield return new WaitForSeconds(0.01f);

            // We set this to true in order to bypass the automatic starting of the host and clients
            m_BypassStartAndWaitForClients = true;

            // Now just create the instances (server and client) without starting anything
            yield return Setup();

            // This provides both host and server coverage, when a server we should still get SceneEventType.LoadEventCompleted and SceneEventType.UnloadEventCompleted events
            // but the client count as a server should be 1 less than when a host
            var isHost = serverType == ServerType.Host ? true : false;

            // Start the host and  clients
            if (!MultiInstanceHelpers.Start(isHost, m_ServerNetworkManager, m_ClientNetworkManagers))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // Wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(m_ClientNetworkManagers));

            var numberOfClients = isHost ? NbClients + 1 : NbClients;
            // Wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(m_ServerNetworkManager, numberOfClients));

            m_ServerNetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            m_CurrentSceneName = k_AdditiveScene1;

            // Check that we cannot call LoadScene when EnableSceneManagement is false (from previous legacy test)
            var threwException = false;
            try
            {
                m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = false;
                m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains($"{nameof(NetworkConfig.EnableSceneManagement)} flag is not enabled in the {nameof(NetworkManager)}'s {nameof(NetworkConfig)}. " +
                    $"Please set {nameof(NetworkConfig.EnableSceneManagement)} flag to true before calling " +
                    $"{nameof(NetworkSceneManager.LoadScene)} or {nameof(NetworkSceneManager.UnloadScene)}."))
                {
                    threwException = true;
                }
            }
            Assert.IsTrue(threwException);

            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = true;

            // Check that a client cannot call LoadScene
            threwException = false;
            try
            {
                m_ClientNetworkManagers[0].SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Only server can start a scene event!"))
                {
                    threwException = true;
                }
            }
            Assert.IsTrue(threwException);


            // Now prepare for the loading and unloading additive scene testing
            InitializeSceneTestInfo(clientSynchronizationMode);

            // Test loading additive scenes and the associated event messaging and notification pipelines
            ResetWait();
            var result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.Started);

            // Check error status for trying to load during an already in progress scene event
            result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.SceneEventInProgress);

            // Wait for all clients to load the scene
            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

            // Test unloading additive scenes and the associated event messaging and notification pipelines
            ResetWait();

            // Check that a client cannot call UnloadScene
            threwException = false;
            try
            {
                m_ClientNetworkManagers[0].SceneManager.UnloadScene(m_CurrentScene);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Only server can start a scene event!"))
                {
                    threwException = true;
                }
            }
            Assert.IsTrue(threwException);

            result = m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene);
            Assert.True(result == SceneEventProgressStatus.Started);

            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

            // Check error status for trying to unloading something not loaded
            ResetWait();
            result = m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene);
            Assert.True(result == SceneEventProgressStatus.SceneNotLoaded);

            LogAssert.Expect(LogType.Error, $"Scene '{k_InvalidSceneName}' couldn't be loaded because it has not been added to the build settings scenes in build list.");
            // Check error status for trying to load an invalid scene name
            result = m_ServerNetworkManager.SceneManager.LoadScene(k_InvalidSceneName, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.InvalidSceneName);

            yield break;
        }

        /// <summary>
        /// Initializes the m_ShouldWaitList
        /// </summary>
        private void InitializeSceneTestInfo(LoadSceneMode clientSynchronizationMode, bool enableSceneVerification = false)
        {
            m_ShouldWaitList.Add(new SceneTestInfo() { ClientId = m_ServerNetworkManager.ServerClientId, ShouldWait = false });
            if (enableSceneVerification)
            {
                m_ServerNetworkManager.SceneManager.VerifySceneBeforeLoading = m_ServerVerificationAction;
                m_ServerNetworkManager.SceneManager.SetClientSynchronizationMode(clientSynchronizationMode);
                if (m_MultiSceneTest)
                {
                    m_ScenesLoaded.Clear();
                }
            }

            foreach (var manager in m_ClientNetworkManagers)
            {
                m_ShouldWaitList.Add(new SceneTestInfo() { ClientId = manager.LocalClientId, ShouldWait = false });
                if (enableSceneVerification)
                {
                    manager.SceneManager.VerifySceneBeforeLoading = m_ClientVerificationAction;
                    manager.SceneManager.SetClientSynchronizationMode(clientSynchronizationMode);
                }
            }
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

            m_TimeOutMarker = Time.realtimeSinceStartup + 4.0f;
            m_TimedOut = false;
        }

        /// <summary>
        /// Wait until all clients have processed the event and the server has determined the event is completed
        /// Will bail if it takes too long via m_TimeOutMarker
        /// </summary>
        private bool ShouldWait()
        {
            m_TimedOut = m_TimeOutMarker < Time.realtimeSinceStartup;
            if (!m_IsTestingVerifyScene)
            {
                return (m_ShouldWaitList.Select(c => c).Where(c => c.ProcessedEvent != true && c.ShouldWait == true).Count() > 0) && !m_TimedOut;
            }
            else
            {
                return (m_ShouldWaitList.Select(c => c).Where(c => c.ProcessedEvent != true && c.ShouldWait == true &&
                c.ClientId == m_ServerNetworkManager.ServerClientId).Count() > 0) && !m_TimedOut && m_ClientsThatFailedVerification != NbClients;
            }
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

        /// <summary>
        /// This test only needs to check the server side for the proper event notifications of loading a scene, each
        /// client response that it loaded the scene, and the final event notifications <see cref="SceneEventType.LoadEventCompleted"/>
        /// and <see cref="SceneEventType.UnloadEventCompleted"/> that signifies all clients have completed a loading or unloading event.
        /// </summary>
        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Synchronize:
                    {
                        // Verify that the Client Synchronization Mode set by the server is being received by the client (which means it is applied when loading the first scene)
                        Assert.AreEqual(m_ClientNetworkManagers.ToArray().Where(c => c.LocalClientId == sceneEvent.ClientId).First().SceneManager.ClientSynchronizationMode, sceneEvent.LoadSceneMode);
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
                        if (sceneEvent.ClientId == m_ServerNetworkManager.ServerClientId)
                        {
                            var sceneHandle = sceneEvent.Scene.handle;
                            var scene = sceneEvent.Scene;
                            m_CurrentScene = scene;
                            if (m_MultiSceneTest)
                            {
                                m_ScenesLoaded.Add(scene);
                            }
                            m_ClientsAreOkToLoad = true;
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

        private bool m_IsTestingVerifyScene;
        private bool m_ServerVerifyScene;
        private bool m_ClientVerifyScene;
        private int m_ExpectedSceneIndex;
        private int m_ClientsThatFailedVerification;
        private string m_ExpectedSceneName;
        private LoadSceneMode m_ExpectedLoadMode;

        private bool ServerVerifySceneBeforeLoading(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            Assert.IsTrue(m_ExpectedSceneIndex == sceneIndex);
            Assert.IsTrue(m_ExpectedSceneName == sceneName);
            Assert.IsTrue(m_ExpectedLoadMode == loadSceneMode);

            return m_ServerVerifyScene;
        }

        private bool ClientVerifySceneBeforeLoading(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            Assert.IsTrue(m_ExpectedSceneIndex == sceneIndex);
            Assert.IsTrue(m_ExpectedSceneName == sceneName);
            Assert.IsTrue(m_ExpectedLoadMode == loadSceneMode);
            if (!m_ClientVerifyScene)
            {
                m_ClientsThatFailedVerification++;
            }
            return m_ClientVerifyScene;
        }

        private bool m_ClientsAreOkToLoad = true;
        protected override bool CanClientsLoad()
        {
            return m_ClientsAreOkToLoad;
        }

        /// <summary>
        /// Unit test to verify that user defined scene verification process works on both the client and
        /// the server side.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator SceneVerifyBeforeLoadTest([Values(LoadSceneMode.Single, LoadSceneMode.Additive)] LoadSceneMode clientSynchronizationMode)
        {
            m_ClientVerificationAction = ClientVerifySceneBeforeLoading;
            m_ServerVerificationAction = ServerVerifySceneBeforeLoading;

            m_ServerNetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            m_CurrentSceneName = k_AdditiveScene1;

            // Now prepare for the loading and unloading additive scene testing
            InitializeSceneTestInfo(clientSynchronizationMode, true);

            // Test VerifySceneBeforeLoading with both server and client set to true
            ResetWait();
            m_ServerVerifyScene = m_ClientVerifyScene = true;
            m_ExpectedSceneIndex = SceneUtility.GetBuildIndexByScenePath(m_CurrentSceneName);
            m_ExpectedSceneName = m_CurrentSceneName;
            m_ExpectedLoadMode = LoadSceneMode.Additive;
            var result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.Started);

            // Wait for all clients to load the scene
            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

            // Unload the scene
            ResetWait();
            result = m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene);
            Assert.True(result == SceneEventProgressStatus.Started);

            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

            // Test VerifySceneBeforeLoading with m_ServerVerifyScene set to false
            // Server will notify it failed scene verification and no client should load
            ResetWait();
            m_ServerVerifyScene = false;
            result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.SceneFailedVerification);

            // Test VerifySceneBeforeLoading with m_ServerVerifyScene set to true and m_ClientVerifyScene set to false
            // Server should load and clients will notify they failed scene verification
            ResetWait();
            m_CurrentSceneName = k_AdditiveScene2;
            m_ExpectedSceneName = m_CurrentSceneName;
            m_ExpectedSceneIndex = SceneUtility.GetBuildIndexByScenePath(m_CurrentSceneName);
            m_ServerVerifyScene = true;
            m_ClientVerifyScene = false;
            m_IsTestingVerifyScene = true;
            m_ClientsThatFailedVerification = 0;
            m_ClientsAreOkToLoad = false;
            result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.Started);

            // Now wait for server to complete and all clients to fail
            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

            // Now unload the scene the server loaded from last test
            ResetWait();

            // All clients did not load this scene, so we can ignore them for the wait
            foreach (var listItem in m_ShouldWaitList)
            {
                if (listItem.ClientId == m_ServerNetworkManager.LocalClientId)
                {
                    continue;
                }
                listItem.ProcessedEvent = true;
            }

            m_IsTestingVerifyScene = false;

            result = m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene);
            Assert.True(result == SceneEventProgressStatus.Started);

            // Now wait for server to unload and clients will fake unload
            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);

            yield break;
        }

        private IEnumerator LoadScene(string sceneName)
        {
            // Test VerifySceneBeforeLoading with both server and client set to true
            ResetWait();
            m_ServerVerifyScene = m_ClientVerifyScene = true;
            m_ExpectedSceneIndex = SceneUtility.GetBuildIndexByScenePath(m_CurrentSceneName);
            m_ExpectedSceneName = m_CurrentSceneName;
            m_ExpectedLoadMode = LoadSceneMode.Additive;
            var result = m_ServerNetworkManager.SceneManager.LoadScene(m_CurrentSceneName, LoadSceneMode.Additive);
            Assert.True(result == SceneEventProgressStatus.Started);

            // Wait for all clients to load the scene
            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);
        }

        private IEnumerator UnloadScene(Scene scene)
        {
            // Unload the scene
            ResetWait();

            m_CurrentSceneName = scene.name;

            var result = m_ServerNetworkManager.SceneManager.UnloadScene(scene);
            Assert.True(result == SceneEventProgressStatus.Started);

            // Wait for all clients to unload the scene
            yield return new WaitWhile(ShouldWait);
            Assert.IsFalse(m_TimedOut);
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

        private const string k_BaseUnitTestSceneName = "UnitTestBaseScene";
        private const string k_MultiInstanceTestScenename = "AdditiveSceneMultiInstance";

        /// <summary>
        /// Small to heavy scene loading scenario to test the dynamically generated SceneEventData objects under a load.
        /// Will load from 1 to 32 scenes in both single and additive ClientSynchronizationMode
        /// </summary>
        [UnityTest]
        public IEnumerator SceneEventDataPoolSceneLoadingTest([Values(LoadSceneMode.Single, LoadSceneMode.Additive)] LoadSceneMode clientSynchronizationMode, [Values(1, 2, 4, 8, 16, 32)] int numberOfScenesToLoad)
        {
            m_MultiSceneTest = true;
            m_ClientVerificationAction = DataPoolVerifySceneClient;
            m_ServerVerificationAction = DataPoolVerifySceneServer;

            m_ServerNetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            m_ServerNetworkManager.SceneManager.DisableValidationWarnings(true);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.SceneManager.DisableValidationWarnings(true);
            }

            // Now prepare for the loading and unloading additive scene testing
            InitializeSceneTestInfo(clientSynchronizationMode, true);

            Scene currentlyActiveScene = SceneManager.GetActiveScene();

            // Now load the base scene
            m_CurrentSceneName = k_BaseUnitTestSceneName;
            yield return LoadScene(m_CurrentSceneName);

            var firstScene = m_CurrentScene;

            m_CurrentSceneName = k_MultiInstanceTestScenename;
            SceneManager.SetActiveScene(m_CurrentScene);
            // Now load the scene(s)
            for (int i = 0; i < numberOfScenesToLoad; i++)
            {
                yield return LoadScene(m_CurrentSceneName);
            }

            // Reverse how we unload the scenes
            m_ScenesLoaded.Reverse();

            // Now unload the scene(s)
            foreach (var scene in m_ScenesLoaded)
            {
                yield return UnloadScene(scene);
            }
            SceneManager.SetActiveScene(currentlyActiveScene);
            m_MultiSceneTest = false;
            yield break;
        }

        private class SceneEventNotificationTestInfo
        {
            public ulong ClientId;
            public bool ShouldWait;
            public List<SceneEventType> EventsProcessed;
        }

        private class SceneEventCompletedTestInfo
        {
            public string SceneName;
            public SceneEventType EventTypeCompleted;
            public List<ulong> ClientThatCompletedEvent;
        }

        private List<SceneEventNotificationTestInfo> m_ClientNotificationInfo = new List<SceneEventNotificationTestInfo>();
        private List<SceneEventNotificationTestInfo> m_ServerNotificationInfo = new List<SceneEventNotificationTestInfo>();

        private List<SceneEventCompletedTestInfo> m_ClientCompletedTestInfo = new List<SceneEventCompletedTestInfo>();
        private List<SceneEventCompletedTestInfo> m_ServerCompletedTestInfo = new List<SceneEventCompletedTestInfo>();

        private void ResetNotificationInfo()
        {
            m_ClientNotificationInfo.Clear();
            m_ServerNotificationInfo.Clear();
            m_ClientCompletedTestInfo.Clear();
            m_ServerCompletedTestInfo.Clear();
        }

        private void ClientProcessedNotification(ulong clientId, SceneEventType notificationType, bool isServer, bool eventComplete = false)
        {
            if (clientId == m_ServerNetworkManager.LocalClientId)
            {
                return;
            }
            var notificationList = isServer ? m_ServerNotificationInfo : m_ClientNotificationInfo;
            var clientSelection = notificationList.Select(c => c).Where(c => c.ClientId == clientId);
            if (clientSelection == null || clientSelection.Count() == 0)
            {
                notificationList.Add(new SceneEventNotificationTestInfo()
                {
                    ClientId = clientId,
                    EventsProcessed = new List<SceneEventType>(),
                });
            }
            var clientNotificationObject = notificationList.Select(c => c).Where(c => c.ClientId == clientId).First();
            clientNotificationObject.EventsProcessed.Add(notificationType);
            clientNotificationObject.ShouldWait = !eventComplete;
        }

        private void ProcessCompletedNotification(string sceneName, List<ulong> clientIds, SceneEventType notificationType, bool isServer)
        {
            var notificationList = isServer ? m_ServerCompletedTestInfo : m_ClientCompletedTestInfo;

            notificationList.Add(new SceneEventCompletedTestInfo()
            {
                SceneName = sceneName,
                EventTypeCompleted = notificationType,
                ClientThatCompletedEvent = new List<ulong>(clientIds)
            });
        }

        private bool ValidateCompletedNotifications()
        {
            var isValidated = m_ClientCompletedTestInfo.Count == NbClients && m_ServerCompletedTestInfo.Count == 1;
            if (isValidated)
            {
                foreach (var client in m_ClientCompletedTestInfo)
                {
                    Assert.That(m_ServerCompletedTestInfo[0].SceneName == client.SceneName);
                    Assert.That(m_ServerCompletedTestInfo[0].EventTypeCompleted == client.EventTypeCompleted);
                    Assert.That(m_ServerCompletedTestInfo[0].ClientThatCompletedEvent.Count == client.ClientThatCompletedEvent.Count);
                    foreach (var clientId in m_ServerCompletedTestInfo[0].ClientThatCompletedEvent)
                    {
                        Assert.That(client.ClientThatCompletedEvent.Contains(clientId));
                    }
                }
                Debug.Log($"{m_ServerCompletedTestInfo[0].EventTypeCompleted} validated!");
            }
            return isValidated;
        }

        private bool NotificationTestShouldWait()
        {
            // if all of our clients are not done we should wait.
            var shouldWait = m_ClientNotificationInfo.Select(c => c).Where(c => c.ShouldWait == false).Count() != m_ClientNotificationInfo.Count;

            // if our client count (server side vs client side) do not match yet we should wait
            shouldWait |= m_ServerNotificationInfo.Count() != m_ClientNotificationInfo.Count();

            // if our client count is zero for either side or both sides we should wait
            shouldWait |= m_ServerNotificationInfo.Count() == 0 || m_ClientNotificationInfo.Count() == 0;

            // Early exit if we should wait at this point
            if (shouldWait)
            {
                return shouldWait;
            }

            foreach (var clientEntry in m_ServerNotificationInfo)
            {
                var clientList = m_ClientNotificationInfo.Select(c => c).Where(c => c.ClientId == clientEntry.ClientId);
                if (clientList == null || clientList.Count() == 0)
                {
                    shouldWait = true;
                }
                else
                {
                    var client = clientList.First();
                    // Compare the events processed to make sure the events are invoked on the client,a message sent to the server,
                    // and the same event is invoked on the server too.
                    foreach (var sceneEventType in clientEntry.EventsProcessed)
                    {
                        if (!client.EventsProcessed.Contains(sceneEventType))
                        {
                            shouldWait = true;
                            break;
                        }
                    }
                }

                if (shouldWait)
                {
                    break;
                }
            }

            return shouldWait;
        }

        [UnityTest]
        public IEnumerator SceneEventCallbackNotifications()
        {
            // First we disconnect and shutdown because we want to verify the synchronize events
            yield return Teardown();

            // Give a little time for handling clean-up and the like
            yield return new WaitForSeconds(0.01f);

            // We set this to true in order to bypass the automatic starting of the host and clients
            m_BypassStartAndWaitForClients = true;

            // Now just create the instances (server and client) without starting anything
            yield return Setup();

            // Start the host and  clients
            if (!MultiInstanceHelpers.Start(true, m_ServerNetworkManager, m_ClientNetworkManagers))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }
            RegisterSceneManagerHandler();
            // Immediately register for all pertinent event notifications we want to test and validate working
            // For the server:
            m_ServerNetworkManager.SceneManager.OnLoad += Server_OnLoad;
            m_ServerNetworkManager.SceneManager.OnLoadComplete += Server_OnLoadComplete;
            m_ServerNetworkManager.SceneManager.OnLoadEventCompleted += Server_OnLoadEventCompleted;
            m_ServerNetworkManager.SceneManager.OnUnload += Server_OnUnload;
            m_ServerNetworkManager.SceneManager.OnUnloadComplete += Server_OnUnloadComplete;
            m_ServerNetworkManager.SceneManager.OnUnloadEventCompleted += Server_OnUnloadEventCompleted;
            m_ServerNetworkManager.SceneManager.OnSynchronizeComplete += Server_OnSynchronizeComplete;
            m_ServerNetworkManager.SceneManager.OnSceneEvent += Server_OnSceneEvent;
            // For the clients:
            foreach (var client in m_ClientNetworkManagers)
            {
                client.SceneManager.OnLoad += Client_OnLoad;
                client.SceneManager.OnLoadComplete += Client_OnLoadComplete;
                client.SceneManager.OnLoadEventCompleted += Client_OnLoadEventCompleted;
                client.SceneManager.OnUnload += Client_OnUnload;
                client.SceneManager.OnUnloadComplete += Client_OnUnloadComplete;
                client.SceneManager.OnUnloadEventCompleted += Client_OnUnloadEventCompleted;
                client.SceneManager.OnSynchronizeComplete += Client_OnSynchronizeComplete;
                client.SceneManager.OnSynchronize += Client_OnSynchronize;
            }

            // Wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(m_ClientNetworkManagers));

            // Wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(m_ServerNetworkManager, NbClients + 1));

            //////////////////////////////////////////
            // Testing synchronize event notifications
            var shouldWait = NotificationTestShouldWait();

            while (shouldWait)
            {
                yield return new WaitForSeconds(0.01f);
                shouldWait = NotificationTestShouldWait();
            }

            // Reset for next test
            ResetNotificationInfo();

            //////////////////////////////////////////
            // Testing load event notifications
            Assert.That(m_ServerNetworkManager.SceneManager.LoadScene(k_AdditiveScene1, LoadSceneMode.Additive) == SceneEventProgressStatus.Started);
            shouldWait = NotificationTestShouldWait() || !m_CurrentScene.IsValid() || !m_CurrentScene.isLoaded;

            while (shouldWait)
            {
                yield return new WaitForSeconds(0.01f);
                shouldWait = NotificationTestShouldWait() || !m_CurrentScene.IsValid() || !m_CurrentScene.isLoaded;
                if (!shouldWait)
                {
                    shouldWait = !ValidateCompletedNotifications();
                }
            }

            // Reset for next test
            ResetNotificationInfo();

            //////////////////////////////////////////
            // Testing unload event notifications
            Assert.That(m_ServerNetworkManager.SceneManager.UnloadScene(m_CurrentScene) == SceneEventProgressStatus.Started);
            shouldWait = NotificationTestShouldWait() || m_CurrentScene.IsValid() || m_CurrentScene.isLoaded;
            while (shouldWait)
            {
                yield return new WaitForSeconds(0.01f);
                shouldWait = NotificationTestShouldWait() || m_CurrentScene.isLoaded;
                if (!shouldWait)
                {
                    shouldWait = !ValidateCompletedNotifications();
                }
            }
            yield break;
        }

        private void Client_OnSynchronize(ulong clientId)
        {
            ClientProcessedNotification(clientId, SceneEventType.Synchronize, false);
        }

        private void Client_OnSynchronizeComplete(ulong clientId)
        {
            Debug.Log($"Client {clientId} synchronized.");
            ClientProcessedNotification(clientId, SceneEventType.SynchronizeComplete, false, true);
        }

        private void Client_OnUnloadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            ProcessCompletedNotification(sceneName, clientsCompleted, SceneEventType.UnloadEventCompleted, false);
        }

        private void Client_OnUnloadComplete(ulong clientId, string sceneName)
        {
            Debug.Log($"Client {clientId} unloaded {sceneName}");
            ClientProcessedNotification(clientId, SceneEventType.UnloadComplete, false, true);
        }

        private void Client_OnUnload(ulong clientId, string sceneName, AsyncOperation asyncOperation)
        {
            ClientProcessedNotification(clientId, SceneEventType.Unload, false);
        }

        private void Client_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            ProcessCompletedNotification(sceneName, clientsCompleted, SceneEventType.LoadEventCompleted, false);
        }

        private void Client_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            Debug.Log($"Client {clientId} loaded {sceneName} in {loadSceneMode} LoadSceneMode");
            ClientProcessedNotification(clientId, SceneEventType.LoadComplete, false, true);
        }

        private void Client_OnLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            ClientProcessedNotification(clientId, SceneEventType.Load, false);
        }

        #region ServerEvents
        private void Server_OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        if (sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId)
                        {
                            // Set the scene currently loaded
                            m_CurrentScene = sceneEvent.Scene;
                        }
                        break;
                    }
            }
        }

        private void Server_OnSynchronizeComplete(ulong clientId)
        {
            Debug.Log($"Server Received Client {clientId} synchronized complete.");
            ClientProcessedNotification(clientId, SceneEventType.SynchronizeComplete, true, true);
        }

        private void Server_OnUnloadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            ProcessCompletedNotification(sceneName, clientsCompleted, SceneEventType.UnloadEventCompleted, true);
        }

        private void Server_OnUnloadComplete(ulong clientId, string sceneName)
        {
            ClientProcessedNotification(clientId, SceneEventType.UnloadComplete, true, true);
        }

        private void Server_OnUnload(ulong clientId, string sceneName, AsyncOperation asyncOperation)
        {
            ClientProcessedNotification(clientId, SceneEventType.Unload, true);
        }

        private void Server_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            ProcessCompletedNotification(sceneName, clientsCompleted, SceneEventType.LoadEventCompleted, true);
        }

        private void Server_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            ClientProcessedNotification(clientId, SceneEventType.LoadComplete, true, true);
        }

        private void Server_OnLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            ClientProcessedNotification(clientId, SceneEventType.Load, true);
        }
        #endregion

    }

    /// <summary>
    /// This is where all of the SceneEventData specific tests should reside.
    /// </summary>
    public class SceneEventDataTests
    {
        /// <summary>
        /// This verifies that change from Allocator.TmpJob to Allocator.Persistent
        /// will not cause memory leak warning notifications if the scene event takes
        /// longer than 4 frames to complete.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator FastReaderAllocationTest()
        {
            var fastBufferWriter = new FastBufferWriter(1024, Unity.Collections.Allocator.Persistent);
            var networkManagerGameObject = new GameObject("NetworkManager - Host");

            var networkManager = networkManagerGameObject.AddComponent<NetworkManager>();
            networkManager.NetworkConfig = new NetworkConfig()
            {
                ConnectionApproval = false,
                NetworkPrefabs = new List<NetworkPrefab>(),
                NetworkTransport = networkManagerGameObject.AddComponent<SIPTransport>(),
            };

            networkManager.StartHost();

            var sceneEventData = new SceneEventData(networkManager);
            sceneEventData.SceneEventType = SceneEventType.Load;
            sceneEventData.SceneHash = XXHash.Hash32("SomeRandomSceneName");
            sceneEventData.SceneEventProgressId = Guid.NewGuid();
            sceneEventData.LoadSceneMode = LoadSceneMode.Single;
            sceneEventData.SceneHandle = 32768;

            sceneEventData.Serialize(fastBufferWriter);
            var nativeArray = new Unity.Collections.NativeArray<byte>(fastBufferWriter.ToArray(), Unity.Collections.Allocator.Persistent);
            var fastBufferReader = new FastBufferReader(nativeArray, Unity.Collections.Allocator.Persistent, fastBufferWriter.ToArray().Length);

            var incomingSceneEventData = new SceneEventData(networkManager);
            incomingSceneEventData.Deserialize(fastBufferReader);

            // Wait for 30 frames
            var framesToWait = Time.frameCount + 30;
            yield return new WaitUntil(() => Time.frameCount > framesToWait);

            // As long as no errors occurred, the test verifies that
            incomingSceneEventData.Dispose();
            fastBufferReader.Dispose();
            nativeArray.Dispose();
            fastBufferWriter.Dispose();
            networkManager.Shutdown();
            Object.Destroy(networkManagerGameObject);
        }
    }

    public class NetworkSceneManagerDDOLTests
    {
        private NetworkManager m_ServerNetworkManager;
        private GameObject m_NetworkManagerGameObject;
        private GameObject m_DDOL_ObjectToSpawn;

        protected float m_ConditionMetFrequency = 0.1f;

        [UnitySetUp]
        protected IEnumerator SetUp()
        {
            m_NetworkManagerGameObject = new GameObject("NetworkManager - Host");
            m_ServerNetworkManager = m_NetworkManagerGameObject.AddComponent<NetworkManager>();

            m_DDOL_ObjectToSpawn = new GameObject();
            m_DDOL_ObjectToSpawn.AddComponent<NetworkObject>();
            m_DDOL_ObjectToSpawn.AddComponent<DDOLBehaviour>();

            m_ServerNetworkManager.NetworkConfig = new NetworkConfig()
            {
                ConnectionApproval = false,
                NetworkPrefabs = new List<NetworkPrefab>(),
                NetworkTransport = m_NetworkManagerGameObject.AddComponent<SIPTransport>(),
            };
            m_ServerNetworkManager.StartHost();
            yield break;
        }

        [UnityTearDown]
        protected IEnumerator TearDown()
        {
            m_ServerNetworkManager.Shutdown();

            Object.Destroy(m_NetworkManagerGameObject);
            Object.Destroy(m_DDOL_ObjectToSpawn);

            yield break;
        }

        public enum DefaultState
        {
            IsEnabled,
            IsDisabled
        }

        public enum MovedIntoDDOLBy
        {
            User,
            NetworkSceneManager
        }

        public enum NetworkObjectType
        {
            InScenePlaced,
            DynamicallySpawned
        }

        /// <summary>
        /// Tests to make sure NetworkObjects moved into the DDOL will
        /// restore back to their currently active state when a full
        /// scene transition is complete.
        /// This tests both in-scene placed and dynamically spawned NetworkObjects
        [UnityTest]
        public IEnumerator InSceneNetworkObjectState([Values(DefaultState.IsEnabled, DefaultState.IsDisabled)] DefaultState activeState,
            [Values(MovedIntoDDOLBy.User, MovedIntoDDOLBy.NetworkSceneManager)] MovedIntoDDOLBy movedIntoDDOLBy,
            [Values(NetworkObjectType.InScenePlaced, NetworkObjectType.DynamicallySpawned)] NetworkObjectType networkObjectType)
        {
            var isActive = activeState == DefaultState.IsEnabled ? true : false;
            var isInScene = networkObjectType == NetworkObjectType.InScenePlaced ? true : false;
            var networkObject = m_DDOL_ObjectToSpawn.GetComponent<NetworkObject>();
            var ddolBehaviour = m_DDOL_ObjectToSpawn.GetComponent<DDOLBehaviour>();

            // All tests require this to be false
            networkObject.DestroyWithScene = false;

            if (movedIntoDDOLBy == MovedIntoDDOLBy.User)
            {
                ddolBehaviour.MoveToDDOL();
            }

            // Sets whether we are in-scene or dynamically spawned NetworkObject
            ddolBehaviour.SetInScene(isInScene);

            Assert.That(networkObject.IsSpawned);

            m_DDOL_ObjectToSpawn.SetActive(isActive);

            m_ServerNetworkManager.SceneManager.MoveObjectsToDontDestroyOnLoad();

            yield return new WaitForSeconds(0.03f);

            // It should be isActive when MoveObjectsToDontDestroyOnLoad is called.
            Assert.That(networkObject.isActiveAndEnabled == isActive);

            m_ServerNetworkManager.SceneManager.MoveObjectsFromDontDestroyOnLoadToScene(SceneManager.GetActiveScene());

            yield return new WaitForSeconds(0.03f);

            // It should be isActive when MoveObjectsFromDontDestroyOnLoadToScene is called.
            Assert.That(networkObject.isActiveAndEnabled == isActive);

            //Done
            networkObject.Despawn(false);
        }

        public class DDOLBehaviour : NetworkBehaviour
        {
            public void MoveToDDOL()
            {
                DontDestroyOnLoad(gameObject);
            }

            public void SetInScene(bool isInScene)
            {
                var networkObject = GetComponent<NetworkObject>();
                networkObject.IsSceneObject = isInScene;
            }
        }

    }


    public class NetworkSceneManagerFixValidationTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 0;

        public override IEnumerator Setup()
        {
            m_BypassStartAndWaitForClients = true;
            return base.Setup();
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
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = gameObject });

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = gameObject });
            }

            // Start the host and clients
            if (!MultiInstanceHelpers.Start(useHost, m_ServerNetworkManager, m_ClientNetworkManagers))
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
    }
}
