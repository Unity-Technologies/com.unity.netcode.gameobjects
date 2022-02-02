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

            yield return UnloadTestScene(m_LoadedScene);
            yield return base.Teardown();
        }

        [UnityTest]
        public IEnumerator TestS2CLoadSent()
        {
            var serverSceneLoaded = false;
            // Register a callback so we know when the scene has loaded server side, as this is when
            // the message is sent to the client. AsyncOperation is the ScceneManager.LoadSceneAsync operation.
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventType.LoadComplete) && sceneEvent.ClientId == Server.LocalClientId)
                {
                    serverSceneLoaded = true;
                }
            };

            var waitForSentMetric = new WaitForEventMetricValues<SceneEventMetric>(ServerMetrics.Dispatcher, NetworkMetricTypes.SceneEventSent);

            // Load a scene to trigger the messages
            StartServerLoadScene();

            // Wait for the server to load the scene locally first.
            yield return WaitForCondition(() => serverSceneLoaded);
            Assert.IsTrue(serverSceneLoaded);

            // Now start the wait for the metric to be emitted when the message is sent.
            yield return waitForSentMetric.WaitForMetricsReceived();

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
            var serverSceneLoaded = false;
            // Register a callback so we know when the scene has loaded server side, as this is when
            // the message is sent to the client. AsyncOperation is the ScceneManager.LoadSceneAsync operation.
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventType.Load))
                {
                    serverSceneLoaded = sceneEvent.AsyncOperation.isDone;
                    sceneEvent.AsyncOperation.completed += _ => serverSceneLoaded = true;
                }
            };

            var waitForReceivedMetric = new WaitForEventMetricValues<SceneEventMetric>(ClientMetrics.Dispatcher, NetworkMetricTypes.SceneEventReceived);

            // Load a scene to trigger the messages
            StartServerLoadScene();

            // Wait for the server to load the scene locally first.
            yield return WaitForCondition(() => serverSceneLoaded);
            Assert.IsTrue(serverSceneLoaded);

            // Now start the wait for the metric to be emitted when the message is received.
            yield return waitForReceivedMetric.WaitForMetricsReceived();

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

            var waitForSentMetric = new WaitForEventMetricValues<SceneEventMetric>(ClientMetrics.Dispatcher, NetworkMetricTypes.SceneEventSent);

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

            var waitForReceivedMetric = new WaitForEventMetricValues<SceneEventMetric>(ServerMetrics.Dispatcher, NetworkMetricTypes.SceneEventReceived);

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

            var waitForSentMetric = new WaitForEventMetricValues<SceneEventMetric>(
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

            var waitForReceivedMetric = new WaitForEventMetricValues<SceneEventMetric>(
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
            // Register a callback so we can notify the test when the scene has started to unload server side
            // as this is when the message is sent
            m_ServerNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType.Equals(SceneEventType.UnloadComplete) && sceneEvent.ClientId == Server.LocalClientId)
                {
                    serverSceneUnloaded = true;
                }
            };

            var waitForSentMetric = new WaitForEventMetricValues<SceneEventMetric>(
                ServerMetrics.Dispatcher,
                NetworkMetricTypes.SceneEventSent,
                metric => metric.SceneEventType.Equals(SceneEventType.Unload.ToString()));

            // Unload the scene to trigger the messages
            StartServerUnloadScene();

            // Wait for the scene to unload locally
            yield return WaitForCondition(() => serverSceneUnloaded);
            Assert.IsTrue(serverSceneUnloaded);

            // Now wait for the metric to be emitted when the message is sent
            yield return waitForSentMetric.WaitForMetricsReceived();

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

            var waitForReceivedMetric = new WaitForEventMetricValues<SceneEventMetric>(
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

            var waitForSentMetric = new WaitForEventMetricValues<SceneEventMetric>(
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

            var waitForReceivedMetric = new WaitForEventMetricValues<SceneEventMetric>(
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

            var waitForSentMetric = new WaitForEventMetricValues<SceneEventMetric>(
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

            var waitForReceivedMetric = new WaitForEventMetricValues<SceneEventMetric>(
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

            var waitForSentMetric = new WaitForEventMetricValues<SceneEventMetric>(
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

            var waitForReceivedMetric = new WaitForEventMetricValues<SceneEventMetric>(
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

            var waitForSentMetric = new WaitForEventMetricValues<SceneEventMetric>(
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
            var waitForReceivedMetric = new WaitForEventMetricValues<SceneEventMetric>(
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
            var sceneLoadComplete = false;
            m_ClientNetworkSceneManager.OnSceneEvent += sceneEvent =>
            {
                if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
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

        // Registers a callback for the client's NetworkSceneManager which will synchronize the scene handles from
        // the server to the client. This only needs to be done in multi-instance unit tests as the client and the
        // server share a (Unity) SceneManager.
        private void RegisterLoadedSceneCallback(SceneEvent sceneEvent)
        {
            if (!sceneEvent.SceneEventType.Equals(SceneEventType.Load))
            {
                return;
            }

            m_LoadedScene = SceneManager.GetSceneByName(sceneEvent.SceneName);
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
    }
}
