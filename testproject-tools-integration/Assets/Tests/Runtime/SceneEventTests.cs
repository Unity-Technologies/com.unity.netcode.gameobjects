using System.Collections;
using System.Linq;
using NUnit.Framework;
using TestProject.ToolsIntegration.RuntimeTests.Utility;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.ToolsIntegration.RuntimeTests
{
    public class SceneEventTests : SingleClientMetricTestBase
    {
        // scenes referenced in this test must also be in the build settings of the project.
        private const string SimpleSceneName = "SimpleScene";
        // private const string EmptySceneName = "EmptyScene";

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
            m_ServerNetworkSceneManager = m_ServerNetworkManager.SceneManager;
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = true;

            // For some reason there is code in the NetworkSceneManager that if you are running unit tests,
            // the scenes must be synchronised between server and client as below - the test throws an exception
            // otherwise.
            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventData.SceneEventTypes.S2C_Load))
                {
                    m_LoadedScene = SceneManager.GetSceneByName(sceneEvent.SceneName);
                    if (m_ClientNetworkSceneManager.ScenesLoaded.ContainsKey(m_LoadedScene.handle))
                        return;
                    m_ClientNetworkSceneManager.ScenesLoaded.Add(m_LoadedScene.handle, m_LoadedScene);
                    m_ClientNetworkSceneManager.ServerSceneHandleToClientSceneHandle.Add(sceneEvent.Scene.handle, sceneEvent.Scene.handle);
                }
            };
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            m_ServerNetworkSceneManager.UnloadScene(m_LoadedScene);
            return base.Teardown();
        }

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
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMetric.Connection.Id);
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

            // Now that the scene has loaded locally, the message will be sent to the client which is the point
            // the metric is tracked.
            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.C2S_LoadComplete, sentMetric.SceneEventType);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, sentMetric.Connection.Id);
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

            // Now that the scene has loaded locally, the message will be sent to the client which is the point
            // the metric is tracked.
            yield return waitForSentMetric.WaitForMetricsReceived();
            yield return waitForReceivedMetric.WaitForMetricsReceived();

            // Assert sent metrics
            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(m_ServerNetworkManager.ConnectedClients.Count, sentMetrics.Count);

            // message is sent to all connected clients which for the server includes itself.
            foreach (var client in m_ServerNetworkManager.ConnectedClients)
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
                Assert.True(check);
            }

            // Assert received metrics
            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);
            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.S2C_LoadComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestSceneEventMetrics_UnloadScene_S2C_Unload()
        {
            ////////// ARRANGE //////////

            // We need to load a scene to unload one, so wait for the server to be notified that the client has
            // completed loading the seen, as this is the last thing that happens.
            var sceneLoadComplete = false;
            var serverSceneUnloaded = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                switch (sceneEvent.SceneEventType)
                {
                    case SceneEventData.SceneEventTypes.S2C_LoadComplete when sceneEvent.ClientId.Equals(m_Client.LocalClientId):
                        sceneLoadComplete = true;
                        break;
                    case SceneEventData.SceneEventTypes.S2C_Unload:
                        serverSceneUnloaded = sceneEvent.AsyncOperation.isDone;
                        sceneEvent.AsyncOperation.completed += op => serverSceneUnloaded = true;
                        break;
                }
            };

            Assert.AreEqual(SceneEventProgressStatus.Started, m_ServerNetworkSceneManager.LoadScene(SimpleSceneName, LoadSceneMode.Additive));
            for (int i = 0; i < 240; i++)
            {
                if (sceneLoadComplete)
                    break;
                yield return null;
            }

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
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);

        }
    }
}