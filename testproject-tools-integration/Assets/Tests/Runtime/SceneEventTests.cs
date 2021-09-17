using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;
using Unity.Netcode.RuntimeTests.Metrics.Utlity;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.ToolsIntegration.RuntimeTests
{
    public class SceneEventTests : SingleClientMetricTestBase
    {
        // scenes referenced in this test must also be in the build settings of the project.
        private const string SimpleSceneName = "SimpleScene";

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

            m_ClientNetworkSceneManager.OnSceneEvent += RegisterLoadedSceneCallback;
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            yield return UnloadTestScene(m_LoadedScene);
            yield return base.Teardown();
        }

        [UnityTest]
        public IEnumerator TestS2CLoadSent()
        {
            var serverSceneLoaded = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventData.SceneEventTypes.S2C_Load))
                {
                    serverSceneLoaded = sceneEvent.AsyncOperation.isDone;
                    sceneEvent.AsyncOperation.completed += _ => serverSceneLoaded = true;
                }
            };

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(ServerMetrics.Dispatcher, NetworkMetricTypes.SceneEventSent);

            StartServerLoadScene();

            yield return WaitForCondition(() => serverSceneLoaded);
            Assert.IsTrue(serverSceneLoaded);

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.S2C_Load, sentMetric.SceneEventType);
            Assert.AreEqual(Client.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, sentMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CLoadReceived()
        {
            var serverSceneLoaded = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventData.SceneEventTypes.S2C_Load))
                {
                    serverSceneLoaded = sceneEvent.AsyncOperation.isDone;
                    sceneEvent.AsyncOperation.completed += _ => serverSceneLoaded = true;
                }
            };

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(ClientMetrics.Dispatcher, NetworkMetricTypes.SceneEventReceived);

            StartServerLoadScene();

            yield return WaitForCondition(() => serverSceneLoaded);
            Assert.IsTrue(serverSceneLoaded);

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.S2C_Load, receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestC2SLoadCompleteSent()
        {
            var waitForClientLoadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventData.SceneEventTypes.C2S_LoadComplete);

            StartServerLoadScene();

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(ClientMetrics.Dispatcher, NetworkMetricTypes.SceneEventSent);

            yield return waitForClientLoadComplete.Wait();
            Assert.IsTrue(waitForClientLoadComplete.Done);

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.C2S_LoadComplete, sentMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, sentMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestC2SLoadCompleteReceived()
        {
            var waitForClientLoadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventData.SceneEventTypes.C2S_LoadComplete);

            StartServerLoadScene();

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(ServerMetrics.Dispatcher, NetworkMetricTypes.SceneEventReceived);

            yield return waitForClientLoadComplete.Wait();
            Assert.IsTrue(waitForClientLoadComplete.Done);

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.C2S_LoadComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CLoadCompleteSent()
        {
            var waitForServerLoadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventData.SceneEventTypes.S2C_LoadComplete);

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_LoadComplete));

            StartServerLoadScene();

            yield return waitForServerLoadComplete.Wait();
            Assert.IsTrue(waitForServerLoadComplete.Done);

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(Server.ConnectedClients.Count, sentMetrics.Count);

            var filteredSentMetrics = sentMetrics
                .Where(metric => metric.SceneEventType == SceneEventType.S2C_LoadComplete)
                .Where(metric => metric.SceneName == SimpleSceneName);
            CollectionAssert.AreEquivalent(filteredSentMetrics.Select(x => x.Connection.Id), Server.ConnectedClients.Select(x => x.Key));
        }

        [UnityTest]
        public IEnumerator TestS2CLoadCompleteReceived()
        {
            var waitForServerLoadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventData.SceneEventTypes.S2C_LoadComplete);

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_LoadComplete));

            StartServerLoadScene();

            yield return waitForServerLoadComplete.Wait();
            Assert.IsTrue(waitForServerLoadComplete.Done);

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);
            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.S2C_LoadComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CUnloadSent()
        {
            yield return LoadTestScene(SimpleSceneName);

            var serverSceneUnloaded = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType != SceneEventData.SceneEventTypes.S2C_Unload)
                {
                    return;
                }

                serverSceneUnloaded = sceneEvent.AsyncOperation.isDone;
                sceneEvent.AsyncOperation.completed += _ => serverSceneUnloaded = true;
            };

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_Unload));

            StartServerUnloadScene();

            yield return WaitForCondition(() => serverSceneUnloaded);

            Assert.IsTrue(serverSceneUnloaded);

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.S2C_Unload, sentMetric.SceneEventType);
            Assert.AreEqual(Client.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, sentMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CUnloadReceived()
        {
            yield return LoadTestScene(SimpleSceneName);

            var serverSceneUnloaded = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType != SceneEventData.SceneEventTypes.S2C_Unload)
                {
                    return;
                }

                serverSceneUnloaded = sceneEvent.AsyncOperation.isDone;
                sceneEvent.AsyncOperation.completed += _ => serverSceneUnloaded = true;
            };

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_Unload));

            StartServerUnloadScene();

            yield return WaitForCondition(() => serverSceneUnloaded);

            Assert.IsTrue(serverSceneUnloaded);

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.S2C_Unload, receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestC2SUnloadCompleteSent()
        {
            yield return LoadTestScene(SimpleSceneName);

            var waitForClientUnloadComplete = new WaitForSceneEvent(
                m_ClientNetworkSceneManager,
                SceneEventData.SceneEventTypes.C2S_UnloadComplete);

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.C2S_UnloadComplete));

            StartServerUnloadScene();

            yield return waitForClientUnloadComplete.Wait();
            Assert.IsTrue(waitForClientUnloadComplete.Done);

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(SceneEventType.C2S_UnloadComplete, sentMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, sentMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestC2SUnloadCompleteReceived()
        {
            yield return LoadTestScene(SimpleSceneName);

            var waitForClientUnloadComplete = new WaitForSceneEvent(
                m_ClientNetworkSceneManager,
                SceneEventData.SceneEventTypes.C2S_UnloadComplete);

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.C2S_UnloadComplete));

            StartServerUnloadScene();

            yield return waitForClientUnloadComplete.Wait();
            Assert.IsTrue(waitForClientUnloadComplete.Done);

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(SceneEventType.C2S_UnloadComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CUnloadCompleteSent()
        {
            yield return LoadTestScene(SimpleSceneName);

            var waitForServerUnloadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventData.SceneEventTypes.S2C_UnLoadComplete);

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_UnLoadComplete));

            StartServerUnloadScene();

            yield return waitForServerUnloadComplete.Wait();
            Assert.IsTrue(waitForServerUnloadComplete.Done);

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(Server.ConnectedClients.Count, sentMetrics.Count);

            var filteredSentMetrics = sentMetrics
                .Where(metric => metric.SceneEventType == SceneEventType.S2C_UnLoadComplete)
                .Where(metric => metric.SceneName == SimpleSceneName);
            CollectionAssert.AreEquivalent(filteredSentMetrics.Select(x => x.Connection.Id), Server.ConnectedClients.Select(x => x.Key));
        }

        [UnityTest]
        public IEnumerator TestS2CUnloadCompleteReceived()
        {
            yield return LoadTestScene(SimpleSceneName);

            var waitForServerUnloadComplete = new WaitForSceneEvent(
                m_ServerNetworkSceneManager,
                SceneEventData.SceneEventTypes.S2C_UnLoadComplete);

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_UnLoadComplete));

            StartServerUnloadScene();

            yield return waitForServerUnloadComplete.Wait();
            Assert.IsTrue(waitForServerUnloadComplete.Done);

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);
            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.S2C_UnLoadComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual(SimpleSceneName, receivedMetric.SceneName);
        }

        [UnityTest]
        public IEnumerator TestS2CSyncSent()
        {
            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_Sync));

            MultiInstanceHelpers.CreateNewClients(1, out var newClients);
            var newClient = newClients[0];

            newClient.NetworkConfig.EnableSceneManagement = true;
            newClient.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            newClient.StartClient();

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);
            var sentMetric = sentMetrics.First();

            Assert.AreEqual(SceneEventType.S2C_Sync, sentMetric.SceneEventType);
            Assert.AreEqual(newClients[0].LocalClientId, sentMetric.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TestS2CSyncReceived()
        {
            MultiInstanceHelpers.CreateNewClients(1, out var newClients);
            var newClient = newClients[0];

            newClient.NetworkConfig.EnableSceneManagement = true;
            newClient.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            newClient.StartClient();
            var newClientMetrics = newClient.NetworkMetrics as NetworkMetrics;

            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                newClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.S2C_Sync));

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);
            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.S2C_Sync, receivedMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, receivedMetric.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TestC2SSyncCompleteSent()
        {
            MultiInstanceHelpers.CreateNewClients(1, out var newClients);
            var newClient = newClients[0];

            newClient.NetworkConfig.EnableSceneManagement = true;
            newClient.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            newClient.StartClient();
            var newClientMetrics = newClient.NetworkMetrics as NetworkMetrics;

            var waitForSentMetric = new WaitForMetricValues<SceneEventMetric>(
                newClientMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.C2S_SyncComplete));

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);
            var sentMetric = sentMetrics.First();

            Assert.AreEqual(SceneEventType.C2S_SyncComplete, sentMetric.SceneEventType);
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TestC2SSyncCompleteReceived()
        {
            var waitForReceivedMetric = new WaitForMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventReceived,
                metric => metric.SceneEventType.Equals(SceneEventType.C2S_SyncComplete));

            MultiInstanceHelpers.CreateNewClients(1, out var newClients);
            var newClient = newClients[0];

            newClient.NetworkConfig.EnableSceneManagement = true;
            newClient.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            newClient.StartClient();

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();

            Assert.AreEqual(SceneEventType.C2S_SyncComplete, receivedMetric.SceneEventType);
            Assert.AreEqual(newClient.LocalClientId, receivedMetric.Connection.Id);
        }

        private void StartServerLoadScene()
        {
            var loadSceneResult = m_ServerNetworkSceneManager.LoadScene(SimpleSceneName, LoadSceneMode.Additive);
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
            var sceneLoadComplete = false;
            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType == SceneEventData.SceneEventTypes.S2C_LoadComplete)
                {
                    sceneLoadComplete = true;
                }
            };

            var loadSceneResult = m_ServerNetworkSceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            Assert.AreEqual(SceneEventProgressStatus.Started, loadSceneResult);

            yield return WaitForCondition(() => sceneLoadComplete);

            Assert.IsTrue(sceneLoadComplete);
        }

        // Unloads a loaded scene. If the scene is not loaded, this is a no-op
        private IEnumerator UnloadTestScene(Scene scene)
        {
            if (!scene.isLoaded) yield break;

            m_ServerNetworkSceneManager.UnloadScene(scene);
            var sceneUnloaded = false;
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType == SceneEventData.SceneEventTypes.C2S_UnloadComplete)
                {
                    sceneUnloaded = true;
                }
            };

            yield return WaitForCondition(() => sceneUnloaded);

            Assert.IsTrue(sceneUnloaded);
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

        private void RegisterLoadedSceneCallback(SceneEvent sceneEvent)
        {
            if (!sceneEvent.SceneEventType.Equals(SceneEventData.SceneEventTypes.S2C_Load))
            {
                return;
            }

            m_LoadedScene = SceneManager.GetSceneByName(sceneEvent.SceneName);
            if (m_ClientNetworkSceneManager.ScenesLoaded.ContainsKey(m_LoadedScene.handle))
            {
                return;
            }

            // As we are running the client and the server using the multi-instance test runner we need to sync the
            // scene handles manually here, as they share a SceneManager.
            m_ClientNetworkSceneManager.ScenesLoaded.Add(m_LoadedScene.handle, m_LoadedScene);
            m_ClientNetworkSceneManager.ServerSceneHandleToClientSceneHandle.Add(m_LoadedScene.handle, m_LoadedScene.handle);
        }

        private class WaitForSceneEvent
        {
            public WaitForSceneEvent(NetworkSceneManager sceneManager, SceneEventData.SceneEventTypes sceneEventType)
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
    }
}