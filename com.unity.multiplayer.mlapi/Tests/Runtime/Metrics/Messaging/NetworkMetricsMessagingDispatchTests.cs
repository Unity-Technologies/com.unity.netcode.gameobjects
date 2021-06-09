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
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.Messaging
{
#if true
    public class NetworkMetricsMessagingDispatchTests : MetricsTestBase
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
        public IEnumerator TrackNamedMessageSentMetric()
        {
            var messageName = Guid.NewGuid().ToString();

            var clientId = 100UL;
            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var namedMessageSentMetric = AssertSingleMetricEventOfType<NamedMessageEvent>(collection, MetricNames.NamedMessageSent);
                Assert.AreEqual(1, namedMessageSentMetric.Values.Count);

                var namedMessageSent = namedMessageSentMetric.Values.First();
                Assert.AreEqual(messageName, namedMessageSent.Name);
                Assert.AreEqual(clientId, namedMessageSent.Connection.Id);
            }));

            m_NetworkManager.CustomMessagingManager.SendNamedMessage(messageName, clientId, Stream.Null);

            yield return WaitForMetricsDispatch();
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageSentMetricToMultipleClients()
        {
            var messageName = Guid.NewGuid().ToString();

            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var namedMessageSentMetric = AssertSingleMetricEventOfType<NamedMessageEvent>(collection, MetricNames.NamedMessageSent);
                Assert.AreEqual(3, namedMessageSentMetric.Values.Count);
                Assert.True(namedMessageSentMetric.Values.All(x => x.Name == messageName));

                var clientIds = namedMessageSentMetric.Values.Select(x => x.Connection.Id).ToList();
                Assert.Contains(100UL, clientIds);
                Assert.Contains(200UL, clientIds);
                Assert.Contains(300UL, clientIds);
            }));

            m_NetworkManager.CustomMessagingManager.SendNamedMessage(messageName, new List<ulong> { 100, 200, 300 }, Stream.Null);

            yield return WaitForMetricsDispatch();
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetric()
        {
            var clientId = 100UL;
            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var unnamedMessageSentMetric = AssertSingleMetricEventOfType<UnnamedMessageEvent>(collection, MetricNames.UnnamedMessageSent);
                Assert.AreEqual(1, unnamedMessageSentMetric.Values.Count);

                var unnamedMessageSent = unnamedMessageSentMetric.Values.First();
                Assert.AreEqual(clientId, unnamedMessageSent.Connection.Id);
            }));

            m_NetworkManager.CustomMessagingManager.SendUnnamedMessage(clientId, new NetworkBuffer());

            yield return WaitForMetricsDispatch();
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetricToMultipleClients()
        {
            m_NetworkMetrics.Dispatcher.RegisterObserver(new TestObserver(collection =>
            {
                var unnamedMessageSentMetric = AssertSingleMetricEventOfType<UnnamedMessageEvent>(collection, MetricNames.UnnamedMessageSent);
                Assert.AreEqual(3, unnamedMessageSentMetric.Values.Count);

                var clientIds = unnamedMessageSentMetric.Values.Select(x => x.Connection.Id).ToList();
                Assert.Contains(100UL, clientIds);
                Assert.Contains(200UL, clientIds);
                Assert.Contains(300UL, clientIds);
            }));

            m_NetworkManager.CustomMessagingManager.SendUnnamedMessage(new List<ulong> { 100, 200, 300 }, new NetworkBuffer());

            yield return WaitForMetricsDispatch();
        }
    }
#endif
}
