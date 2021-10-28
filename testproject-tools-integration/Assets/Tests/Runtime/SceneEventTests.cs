using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SceneEventType = Unity.Netcode.SceneEventType;

namespace TestProject.ToolsIntegration.RuntimeTests
{
    internal class SceneEventTests : SingleClientMetricTestBase
    {
        // scenes referenced in this test must also be in the build settings of the project.
        private const string k_SimpleSceneName = "SimpleScene";

        private NetworkSceneManager m_ClientNetworkSceneManager;
        private NetworkSceneManager m_ServerNetworkSceneManager;
        private Scene m_LoadedScene;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return base.Setup();
            m_ClientNetworkSceneManager = Client.SceneManager;
            m_ServerNetworkSceneManager = Server.SceneManager;
            Server.NetworkConfig.EnableSceneManagement = true;
            m_ServerNetworkSceneManager.OnSceneEvent += RegisterLoadedSceneCallback;
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            if (m_LoadedScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(m_LoadedScene);
            }
            Client.Shutdown();
            Server.Shutdown();
            yield return base.Teardown();
        }

        /// <summary>
        /// Used to create configurations for each scene event metric test
        /// </summary>
        public class EventConfiguration
        {
            /// <summary>
            /// Scene event to wait for in order for the metric event
            /// to be sent.
            /// </summary>
            public SceneEventType SceneEventToCheck;

            /// <summary>
            /// What scene event metric we are testing/expecting
            /// </summary>
            public SceneEventType MetricEventToCheck;

            /// <summary>
            /// If true: use NetworkMetricTypes.SceneEventSent to check for MetricEventToCheck
            /// If false: use NetworkMetricTypes.SceneEventReceived to check for MetricEventToCheck
            /// </summary>
            public bool CheckSending;

            /// <summary>
            /// If true: we are expecting to be sending to or receiving from the server id
            /// If false: we are expecting to be sending to or receiving from the client id
            /// </summary>
            public bool ExpectServerId;

            /// <summary>
            /// If true: We use the ServerMetrics.Dispatcher
            /// If false: We use the ClientMetrics.Dispatcher
            /// </summary>
            public bool ServerDispatcher;
        }

        /// <summary>
        /// This is the list of tests to be performed by:
        /// TestLoadEvents: Tests all loading event metrics
        /// TestUnloadEvents: Tests all unloading event metrics
        /// TestSynchronizeEvents: Tests all synchronization event metrics
        /// Note: The names are the same as the original methods
        /// </summary>
        public enum EventToCheck
        {
            TestS2CLoadSent,
            TestS2CLoadReceived,
            TestC2SLoadCompleteSent,
            TestC2SLoadCompleteReceived,
            TestS2CLoadEventCompletedSent,
            TestS2CLoadEventCompletedReceived,
            TestS2CUnloadSent,
            TestS2CUnloadReceived,
            TestC2SUnloadCompleteSent,
            TestC2SUnloadCompleteReceived,
            TestS2CUnloadEventCompletedSent,
            TestS2CUnloadEventCompletedReceived,
            TestS2CSyncSent,
            TestS2CSyncReceived,
            TestC2SSyncCompleteSent,
            TestC2SSyncCompleteReceived
        }

        /// <summary>
        /// Configurations to be tested
        /// </summary>
        private EventConfiguration[] m_EventConfigurations = new EventConfiguration[]
            {
                new EventConfiguration()                        // TestS2CLoadSent
                {
                    SceneEventToCheck = SceneEventType.LoadEventCompleted,
                    MetricEventToCheck = SceneEventType.Load,
                    CheckSending = true,                        // Check that we sent the Load event
                    ExpectServerId = false,                     // Check that we are sending to the client
                    ServerDispatcher = true,                    // We listen to the server dispatcher for this
                },
                new EventConfiguration()                        // TestS2CLoadReceived
                {
                    SceneEventToCheck = SceneEventType.LoadEventCompleted,
                    MetricEventToCheck = SceneEventType.Load,
                    CheckSending = false,                       // Check that we received the Load event
                    ExpectServerId = true,                      // Check that we are receiving from the server
                    ServerDispatcher = false,                   // We listen to the client dispatcher for this
                },
                new EventConfiguration()                        // TestC2SLoadCompleteSent
                {
                    SceneEventToCheck = SceneEventType.LoadEventCompleted,
                    MetricEventToCheck = SceneEventType.LoadComplete,
                    CheckSending = true,                        // Check that we sent the LoadComplete event
                    ExpectServerId = true,                      // Check that we are sending to the server
                    ServerDispatcher = false,                   // We listen to the client dispatcher for this
                },
                new EventConfiguration()                        // TestC2SLoadCompleteReceived
                {
                    SceneEventToCheck = SceneEventType.LoadEventCompleted,
                    MetricEventToCheck = SceneEventType.LoadComplete,
                    CheckSending = false,                       // Check that we received the LoadComplete event
                    ExpectServerId = false,                     // Check that we are sending to the client
                    ServerDispatcher = true,                    // We listen to the server dispatcher for this
                },
                new EventConfiguration()                        // TestS2CLoadEventCompletedSent
                {
                    SceneEventToCheck = SceneEventType.LoadEventCompleted,
                    MetricEventToCheck = SceneEventType.LoadEventCompleted,
                    CheckSending = true,                        // Check that we sent the LoadEventCompleted event
                    ExpectServerId = true,                      // Check that we are sending to the client
                    ServerDispatcher = true,                    // We listen to the server dispatcher for this
                },
                new EventConfiguration()                        // TestS2CLoadEventCompletedReceived
                {
                    SceneEventToCheck = SceneEventType.LoadEventCompleted,
                    MetricEventToCheck = SceneEventType.LoadEventCompleted,
                    CheckSending = false,                       // Check that we received the LoadEventCompleted event
                    ExpectServerId = true,                      // Check that we received from the server
                    ServerDispatcher = false,                   // We listen to the client dispatcher for this
                },
                new EventConfiguration()                        // TestS2CUnloadSent
                {
                    SceneEventToCheck = SceneEventType.UnloadComplete,
                    MetricEventToCheck = SceneEventType.Unload,
                    CheckSending = true,                        // Check that we sent the Unload event
                    ExpectServerId = false,                     // Check that we are sending to the client
                    ServerDispatcher = true,                    // We listen to the server dispatcher for this
                },
                new EventConfiguration()                        // TestS2CUnloadReceived
                {
                    SceneEventToCheck = SceneEventType.UnloadComplete,
                    MetricEventToCheck = SceneEventType.Unload,
                    CheckSending = false,                       // Check that we received the Unload event
                    ExpectServerId = true,                      // Check that we received from the server
                    ServerDispatcher = false,                   // We listen to the client dispatcher for this
                },
                new EventConfiguration()                        // TestC2SUnloadCompleteSent
                {
                    SceneEventToCheck = SceneEventType.UnloadComplete,
                    MetricEventToCheck = SceneEventType.UnloadComplete,
                    CheckSending = true,                        // Check that we sent the UnloadComplete event
                    ExpectServerId = true,                      // Check that we are sending to the server
                    ServerDispatcher = false,                   // We listen to the client dispatcher for this
                },
                new EventConfiguration()                        // TestC2SUnloadCompleteReceived
                {
                    SceneEventToCheck = SceneEventType.UnloadComplete,
                    MetricEventToCheck = SceneEventType.UnloadComplete,
                    CheckSending = false,                       // Check that we received the UnloadComplete event
                    ExpectServerId = false,                     // Check that we are receiving from the client
                    ServerDispatcher = true,                    // We listen to the server dispatcher for this
                },
                new EventConfiguration()                        // TestS2CUnloadEventCompletedSent
                {
                    SceneEventToCheck = SceneEventType.UnloadComplete,
                    MetricEventToCheck = SceneEventType.UnloadEventCompleted,
                    CheckSending = true,                        // Check that we send the UnloadEventCompleted event
                    ExpectServerId = true,                      // UnloadEventCompleted is always a broadcast by server
                    ServerDispatcher = true,                    // We listen to the server dispatcher for this
                },
                new EventConfiguration()                        // TestS2CUnloadEventCompletedReceived
                {
                    SceneEventToCheck = SceneEventType.UnloadComplete,
                    MetricEventToCheck = SceneEventType.UnloadEventCompleted,
                    CheckSending = false,                      // Check that we received the UnloadEventCompleted event
                    ExpectServerId = true,                     // Check that we are receiving from the server
                    ServerDispatcher = false,                  // We listen to the client dispatcher for this
                },
                new EventConfiguration()                        // TestS2CSyncSent
                {
                    SceneEventToCheck = SceneEventType.SynchronizeComplete,
                    MetricEventToCheck = SceneEventType.Synchronize,
                    CheckSending = true,                        // Check that we send the Synchronize event
                    ExpectServerId = false,                     // Check that we are sending to the client
                    ServerDispatcher = true,                    // We listen to the server dispatcher for this
                },
                new EventConfiguration()                        // TestS2CSyncReceived
                {
                    SceneEventToCheck = SceneEventType.SynchronizeComplete,
                    MetricEventToCheck = SceneEventType.Synchronize,
                    CheckSending = false,                      // Check that we receive the Synchronize event
                    ExpectServerId = true,                     // Check that we are receiving from the server
                    ServerDispatcher = false,                  // We listen to the client dispatcher for this
                },
                new EventConfiguration()                        // TestC2SSyncCompleteSent
                {
                    SceneEventToCheck = SceneEventType.SynchronizeComplete,
                    MetricEventToCheck = SceneEventType.SynchronizeComplete,
                    CheckSending = true,                        // Check that we send the SynchronizeComplete event
                    ExpectServerId = true,                      // Check that we are sending to the server
                    ServerDispatcher = false,                   // We listen to the client dispatcher for this
                },
                new EventConfiguration()                        // TestC2SSyncCompleteReceived
                {
                    SceneEventToCheck = SceneEventType.SynchronizeComplete,
                    MetricEventToCheck = SceneEventType.SynchronizeComplete,
                    CheckSending = false,                       // Check that we receive the SynchronizeComplete event
                    ExpectServerId = false,                     // Check that we are receiving from the client
                    ServerDispatcher = true,                    // We listen to the server dispatcher for this
                }
            };

        /// <summary>
        /// The first thing to test is that we are in alignment
        /// with the configurations vs the event types.
        /// </summary>
        [Test]
        [Order(0)]
        public void ValidateEventTestConfigurations()
        {
            var eventTypesCount = Enum.GetNames(typeof(EventToCheck)).Length;
            Assert.That(eventTypesCount == m_EventConfigurations.Length);
            var eventTestTypes = Enum.GetValues(typeof(EventToCheck));

            // Make sure we have aligned all configuration types with scene event types
            for (int i = 0; i < eventTypesCount; i++)
            {
                var eventTestType = (EventToCheck)eventTestTypes.GetValue(i);
                var eventConfiguration = m_EventConfigurations[i];
                var validatedConfigToEventTest = false;
                switch (eventTestType)
                {
                    case EventToCheck.TestS2CLoadSent:
                    case EventToCheck.TestS2CLoadReceived:
                    case EventToCheck.TestC2SLoadCompleteSent:
                    case EventToCheck.TestC2SLoadCompleteReceived:
                    case EventToCheck.TestS2CLoadEventCompletedSent:
                    case EventToCheck.TestS2CLoadEventCompletedReceived:
                        {
                            switch (eventConfiguration.MetricEventToCheck)
                            {
                                case SceneEventType.Load:
                                case SceneEventType.LoadComplete:
                                case SceneEventType.LoadEventCompleted:
                                    {
                                        validatedConfigToEventTest = true;
                                        break;
                                    }
                            }
                            break;
                        }
                    case EventToCheck.TestS2CUnloadSent:
                    case EventToCheck.TestS2CUnloadReceived:
                    case EventToCheck.TestC2SUnloadCompleteSent:
                    case EventToCheck.TestC2SUnloadCompleteReceived:
                    case EventToCheck.TestS2CUnloadEventCompletedSent:
                    case EventToCheck.TestS2CUnloadEventCompletedReceived:
                        {
                            switch (eventConfiguration.MetricEventToCheck)
                            {
                                case SceneEventType.Unload:
                                case SceneEventType.UnloadComplete:
                                case SceneEventType.UnloadEventCompleted:
                                    {
                                        validatedConfigToEventTest = true;
                                        break;
                                    }
                            }
                            break;
                        }
                    case EventToCheck.TestS2CSyncSent:
                    case EventToCheck.TestS2CSyncReceived:
                    case EventToCheck.TestC2SSyncCompleteSent:
                    case EventToCheck.TestC2SSyncCompleteReceived:
                        {
                            switch (eventConfiguration.MetricEventToCheck)
                            {
                                case SceneEventType.Synchronize:
                                case SceneEventType.SynchronizeComplete:
                                    {
                                        validatedConfigToEventTest = true;
                                        break;
                                    }
                            }
                            break;
                        }
                }
                Assert.IsTrue(validatedConfigToEventTest);
            }
        }

        /// <summary>
        /// Load Scene Event Tests
        /// </summary>
        [UnityTest]
        public IEnumerator TestLoadEvents([Values(EventToCheck.TestS2CLoadSent,EventToCheck.TestS2CLoadReceived,
            EventToCheck.TestC2SLoadCompleteSent, EventToCheck.TestC2SLoadCompleteReceived, EventToCheck.TestS2CLoadEventCompletedSent,
            EventToCheck.TestS2CLoadEventCompletedReceived)] EventToCheck eventConfigurationType)
        {
            var loadEvent = m_EventConfigurations[(int)eventConfigurationType];
            var clientIdToExpect = loadEvent.ExpectServerId ? Server.LocalClientId : Client.LocalClientId;
            var metricType = loadEvent.CheckSending ? NetworkMetricTypes.SceneEventSent : NetworkMetricTypes.SceneEventReceived;
            var metricsDispatcher = loadEvent.ServerDispatcher ? ServerMetrics.Dispatcher : ClientMetrics.Dispatcher;

            var waitForServerSceneLoaded = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                loadEvent.SceneEventToCheck);

            var waitForClientSceneLoaded = new WaitForSceneEvent(
                 m_ClientNetworkSceneManager,
                 loadEvent.SceneEventToCheck);

            var waitForMetric = new WaitForMetricValues<SceneEventMetric>(
                 metricsDispatcher,
                 metricType,
                 metric => metric.SceneEventType.Equals(loadEvent.MetricEventToCheck.ToString()));

            // Load a scene to trigger the messages
            StartServerLoadScene();

            // Wait for the scene to be loaded completely
            yield return waitForServerSceneLoaded.Wait();
            Assert.IsTrue(waitForServerSceneLoaded.Done);

            yield return waitForClientSceneLoaded.Wait();
            Assert.IsTrue(waitForClientSceneLoaded.Done);

            if (loadEvent.MetricEventToCheck == SceneEventType.LoadEventCompleted)
            {
                yield return waitForMetric.WaitForMetricsReceived();
            }

            var metrics = waitForMetric.AssertMetricValuesHaveBeenFound();
            Assert.That(metrics.Count > 0);

            var allResultsVerified = false;
            foreach (var metric in metrics)
            {
                if (metric.SceneEventType != loadEvent.MetricEventToCheck.ToString())
                {
                    continue;
                }
                if (clientIdToExpect != metric.Connection.Id)
                {
                    continue;
                }

                if (metric.SceneName != k_SimpleSceneName)
                {
                    continue;
                }
                allResultsVerified = true;
                break;
            }
            Assert.IsTrue(allResultsVerified);
        }

        /// <summary>
        /// Unload Scene Event Tests
        /// </summary>
        [UnityTest]
        public IEnumerator TestUnloadEvents([Values(EventToCheck.TestS2CUnloadSent,
            EventToCheck.TestS2CUnloadReceived, EventToCheck.TestC2SUnloadCompleteSent,
            EventToCheck.TestC2SUnloadCompleteReceived, EventToCheck.TestS2CUnloadEventCompletedSent,
            EventToCheck.TestS2CUnloadEventCompletedReceived)] EventToCheck eventConfigurationType)
        {
            var unloadEvent = m_EventConfigurations[(int)eventConfigurationType];
            var clientIdToExpect = unloadEvent.ExpectServerId ? Server.LocalClientId : Client.LocalClientId;
            var metricType = unloadEvent.CheckSending ? NetworkMetricTypes.SceneEventSent : NetworkMetricTypes.SceneEventReceived;
            var metricsDispatcher = unloadEvent.ServerDispatcher ? ServerMetrics.Dispatcher : ClientMetrics.Dispatcher;

            // Load a scene so that we can unload it
            yield return LoadTestScene(k_SimpleSceneName);

            var serverSceneUnloaded = false;
            var clientSceneUnloaded = false;
            // Subscribe to the server OnSceneEvent to detect when both client and server are done unloading
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(unloadEvent.SceneEventToCheck))
                {
                    if (sceneEvent.ClientId == Server.LocalClientId)
                    {
                        serverSceneUnloaded = true;
                    }
                    else
                    {
                        clientSceneUnloaded = true;
                    }
                }
            };

            var waitForMetric = new WaitForMetricValues<SceneEventMetric>(
                metricsDispatcher,
                metricType,
                metric => metric.SceneEventType.Equals(unloadEvent.MetricEventToCheck.ToString()));

            // Unload the scene to trigger the messages
            StartServerUnloadScene();

            // Wait for the scene to unload for the server
            yield return WaitForCondition(() => serverSceneUnloaded);
            Assert.IsTrue(serverSceneUnloaded);

            // Wait for the scene  to unload for the client
            yield return WaitForCondition(() => clientSceneUnloaded);
            Assert.IsTrue(clientSceneUnloaded);

            // if the server is listening for incoming client messages
            if (unloadEvent.ServerDispatcher && !unloadEvent.CheckSending)
            {
                // Wait for the metric to be emitted when the message is received
                yield return waitForMetric.WaitForMetricsReceived();

            }

            if (unloadEvent.MetricEventToCheck == SceneEventType.UnloadEventCompleted)
            {
                yield return waitForMetric.WaitForMetricsReceived();
            }

            var metrics = waitForMetric.AssertMetricValuesHaveBeenFound();
            Assert.That(metrics.Count > 0);

            var metric = metrics.First();
            Assert.AreEqual(unloadEvent.MetricEventToCheck.ToString(), metric.SceneEventType);
            Assert.AreEqual(clientIdToExpect, metric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, metric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestSynchronizeEvents([Values(EventToCheck.TestS2CSyncSent, EventToCheck.TestS2CSyncReceived,
            EventToCheck.TestC2SSyncCompleteSent, EventToCheck.TestC2SSyncCompleteReceived)] EventToCheck eventConfigurationType)
        {
            var syncEvent = m_EventConfigurations[(int)eventConfigurationType];

            var metricType = syncEvent.CheckSending ? NetworkMetricTypes.SceneEventSent : NetworkMetricTypes.SceneEventReceived;

            // Register a callback so we can notify the test when the client and server have completed their sync
            var waitForServerSyncComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                syncEvent.SceneEventToCheck);

            var newClient = CreateAndStartClient();
            var metricsDispatcher = syncEvent.ServerDispatcher ? ServerMetrics.Dispatcher : (newClient.NetworkMetrics as NetworkMetrics).Dispatcher;
            var waitForMetric = new WaitForMetricValues<SceneEventMetric>(
                metricsDispatcher,
                metricType,
                metric => metric.SceneEventType.Equals(syncEvent.MetricEventToCheck.ToString()));

            // Although the metric should have been emitted, wait for the sync to complete
            // as the client/server IDs have not been fully initialized until this is done.
            yield return waitForServerSyncComplete.Wait();
            Assert.IsTrue(waitForServerSyncComplete.Done);

            if (syncEvent.MetricEventToCheck == SceneEventType.SynchronizeComplete)
            {
                yield return waitForMetric.WaitForMetricsReceived();
            }

            var metrics = waitForMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, metrics.Count);

            var clientIdToExpect = syncEvent.ExpectServerId ? Server.LocalClientId : newClient.LocalClientId;
            var sentMetric = metrics.First();

            Assert.AreEqual(syncEvent.MetricEventToCheck.ToString(), sentMetric.SceneEventType);
            Assert.AreEqual(clientIdToExpect, sentMetric.Connection.Id);

            MultiInstanceHelpers.StopOneClient(newClient);
        }

        // Create a new client to connect to an already started server to trigger a server sync.
        private NetworkManager CreateAndStartClient()
        {
            MultiInstanceHelpers.CreateNewClients(1, out var newClients);
            var newClient = newClients[0];

            // Set up the client so it has the same NetworkConfig as the server
            newClient.NetworkConfig.EnableSceneManagement = true;
            newClient.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            // Start the client to trigger the sync
            newClient.StartClient();

            return newClient;
        }

        private void StartServerLoadScene()
        {
            var loadSceneResult = m_ServerNetworkSceneManager.LoadScene(k_SimpleSceneName, LoadSceneMode.Additive);
            Assert.AreEqual(SceneEventProgressStatus.Started, loadSceneResult);
        }

        private void StartServerUnloadScene()
        {
            var unloadSceneResult = m_ServerNetworkSceneManager.UnloadScene(m_LoadedScene);
            Assert.AreEqual(SceneEventProgressStatus.Started, unloadSceneResult);
        }

        // Loads a scene, then waits for the client to notify the server
        // that it has finished loading the scene, as this is the last thing that happens.
        private IEnumerator LoadTestScene(string sceneName)
        {
            var serverLoadComplete = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
                {
                    serverLoadComplete = true;
                }
            };

            var clientLoadComplete = false;
            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
                {
                    clientLoadComplete = true;
                }
            };

            var loadSceneResult = m_ServerNetworkSceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            Assert.AreEqual(SceneEventProgressStatus.Started, loadSceneResult);

            yield return WaitForCondition(() => serverLoadComplete);
            yield return WaitForCondition(() => clientLoadComplete);

            Assert.IsTrue(serverLoadComplete && clientLoadComplete);
        }

        private static IEnumerator WaitForCondition(Func<bool> condition, int maxFrames = 240)
        {
            for (var i = 0; i < maxFrames; i++)
            {
                if (condition.Invoke())
                {
                    break;
                }

                yield return null;
            }
        }

        // Primarily used to get a reference to the Scene loaded
        private void RegisterLoadedSceneCallback(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType.Equals(SceneEventType.LoadComplete) && sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId)
            {
                m_LoadedScene = sceneEvent.Scene;
            }
        }

        private class WaitForSceneEvent
        {
            public WaitForSceneEvent(NetworkSceneManager sceneManager, SceneEventType sceneEventType)
            {
                sceneManager.OnSceneEvent += sceneEvent =>
                {
                    if (sceneEvent.SceneEventType == sceneEventType)
                    {
                        Done = true;
                    }
                };
            }

            public bool Done { get; private set; }

            public IEnumerator Wait()
            {
                yield return WaitForCondition(() => Done);
            }
        }

        // TODO: Remove this once the changes have been approved by the tools team
#if USE_ORIGINAL
        [UnityTest]
        public IEnumerator TestS2CLoadSent()
        {
            m_ServerLoadedScene = false;

            // Register callback for when the server is done loading the scene as this is when it sends the
            // load notification
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventType.LoadComplete) && sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId)
                {
                    m_ServerLoadedScene = true;
                }
            };

            var clientReceivedLoadEvent = false;
            // Register callback for when the client receives the load event from the server
            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventType.LoadComplete))
                {
                    clientReceivedLoadEvent = true;
                }
            };

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(ServerMetrics.Dispatcher, NetworkMetricTypes.SceneEventSent);

            // Load a scene to trigger the messages
            StartServerLoadScene();

            // Wait for the server to load the scene locally first.
            yield return WaitForCondition(() => m_ServerLoadedScene);
            Assert.IsTrue(m_ServerLoadedScene);

            // Now start the wait for the metric to be emitted when the message is sent.
            yield return waitForSentMetric.WaitForMetricsReceived();

            // Finally wait for the client to confirm it loaded the scene
            yield return WaitForCondition(() => clientReceivedLoadEvent);
            Assert.IsTrue(clientReceivedLoadEvent);

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.Load.ToString(), sentMetric.SceneEventType);
            Assert.AreEqual(Client.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, sentMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CLoadReceived()
        {
            m_ServerLoadedScene = false;
            // Register callback for when the server is done loading the scene
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventType.LoadComplete) && sceneEvent.ClientId != m_ServerNetworkManager.LocalClientId)
                {
                    m_ServerLoadedScene = true;
                }
            };

            var clientReceivedLoadEvent = false;
            // Register callback for when the client receives the load event from the server
            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventType.Load))
                {
                    clientReceivedLoadEvent = true;
                }
            };

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(ClientMetrics.Dispatcher, NetworkMetricTypes.SceneEventReceived);

            // Load a scene to trigger the messages
            StartServerLoadScene();

            // Wait for the server to load the scene locally first.
            yield return WaitForCondition(() => m_ServerLoadedScene);
            Assert.IsTrue(m_ServerLoadedScene);

            // Now start the wait for the metric to be emitted when the message is received.
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            // Finally wait for the client to confirm it loaded the scene
            yield return WaitForCondition(() => clientReceivedLoadEvent);
            Assert.IsTrue(clientReceivedLoadEvent);

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.Load.ToString(), receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestC2SLoadCompleteSent()
        {
            // Register a callback so we can notify the test when the client has finished loading the scene locally
            // as this is when the message is sent
            var waitForClientLoadComplete = new WaitForSceneEvent(
                m_ClientNetworkSceneManager,
                SceneEventType.LoadComplete);

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(ClientMetrics.Dispatcher, NetworkMetricTypes.SceneEventSent);

            // Load a scene to trigger the messages
            StartServerLoadScene();

            // Wait for the client to complete loading the scene locally
            yield return waitForClientLoadComplete.Wait();
            Assert.IsTrue(waitForClientLoadComplete.Done);

            // Now start the wait for the metric to be emitted when the message is sent.
            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.LoadComplete.ToString(), sentMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, sentMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestC2SLoadCompleteReceived()
        {
            // Register a callback so we can notify the test when the client has finished loading the scene locally
            // as this is when the message is sent
            var waitForClientLoadComplete = new WaitForSceneEvent(
                m_ClientNetworkSceneManager,
                SceneEventType.LoadComplete);

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(ServerMetrics.Dispatcher, NetworkMetricTypes.SceneEventReceived);

            // Load a scene to trigger the messages
            StartServerLoadScene();


            // Wait for the client to complete loading the scene locally
            yield return waitForClientLoadComplete.Wait();
            Assert.IsTrue(waitForClientLoadComplete.Done);

            // Now start the wait for the metric to be emitted when the message is received
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.LoadComplete.ToString(), receivedMetric.SceneEventType);
            Assert.AreEqual(Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CLoadCompleteSent()
        {
            // Register a callback so we can notify the test when the server has finished loading the scene locally
            // as this is when the message is sent
            var waitForServerLoadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventType.LoadEventCompleted);

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.LoadEventCompleted.ToString()));

            // Load a scene to trigger the messages
            StartServerLoadScene();

            // Wait for the server to complete loading the scene locally
            yield return waitForServerLoadComplete.Wait();
            Assert.IsTrue(waitForServerLoadComplete.Done);

            // Now start the wait for the metric to be emitted when the message is sent
            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(Server.ConnectedClients.Count, sentMetrics.Count);

            var filteredSentMetrics = sentMetrics
                .Where(metric => metric.SceneEventType == SceneEventType.LoadEventCompleted.ToString())
                .Where(metric => metric.SceneName == k_SimpleSceneName);
            CollectionAssert.AreEquivalent(filteredSentMetrics.Select(x => x.Connection.Id), Server.ConnectedClients.Select(x => x.Key));
        }

        [UnityTest]
        public IEnumerator TestS2CLoadCompleteReceived()
        {
            // Register a callback so we can notify the test when the server has finished loading the scene locally
            // as this is when the message is sent
            var waitForServerLoadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventType.LoadEventCompleted);

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.LoadEventCompleted.ToString()));

            // Load a scene to trigger the messages
            StartServerLoadScene();

            // Wait for the server to complete loading the scene locally
            yield return waitForServerLoadComplete.Wait();
            Assert.IsTrue(waitForServerLoadComplete.Done);

            // Now start the wait for the metric to be emitted when the message is received
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);
            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.LoadEventCompleted.ToString(), receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CUnloadSent()
        {
            // Load a scene so that we can unload it
            yield return LoadTestScene(k_SimpleSceneName);

            var serverSceneUnloaded = false;
            var clientSceneUnloaded = false;
            // Register a callback so we can notify the test when the scene has started to unload server side
            // as this is when the message is sent
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                // Since the server sends the SceneEventType.Unload event once it has unloaded
                // the scene, we wait for SceneEventType.UnloadComplete event to make sure the
                // message was sent.
                if (sceneEvent.SceneEventType.Equals(SceneEventType.UnloadComplete))
                {
                    if (sceneEvent.ClientId == m_ServerNetworkManager.LocalClientId)
                    {
                        serverSceneUnloaded = true;
                    }
                    else
                    {
                        clientSceneUnloaded = true;
                    }
                }
            };

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.Unload.ToString()));

            // Unload the scene to trigger the messages
            StartServerUnloadScene();

            // Wait for the scene to unload for the server
            yield return WaitForCondition(() => serverSceneUnloaded);
            Assert.IsTrue(serverSceneUnloaded);

            // Wait for the scene  to unload for the client
            yield return WaitForCondition(() => clientSceneUnloaded);
            Assert.IsTrue(clientSceneUnloaded);

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.Unload.ToString(), sentMetric.SceneEventType);
            Assert.AreEqual(Client.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, sentMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CUnloadReceived()
        {
            // Load a scene so that we can unload it.
            yield return LoadTestScene(k_SimpleSceneName);

            var serverSceneUnloaded = false;

            // Register a callback so we can notify the test when the scene has started to unload server side
            // as this is when the message is sent
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventType.Unload))
                {
                    serverSceneUnloaded = sceneEvent.AsyncOperation.isDone;
                    sceneEvent.AsyncOperation.completed += _ => serverSceneUnloaded = true;
                }
            };

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.Unload.ToString()));

            // Unload the scene to trigger the messages
            StartServerUnloadScene();

            // Wait for the scene to unload locally
            yield return WaitForCondition(() => serverSceneUnloaded);
            Assert.IsTrue(serverSceneUnloaded);

            // Now wait for the metric to be emitted when the message is received
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.Unload.ToString(), receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestC2SUnloadCompleteSent()
        {
            // Load a scene so that we can unload it
            yield return LoadTestScene(k_SimpleSceneName);

            // Register a callback so we can notify the test when the scene has finished unloading client side
            // as this is when the message is sent.
            var waitForClientUnloadComplete = new WaitForSceneEvent(
                m_ClientNetworkSceneManager,
                SceneEventType.UnloadComplete);

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.UnloadComplete.ToString()));

            // Unload a scene to trigger the messages
            StartServerUnloadScene();

            // Wait for the scene to complete unloading locally
            yield return waitForClientUnloadComplete.Wait();
            Assert.IsTrue(waitForClientUnloadComplete.Done);

            // Now wait for the metric to be emitted when the message is sent
            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.UnloadComplete.ToString(), sentMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, sentMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestC2SUnloadCompleteReceived()
        {
            // Load a scene so that we can unload it
            yield return LoadTestScene(k_SimpleSceneName);

            // Register a callback we can notify the test when the scene has finished unloading client side
            // as this is when the message is sent
            var waitForClientUnloadComplete = new WaitForSceneEvent(
                m_ClientNetworkSceneManager,
                SceneEventType.UnloadComplete);

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.UnloadComplete.ToString()));

            // Unload a scene to trigger the messages
            StartServerUnloadScene();

            //Wait for the scene to complete unloading locally
            yield return waitForClientUnloadComplete.Wait();
            Assert.IsTrue(waitForClientUnloadComplete.Done);

            // Now wait for the metric to be emitted when the message is received
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.UnloadComplete.ToString(), receivedMetric.SceneEventType);
            Assert.AreEqual(Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CUnloadCompleteSent()
        {
            // Load a scene so that we can unload it
            yield return LoadTestScene(k_SimpleSceneName);

            // Register a callback so we can notify the test when the scene has finished unloading server side
            // as this is when the message is sent
            var waitForServerUnloadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventType.UnloadEventCompleted);

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.UnloadEventCompleted.ToString()));

            // Unload a scene to trigger the messages
            StartServerUnloadScene();

            // Wait for the scene to complete unloading locally
            yield return waitForServerUnloadComplete.Wait();
            Assert.IsTrue(waitForServerUnloadComplete.Done);

            // Now wait for the metric to be emitted when the message is sent
            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(Server.ConnectedClients.Count, sentMetrics.Count);

            // This message is sent from the server to all connected clients including itself if it is a host,
            // so iterate over the connected client list on the server to ensure that we have a 1-1 match of connected
            // clients to sent metrics.
            var filteredSentMetrics = sentMetrics
                .Where(metric => metric.SceneEventType == SceneEventType.UnloadEventCompleted.ToString())
                .Where(metric => metric.SceneName == k_SimpleSceneName);
            CollectionAssert.AreEquivalent(filteredSentMetrics.Select(x => x.Connection.Id), Server.ConnectedClients.Select(x => x.Key));
        }

        [UnityTest]
        public IEnumerator TestS2CUnloadCompleteReceived()
        {
            // Load a scene so that we can unload it
            yield return LoadTestScene(k_SimpleSceneName);

            // Register a callback so we can notify the test when the scene has finished unloading server side
            // as this is when the message is sent
            var waitForServerUnloadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventType.UnloadEventCompleted);

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.UnloadEventCompleted.ToString()));

            // Unload the scene to trigger the messages
            StartServerUnloadScene();

            // Wait for the scene to unload locally
            yield return waitForServerUnloadComplete.Wait();
            Assert.IsTrue(waitForServerUnloadComplete.Done);

            // Now wait for the metric to be emitted when the message is received
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);
            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.UnloadEventCompleted.ToString(), receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(k_SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CSyncSent()
        {
            // Register a callback so we can notify the test when the client and server have completed their sync
            var waitForServerSyncComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventType.SynchronizeComplete);

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.Synchronize.ToString()));

            // To trigger a sync, we need to connect a new client to an already started server, so create a client
            var newClient = CreateAndStartClient();

            // Wait for the metric to be emitted when the server sends the sync message back to the client
            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            // Although the metric should have been emitted, wait for the sync to complete
            // as the client/server IDs have not been fully initialized until this is done.
            yield return waitForServerSyncComplete.Wait();
            Assert.IsTrue(waitForServerSyncComplete.Done);

            var sentMetric = sentMetrics.First();

            Assert.AreEqual(SceneEventType.Synchronize.ToString(), sentMetric.SceneEventType);
            Assert.AreEqual(newClient.LocalClientId, sentMetric.Connection.Id);

            MultiInstanceHelpers.StopOneClient(newClient);
        }

        [UnityTest]
        public IEnumerator TestS2CSyncReceived()
        {
            // To trigger a sync, we need to connect a new client to an already started server, so create a client
            var newClient = CreateAndStartClient();

            // Now the client is started we can grab the NetworkMetrics field from it
            var newClientMetrics = newClient.NetworkMetrics as NetworkMetrics;

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                newClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.Synchronize.ToString()));

            // Wait for the metric to be emitted when the message is received on the client from the server
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);
            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.Synchronize.ToString(), receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);

            MultiInstanceHelpers.StopOneClient(newClient);
        }

        [UnityTest]
        public IEnumerator TestC2SSyncCompleteSent()
        {
            // To trigger a sync, we need to connect a new client to an already started server, so create a client
            var newClient = CreateAndStartClient();

            // Now the client is started we can grab the NetworkMetrics field from it
            var newClientMetrics = newClient.NetworkMetrics as NetworkMetrics;

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                newClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.SynchronizeComplete.ToString()));

            // Wait for the metric to be emitted when the client has completed the sync locally and sends the message
            // to the server
            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);
            var sentMetric = sentMetrics.First();

            Assert.AreEqual(SceneEventType.SynchronizeComplete.ToString(), sentMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);

            MultiInstanceHelpers.StopOneClient(newClient);
        }

        [UnityTest]
        public IEnumerator TestC2SSyncCompleteReceived()
        {
            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.SynchronizeComplete.ToString()));

            // To trigger a sync, we need to connect a new client to an already started server, so create a client
            var newClient = CreateAndStartClient();

            // Wait for the metric to be emitted when the client has completed the sync locally and the message is
            // received on the server
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.SynchronizeComplete.ToString(), receivedMetric.SceneEventType);
            Assert.AreEqual(newClient.LocalClientId, receivedMetric.Connection.Id);

            MultiInstanceHelpers.StopOneClient(newClient);
        }

        // Unloads a loaded scene. If the scene is not loaded, this is a no-op
        private IEnumerator UnloadTestScene(Scene scene)
        {
            if (!scene.isLoaded)
            {
                yield break;
            }

            m_ServerNetworkSceneManager.UnloadScene(scene);
            var sceneUnloaded = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType == SceneEventType.UnloadComplete)
                {
                    sceneUnloaded = true;
                }
            };

            yield return WaitForCondition(() => sceneUnloaded);

            Assert.IsTrue(sceneUnloaded);
        }
#endif
    }
}
