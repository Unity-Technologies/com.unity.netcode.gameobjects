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
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.Messaging
{
#if true
    public class NetworkMetricsMessagingDispatchTests
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

            var waitForMetricEvent = new WaitForMetricValues<NamedMessageEvent>(m_NetworkMetrics.Dispatcher, MetricNames.NamedMessageSent);
            m_NetworkManager.CustomMessagingManager.SendNamedMessage(messageName, clientId, Stream.Null);

            yield return waitForMetricEvent.WaitForMetricsDispatch();

            var namedMessageSentMetricValues = waitForMetricEvent.Values;
            Assert.AreEqual(1, namedMessageSentMetricValues.Count);

            var namedMessageSent = namedMessageSentMetricValues.First();
            Assert.AreEqual(messageName, namedMessageSent.Name);
            Assert.AreEqual(clientId, namedMessageSent.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageSentMetricToMultipleClients()
        {
            var messageName = Guid.NewGuid().ToString();

            var waitForMetricEvent = new WaitForMetricValues<NamedMessageEvent>(m_NetworkMetrics.Dispatcher, MetricNames.NamedMessageSent);
            m_NetworkManager.CustomMessagingManager.SendNamedMessage(messageName, new List<ulong> { 100, 200, 300 }, Stream.Null);

            yield return waitForMetricEvent.WaitForMetricsDispatch();

            var namedMessageSentMetricValues = waitForMetricEvent.Values;
            Assert.AreEqual(3, namedMessageSentMetricValues.Count);
            Assert.True(namedMessageSentMetricValues.All(x => x.Name == messageName));

            var clientIds = namedMessageSentMetricValues.Select(x => x.Connection.Id).ToList();
            Assert.Contains(100UL, clientIds);
            Assert.Contains(200UL, clientIds);
            Assert.Contains(300UL, clientIds);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetric()
        {
            var clientId = 100UL;

            var waitForMetricEvent = new WaitForMetricValues<UnnamedMessageEvent>(m_NetworkMetrics.Dispatcher, MetricNames.UnnamedMessageSent);
            m_NetworkManager.CustomMessagingManager.SendUnnamedMessage(clientId, new NetworkBuffer());

            yield return waitForMetricEvent.WaitForMetricsDispatch();

            var unnamedMessageSentMetricValues = waitForMetricEvent.Values;
            Assert.AreEqual(1, unnamedMessageSentMetricValues.Count);

            var unnamedMessageSent = unnamedMessageSentMetricValues.First();
            Assert.AreEqual(clientId, unnamedMessageSent.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetricToMultipleClients()
        {
            var waitForMetricEvent = new WaitForMetricValues<UnnamedMessageEvent>(m_NetworkMetrics.Dispatcher, MetricNames.UnnamedMessageSent);
            m_NetworkManager.CustomMessagingManager.SendUnnamedMessage(new List<ulong> { 100, 200, 300 }, new NetworkBuffer());

            yield return waitForMetricEvent.WaitForMetricsDispatch();

            var unnamedMessageSentMetricValues = waitForMetricEvent.Values;
            Assert.AreEqual(3, unnamedMessageSentMetricValues.Count);

            var clientIds = unnamedMessageSentMetricValues.Select(x => x.Connection.Id).ToList();
            Assert.Contains(100UL, clientIds);
            Assert.Contains(200UL, clientIds);
            Assert.Contains(300UL, clientIds);
        }
    }
#endif
}