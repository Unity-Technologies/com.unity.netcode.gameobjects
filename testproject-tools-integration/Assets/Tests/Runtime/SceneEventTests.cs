using System.Collections;
using System.Linq;
using NUnit.Framework;
using TestProject.ToolsIntegration.RuntimeTests.Utility;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.ToolsIntegration.RuntimeTests
{
    public class SceneEventTests : SingleClientMetricTestBase
    {
        // scenes referenced in this test must also be in the build settings of the project.
        private const string SimpleSceneName = "SimpleScene";
        private const string EmptySceneName = "EmptyScene";

        private NetworkManager m_Client;
        private NetworkSceneManager m_ClientNetworkSceneManager;
        private NetworkSceneManager m_ServerNetworkSceneManager;
        private Scene m_LoadedScene;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return base.Setup();
            m_Client = m_ClientNetworkManagers[0];
            m_ClientNetworkSceneManager = m_Client.SceneManager;
            m_ServerNetworkSceneManager = Server.SceneManager;
            Server.NetworkConfig.EnableSceneManagement = true;

            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (!sceneEvent.SceneEventType.Equals(SceneEventData.SceneEventTypes.S2C_Load)) return;

                m_LoadedScene = SceneManager.GetSceneByName(sceneEvent.SceneName);
                if (m_ClientNetworkSceneManager.ScenesLoaded.ContainsKey(m_LoadedScene.handle)) return;

                // As we are running the client and the server using the multi-process test runner we need to sync the
                // scene handles manually here, as they share a SceneManager.
                m_ClientNetworkSceneManager.ScenesLoaded.Add(m_LoadedScene.handle, m_LoadedScene);
                m_ClientNetworkSceneManager.ServerSceneHandleToClientSceneHandle.Add(m_LoadedScene.handle, m_LoadedScene.handle);
            };
        }

        // [UnityTearDown]
        // public override IEnumerator Teardown()
        // {
        //     m_ServerNetworkSceneManager.UnloadScene(m_LoadedScene);
        //     return base.Teardown();
        // }

        [UnityTest]
        public IEnumerator TestSceneEventMetrics_LoadScene_S2C_Load()
        {
            ////////// ARRANGE //////////

            // Metric is not emitted until the scene has loaded server side, so we wait until this has happened
            // before waiting for the metric.
            var serverSceneLoaded = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventData.SceneEventTypes.S2C_Load))
                {
                    serverSceneLoaded = sceneEvent.AsyncOperation.isDone;
                    sceneEvent.AsyncOperation.completed += op => serverSceneLoaded = true;
                }
            };

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(ServerMetrics.Dispatcher, NetworkMetricTypes.SceneEventSent);
            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(ClientMetrics.Dispatcher, NetworkMetricTypes.SceneEventReceived);

            ////////// ACT //////////
            Assert.AreEqual(SceneEventProgressStatus.Started, m_ServerNetworkSceneManager.LoadScene(SimpleSceneName, LoadSceneMode.Additive));

            ////////// ASSERT //////////

            // Don't wait for metrics until the server has finished loading the scene locally
            for (int i = 0; i < 240; i++)
            {
                if (serverSceneLoaded)
                    break;
                yield return null;
            }

            Assert.IsTrue(serverSceneLoaded);


            // Now that the scene has loaded locally, the message will be sent to the client which is the point
            // the metric is tracked.
            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.S2C_Load, sentMetric.SceneEventType);
            Assert.AreEqual(m_Client.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, sentMetric.SceneName);

            // Assert received metrics
            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.S2C_Load, receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestSceneEventMetrics_LoadScene_C2S_LoadComplete()
        {
            ////////// ARRANGE //////////
            // Don't wait for metrics until the client scene has finished loading locally.
            var clientSceneLoaded = false;
            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventData.SceneEventTypes.C2S_LoadComplete))
                    clientSceneLoaded = true;
            };

            ////////// ACT //////////
            Assert.AreEqual(SceneEventProgressStatus.Started, m_ServerNetworkSceneManager.LoadScene(SimpleSceneName, LoadSceneMode.Additive));

            ////////// ASSERT //////////
            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(ClientMetrics.Dispatcher, NetworkMetricTypes.SceneEventSent);
            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(ServerMetrics.Dispatcher, NetworkMetricTypes.SceneEventReceived);
            // Wait for the server to finish loading the scene locally
            for (int i = 0; i < 240; i++)
            {
                if (clientSceneLoaded)
                    break;
                yield return null;
            }

            Assert.IsTrue(clientSceneLoaded);


            // Now that the scene has loaded locally, the message will be sent to the client which is the point
            // the metric is tracked.
            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.C2S_LoadComplete, sentMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, sentMetric.SceneName);

            // Assert received metrics
            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.C2S_LoadComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(m_Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }


        [UnityTest]
        public IEnumerator TestSceneEventMetrics_LoadScene_S2C_LoadComplete()
        {
            ////////// ARRANGE //////////

            // Metric is emitted just before this callback is called so register so we know when to start checking
            // for metrics
            var serverSceneLoadComplete = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventData.SceneEventTypes.S2C_LoadComplete)) serverSceneLoadComplete = true;
            };

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_LoadComplete));

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_LoadComplete));

            ////////// ACT //////////
            Assert.AreEqual(SceneEventProgressStatus.Started, m_ServerNetworkSceneManager.LoadScene(SimpleSceneName, LoadSceneMode.Additive));

            ////////// ASSERT //////////

            // Don't wait for metrics until the server has finished loading the scene locally
            for (int i = 0; i < 240; i++)
            {
                if (serverSceneLoadComplete)
                    break;
                yield return null;
            }

            Assert.IsTrue(serverSceneLoadComplete);


            // Now that the scene has loaded locally, the message will be sent to the client which is the point
            // the metric is tracked.
            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(Server.ConnectedClients.Count, sentMetrics.Count);

            // message is sent to all connected clients which for the server includes itself.
            foreach (var client in Server.ConnectedClients)
            {
                var check = false;
                foreach (var metric in sentMetrics)
                {
                    // Assert the metric is correct
                    if (!metric.Connection.Id.Equals(client.Key)) continue;
                    check = true;
                    Assert.AreEqual(SceneEventType.S2C_LoadComplete, metric.SceneEventType);
                    Assert.AreEqual(client.Key, metric.Connection.Id);
                    Assert.AreEqual(SimpleSceneName, metric.SceneName);
                    break;
                }

                // assert that we found a metric for the given client ID.
                Assert.IsTrue(check);
            }

            // Assert received metrics
            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);
            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.S2C_LoadComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestSceneEventMetrics_UnloadScene_S2C_Unload()
        {
            ////////// ARRANGE //////////

            // We need to load a scene to unload one
            yield return LoadTestScene(SimpleSceneName);

            var serverSceneUnloaded = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType != SceneEventData.SceneEventTypes.S2C_Unload) return;
                serverSceneUnloaded = sceneEvent.AsyncOperation.isDone;
                sceneEvent.AsyncOperation.completed += op => serverSceneUnloaded = true;
            };

            // Now we can start the test!
            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_Unload));

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_Unload));

            ////////// ACT //////////

            // Unload the scene
            Assert.AreEqual(SceneEventProgressStatus.Started, m_ServerNetworkSceneManager.UnloadScene(m_LoadedScene));

            // Don't wait for metrics until the server has finished unloading the scene locally
            for (int i = 0; i < 240; i++)
            {
                if (serverSceneUnloaded)
                    break;
                yield return null;
            }

            Assert.IsTrue(serverSceneUnloaded);


            // Now that the scene has unloaded locally, the message will be sent to the client which is the point
            // the metric is tracked.
            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.S2C_Unload, sentMetric.SceneEventType);
            Assert.AreEqual(m_Client.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, sentMetric.SceneName);

            // Assert received metrics
            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.S2C_Unload, receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestSceneEventMetrics_UnloadScene_C2S_UnLoadComplete()
        {
            ////////// ARRANGE /////////

            // We need to load a scene to unload one
            yield return LoadTestScene(SimpleSceneName);

            var clientSceneUnloaded = false;
            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType == SceneEventData.SceneEventTypes.C2S_UnloadComplete) clientSceneUnloaded = true;
            };

            // Now we can start the test!
            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(ClientMetrics.Dispatcher, NetworkMetricTypes.SceneEventSent);

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(ServerMetrics.Dispatcher, NetworkMetricTypes.SceneEventReceived);

            ////////// ACT //////////

            // Unload the scene
            Assert.AreEqual(SceneEventProgressStatus.Started, m_ServerNetworkSceneManager.UnloadScene(m_LoadedScene));

            // Don't wait for metrics until the server has finished unloading the scene locally
            for (int i = 0; i < 240; i++)
            {
                if (clientSceneUnloaded)
                    break;
                yield return null;
            }

            Assert.IsTrue(clientSceneUnloaded);


            // Now that the scene has unloaded locally, the message will be sent to the client which is the point
            // the metric is tracked.
            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.C2S_UnloadComplete, sentMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, sentMetric.SceneName);

            // Assert received metrics
            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.C2S_UnloadComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(m_Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestSceneEventMetrics_UnloadScene_S2C_UnLoadComplete()
        {
            ////////// ARRANGE //////////

            // We need to load a scene to unload one
            yield return LoadTestScene(SimpleSceneName);

            var serverUnloadComplete = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType == SceneEventData.SceneEventTypes.S2C_UnLoadComplete) serverUnloadComplete = true;
            };

            // Now we can start the test!
            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_UnLoadComplete));

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_UnLoadComplete));

            ////////// ACT //////////

            // Unload the scene
            Assert.AreEqual(SceneEventProgressStatus.Started, m_ServerNetworkSceneManager.UnloadScene(m_LoadedScene));

            // Don't wait for metrics until the server has finished unloading the scene locally
            for (int i = 0; i < 240; i++)
            {
                if (serverUnloadComplete)
                    break;
                yield return null;
            }

            Assert.IsTrue(serverUnloadComplete);


            // Now that the scene has unloaded locally, the message will be sent to the client which is the point
            // the metric is tracked.
            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(Server.ConnectedClients.Count, sentMetrics.Count);

            // message is sent to all connected clients which for the server includes itself.
            foreach (var client in Server.ConnectedClients)
            {
                var check = false;
                foreach (var metric in sentMetrics)
                {
                    // Assert the metric is correct
                    if (!metric.Connection.Id.Equals(client.Key)) continue;
                    check = true;
                    Assert.AreEqual(SceneEventType.S2C_UnLoadComplete, metric.SceneEventType);
                    Assert.AreEqual(client.Key, metric.Connection.Id);
                    Assert.AreEqual(SimpleSceneName, metric.SceneName);
                    break;
                }

                // assert that we found a metric for the given client ID.
                Assert.IsTrue(check);
            }

            // Assert received metrics
            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);
            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.S2C_UnLoadComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestSceneEventMetrics_SyncScenes_S2C_Sync()
        {
            // Load multiple scenes so we can ensure we get a metric for each when syncing
            yield return LoadTestScene(EmptySceneName);

            // Now we can start the test!
            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_Sync));

            MultiInstanceHelpers.CreateNewClients(1, out var newClients);
            var newClient = newClients[0];

            newClient.NetworkConfig.EnableSceneManagement = true;
            newClient.NetworkConfig.PlayerPrefab = m_PlayerPrefab;


            ////////// ACT //////////
            newClient.StartClient();
            var newClientMetrics = newClient.NetworkMetrics as NetworkMetrics;

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                newClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_Sync));

            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            ////////// ASSERT //////////
            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(SceneManager.sceneCount, sentMetrics.Count);

            // One metric is sent for each scene that is synced
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var check = false;
                foreach (var metric in sentMetrics)
                {
                    if (SceneManager.GetSceneAt(i).name != metric.SceneName) continue;
                        // Assert the metric is correct
                        check = true;
                        Assert.AreEqual(SceneEventType.S2C_Sync, metric.SceneEventType);
                        Assert.AreEqual(newClients[0].LocalClientId, metric.Connection.Id);
                        Assert.AreEqual(SceneManager.GetSceneAt(i).name, metric.SceneName);
                        break;
                }

                // assert that we found a metric for the given client ID.
                Assert.IsTrue(check);
            }

            // Assert received metrics
            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(SceneManager.sceneCount, receivedMetrics.Count);

            // One metric is sent for each scene that is synced
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var check = false;
                foreach (var metric in receivedMetrics)
                {
                    if (SceneManager.GetSceneAt(i).name != metric.SceneName) continue;
                    // Assert the metric is correct
                    check = true;
                    Assert.AreEqual(SceneEventType.S2C_Sync, metric.SceneEventType);
                    Assert.AreEqual(Server.LocalClientId, metric.Connection.Id);
                    Assert.AreEqual(SceneManager.GetSceneAt(i).name, metric.SceneName);
                    break;
                }

                // assert that we found a metric for the given client ID.
                Assert.IsTrue(check);
            }
        }

        [UnityTest]
        public IEnumerator TestSceneEventMetrics_SyncScenes_C2S_SyncComplete()
        {
            // Load multiple scenes so we can ensure we get a metric for each when syncing
            yield return LoadTestScene(EmptySceneName);

            // Now we can start the test!
            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.C2S_SyncComplete));

            MultiInstanceHelpers.CreateNewClients(1, out var newClients);
            var newClient = newClients[0];

            newClient.NetworkConfig.EnableSceneManagement = true;
            newClient.NetworkConfig.PlayerPrefab = m_PlayerPrefab;


            ////////// ACT //////////
            newClient.StartClient();
            var newClientMetrics = newClient.NetworkMetrics as NetworkMetrics;

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                newClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.C2S_SyncComplete));

            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            ////////// ASSERT //////////
            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(SceneManager.sceneCount, sentMetrics.Count);

            // One metric is sent for each scene that is synced
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var check = false;
                foreach (var metric in sentMetrics)
                {
                    if (SceneManager.GetSceneAt(i).name != metric.SceneName) continue;
                        // Assert the metric is correct
                        check = true;
                        Assert.AreEqual(SceneEventType.C2S_SyncComplete, metric.SceneEventType);
                        Assert.AreEqual(newClients[0].LocalClientId, metric.Connection.Id);
                        Assert.AreEqual(SceneManager.GetSceneAt(i).name, metric.SceneName);
                        break;
                }

                // assert that we found a metric for the given client ID.
                Assert.IsTrue(check);
            }

            // Assert received metrics
            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(SceneManager.sceneCount, receivedMetrics.Count);

            // One metric is sent for each scene that is synced
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var check = false;
                foreach (var metric in receivedMetrics)
                {
                    if (SceneManager.GetSceneAt(i).name != metric.SceneName) continue;
                    // Assert the metric is correct
                    check = true;
                    Assert.AreEqual(SceneEventType.S2C_Sync, metric.SceneEventType);
                    Assert.AreEqual(Server.LocalClientId, metric.Connection.Id);
                    Assert.AreEqual(SceneManager.GetSceneAt(i).name, metric.SceneName);
                    break;
                }

                // assert that we found a metric for the given client ID.
                Assert.IsTrue(check);
            }
        }

        // Loads a scene, then waits for the client to notify the server
        // that it has finished loading the scene, as this is the last thing that happens.
        private IEnumerator LoadTestScene(string sceneName)
        {
            var sceneLoadComplete = false;
            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType == SceneEventData.SceneEventTypes.S2C_LoadComplete) sceneLoadComplete = true;
            };

            // load the scene
            Assert.AreEqual(SceneEventProgressStatus.Started, m_ServerNetworkSceneManager.LoadScene(sceneName, LoadSceneMode.Additive));
            for (int i = 0; i < 240; i++)
            {
                if (sceneLoadComplete)
                    break;
                yield return null;
            }

            Assert.IsTrue(sceneLoadComplete);
        }
    }
}