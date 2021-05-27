using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MLAPI.Metrics;
using NUnit.Framework;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
#if true
    public class NetworkMetricsTests
    {
        NetworkManager m_NetworkManager;
        NetworkMetrics m_NetworkMetrics => m_NetworkManager.NetworkMetrics as NetworkMetrics;

        [SetUp]
        public void SetUp()
        {
            NetworkManagerHelper.StartNetworkManager(out var networkManager);
            m_NetworkManager = networkManager;
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

        private IEnumerator WaitForMetricsDispatch()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }

        private IEventMetric<TEvent> AssertSingleMetricEventOfType<TEvent>(MetricCollection collection, string name)
        {
            var namedMessageSentMetric = collection.Metrics.SingleOrDefault(x => x.Name == name);
            Assert.NotNull(namedMessageSentMetric);

            var typedMetric = namedMessageSentMetric as IEventMetric<TEvent>;
            Assert.NotNull(typedMetric);
            Assert.IsNotEmpty(typedMetric.Values);

            return typedMetric;
        }

        private class TestObserver : IMetricObserver
        {
            private readonly Action<MetricCollection> m_Assertion;

            public TestObserver(Action<MetricCollection> assertion)
            {
                m_Assertion = assertion;
            }

            public void Observe(MetricCollection collection)
            {
                m_Assertion.Invoke(collection);
            }
        }
    }
#endif
}
