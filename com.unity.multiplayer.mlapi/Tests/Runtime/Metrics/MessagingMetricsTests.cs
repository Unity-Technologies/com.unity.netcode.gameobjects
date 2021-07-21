using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MLAPI.Metrics;
using MLAPI.Serialization;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
    public class MessagingMetricsTests
    {
        NetworkManager m_Server;
        NetworkMetrics m_ServerMetrics;
        NetworkManager m_FirstClient;
        NetworkMetrics m_FirstClientMetrics;
        NetworkManager m_SecondClient;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!MultiInstanceHelpers.Create(2, out m_Server, out var clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            if (!MultiInstanceHelpers.Start(true, m_Server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(m_Server));

            m_FirstClient = clients[0];
            m_SecondClient = clients[1];
            m_ServerMetrics = m_Server.NetworkMetrics as NetworkMetrics;
            m_FirstClientMetrics = m_FirstClient.NetworkMetrics as NetworkMetrics;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MultiInstanceHelpers.Destroy();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageSentMetric()
        {
            var messageName = Guid.NewGuid().ToString();

            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(m_ServerMetrics.Dispatcher, MetricNames.NamedMessageSent);
            m_Server.CustomMessagingManager.SendNamedMessage(messageName, m_FirstClient.LocalClientId, Stream.Null);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var namedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, namedMessageSentMetricValues.Count);

            var namedMessageSent = namedMessageSentMetricValues.First();
            Assert.AreEqual(messageName, namedMessageSent.Name);
            Assert.AreEqual(m_FirstClient.LocalClientId, namedMessageSent.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageSentMetricToMultipleClients()
        {
            var messageName = Guid.NewGuid().ToString();

            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(m_ServerMetrics.Dispatcher, MetricNames.NamedMessageSent);
            m_Server.CustomMessagingManager.SendNamedMessage(messageName, new List<ulong> { m_FirstClient.LocalClientId, m_SecondClient.LocalClientId }, Stream.Null);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var namedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, namedMessageSentMetricValues.Count);
            Assert.True(namedMessageSentMetricValues.All(x => x.Name == messageName));

            var clientIds = namedMessageSentMetricValues.Select(x => x.Connection.Id).ToList();
            Assert.Contains(m_FirstClient.LocalClientId, clientIds);
            Assert.Contains(m_SecondClient.LocalClientId, clientIds);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(m_ServerMetrics.Dispatcher, MetricNames.UnnamedMessageSent);
            m_Server.CustomMessagingManager.SendUnnamedMessage(m_FirstClient.LocalClientId, new NetworkBuffer());

            yield return waitForMetricValues.WaitForMetricsReceived();

            var unnamedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, unnamedMessageSentMetricValues.Count);

            var unnamedMessageSent = unnamedMessageSentMetricValues.First();
            Assert.AreEqual(m_FirstClient.LocalClientId, unnamedMessageSent.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetricToMultipleClients()
        {
            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(m_ServerMetrics.Dispatcher, MetricNames.UnnamedMessageSent);
            m_Server.CustomMessagingManager.SendUnnamedMessage(new List<ulong> { m_FirstClient.LocalClientId, m_SecondClient.LocalClientId }, new NetworkBuffer());

            yield return waitForMetricValues.WaitForMetricsReceived();

            var unnamedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, unnamedMessageSentMetricValues.Count);

            var clientIds = unnamedMessageSentMetricValues.Select(x => x.Connection.Id).ToList();
            Assert.Contains(m_FirstClient.LocalClientId, clientIds);
            Assert.Contains(m_SecondClient.LocalClientId, clientIds);
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageReceivedMetric()
        {
            var messageName = Guid.NewGuid().ToString();
            LogAssert.Expect(LogType.Log, $"Received from {m_Server.LocalClientId}");
            m_FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(messageName, (sender, payload) =>
            {
                Debug.Log($"Received from {sender}");
            });

            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(m_FirstClientMetrics.Dispatcher, MetricNames.NamedMessageReceived);

            m_Server.CustomMessagingManager.SendNamedMessage(messageName, m_FirstClient.LocalClientId, Stream.Null);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var namedMessageReceivedValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, namedMessageReceivedValues.Count);

            var namedMessageReceived = namedMessageReceivedValues.First();
            Assert.AreEqual(messageName, namedMessageReceived.Name);
            Assert.AreEqual(m_Server.LocalClientId, namedMessageReceived.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageReceivedMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(m_FirstClientMetrics.Dispatcher, MetricNames.UnnamedMessageReceived);

            m_Server.CustomMessagingManager.SendUnnamedMessage(m_FirstClient.LocalClientId, new NetworkBuffer());

            yield return waitForMetricValues.WaitForMetricsReceived();

            var unnamedMessageReceivedValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, unnamedMessageReceivedValues.Count);

            var unnamedMessageReceived = unnamedMessageReceivedValues.First();
            Assert.AreEqual(m_Server.LocalClientId, unnamedMessageReceived.Connection.Id);
        }
    }
}
