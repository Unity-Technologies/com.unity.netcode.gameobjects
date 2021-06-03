using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MLAPI.Metrics;
using MLAPI.Serialization;
using NUnit.Framework;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
#if true
    public class NetworkMetricsDispatchTests : MetricsTestBase
    {
        NetworkManager m_NetworkManager;
        NetworkMetrics m_NetworkMetrics;

        [SetUp]
        public void SetUp()
        {
            NetworkManagerHelper.StartNetworkManager(out m_NetworkManager);
            m_NetworkMetrics = m_NetworkManager.NetworkMetrics as NetworkMetrics;
        }

        [TearDown]
        public void TearDown()
        {
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenNamedMessageSent_TracksNamedMessageSentMetric()
        {
            var messageName = Guid.NewGuid().ToString();

            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var namedMessageSentMetric = AssertSingleMetricEventOfType<NamedMessageEvent>(collection, MetricNames.NamedMessageSent);
                Assert.AreEqual(1, namedMessageSentMetric.Values.Count);

                var namedMessageSent = namedMessageSentMetric.Values.First();
                Assert.AreEqual(messageName, namedMessageSent.Name);
                Assert.AreEqual(m_NetworkManager.LocalClientId, namedMessageSent.Connection.Id);
            }));

            m_NetworkManager.CustomMessagingManager.SendNamedMessage(messageName, 100, Stream.Null);

            yield return WaitForMetricsDispatch();
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenNamedMessageSentToMultipleClients_TracksNamedMessageSentMetric()
        {
            var messageName = Guid.NewGuid().ToString();

            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var namedMessageSentMetric = AssertSingleMetricEventOfType<NamedMessageEvent>(collection, MetricNames.NamedMessageSent);
                Assert.AreEqual(1, namedMessageSentMetric.Values.Count);

                var namedMessageSent = namedMessageSentMetric.Values.First();
                Assert.AreEqual(messageName, namedMessageSent.Name);
                Assert.AreEqual(m_NetworkManager.LocalClientId, namedMessageSent.Connection.Id);
            }));

            m_NetworkManager.CustomMessagingManager.SendNamedMessage(messageName, new List<ulong> { 100, 200, 300 }, Stream.Null);

            yield return WaitForMetricsDispatch();
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenUnnamedMessageSent_TracksNamedMessageSentMetric()
        {
            var messageName = Guid.NewGuid().ToString();

            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var namedMessageSentMetric = AssertSingleMetricEventOfType<UnnamedMessageEvent>(collection, MetricNames.UnnamedMessageSent);
                Assert.AreEqual(1, namedMessageSentMetric.Values.Count);

                var namedMessageSent = namedMessageSentMetric.Values.First();
                Assert.AreEqual(m_NetworkManager.LocalClientId, namedMessageSent.Connection.Id);
            }));

            m_NetworkManager.CustomMessagingManager.SendUnnamedMessage(100, new NetworkBuffer());

            yield return WaitForMetricsDispatch();
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenUnnamedMessageSentToMultipleClients_TracksNamedMessageSentMetric()
        {
            var messageName = Guid.NewGuid().ToString();

            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var namedMessageSentMetric = AssertSingleMetricEventOfType<UnnamedMessageEvent>(collection, MetricNames.UnnamedMessageSent);
                Assert.AreEqual(1, namedMessageSentMetric.Values.Count);

                var namedMessageSent = namedMessageSentMetric.Values.First();
                Assert.AreEqual(m_NetworkManager.LocalClientId, namedMessageSent.Connection.Id);
            }));

            m_NetworkManager.CustomMessagingManager.SendUnnamedMessage(new List<ulong> { 100, 200, 300 }, new NetworkBuffer());

            yield return WaitForMetricsDispatch();
        }

        [UnityTest]
        public IEnumerator NetworkMetrics_WhenSceneSwitchInitiated_TracksSceneSwitchInitiatedMetric()
        {
            var lastSceneName = Guid.NewGuid().ToString();
            var lastScene = SceneManager.CreateScene(lastSceneName);
            var nextSceneName = Guid.NewGuid().ToString();
            var nextScene = SceneManager.CreateScene(nextSceneName);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(lastScene., true),
                new EditorBuildSettingsScene(nextScene.path, true)
            };


            m_NetworkManager.NetworkConfig.AllowRuntimeSceneChanges = true;
            m_NetworkManager.NetworkConfig.EnableSceneManagement = true;

            m_NetworkManager.SceneManager.AddRuntimeSceneName(lastSceneName, 0);
            m_NetworkManager.SceneManager.AddRuntimeSceneName(nextSceneName, 1);

            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var sceneSwitchMetric = AssertSingleMetricEventOfType<SceneSwitchEvent>(collection, MetricNames.SceneSwitchInitiated);
                Assert.AreEqual(1, sceneSwitchMetric.Values.Count);

                var sceneSwitchInitiated = sceneSwitchMetric.Values.First();
                Assert.AreEqual(m_NetworkManager.LocalClientId, sceneSwitchInitiated.Connection.Id);
                Assert.AreEqual(lastSceneName, sceneSwitchInitiated.LastSceneName);
                Assert.AreEqual(nextSceneName, sceneSwitchInitiated.NextSceneName);
            }));


            m_NetworkManager.SceneManager.SwitchScene(nextSceneName);

            yield return WaitForMetricsDispatch();
        }
    }
#endif
}
