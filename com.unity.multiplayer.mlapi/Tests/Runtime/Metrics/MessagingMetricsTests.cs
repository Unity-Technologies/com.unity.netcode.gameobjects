using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MLAPI.Metrics;
using MLAPI.RuntimeTests.Metrics.Utility;
using MLAPI.Serialization;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
    public class MessagingMetricsTests
    {
        const uint MessageNameHashSize = 5;
        const uint MessageContentStringLength = 1;
        const uint MessageTypeLength = 1;

        const uint MessageSentOverhead = MessageNameHashSize + MessageContentStringLength;
        const uint MessageReceivedOverhead = MessageTypeLength + MessageNameHashSize + MessageContentStringLength;

        NetworkManager m_Server;
        NetworkMetrics m_ServerMetrics;
        NetworkManager m_FirstClient;
        NetworkMetrics m_FirstClientMetrics;
        NetworkManager m_SecondClient;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var initializer = new DualClientMetricTestInitializer();

            yield return initializer.Initialize();

            m_Server = initializer.Server;
            m_FirstClient = initializer.FirstClient;
            m_SecondClient = initializer.SecondClient;
            m_ServerMetrics = initializer.ServerMetrics;
            m_FirstClientMetrics = initializer.FirstClientMetrics;
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
            using var memoryStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(messageName);

            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(m_ServerMetrics.Dispatcher, MetricNames.NamedMessageSent);

            m_Server.CustomMessagingManager.SendNamedMessage(messageName, m_FirstClient.LocalClientId, memoryStream);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var namedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, namedMessageSentMetricValues.Count);

            var namedMessageSent = namedMessageSentMetricValues.First();
            Assert.AreEqual(messageName, namedMessageSent.Name);
            Assert.AreEqual(m_FirstClient.LocalClientId, namedMessageSent.Connection.Id);
            Assert.AreEqual(messageName.Length + MessageSentOverhead, namedMessageSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageSentMetricToMultipleClients()
        {
            var messageName = Guid.NewGuid().ToString();
            using var memoryStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(messageName);

            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(m_ServerMetrics.Dispatcher, MetricNames.NamedMessageSent);
            m_Server.CustomMessagingManager.SendNamedMessage(messageName, new List<ulong> { m_FirstClient.LocalClientId, m_SecondClient.LocalClientId }, memoryStream);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var namedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, namedMessageSentMetricValues.Count);
            Assert.That(namedMessageSentMetricValues, Has.All.Matches<NamedMessageEvent>(x => x.Name == messageName));
            Assert.That(namedMessageSentMetricValues, Has.All.Matches<NamedMessageEvent>(x => x.BytesCount == messageName.Length + MessageSentOverhead));
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageReceivedMetric()
        {
            var messageName = Guid.NewGuid().ToString();
            using var memoryStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(messageName);

            LogAssert.Expect(LogType.Log, $"Received from {m_Server.LocalClientId}");
            m_FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(messageName, (sender, payload) =>
            {
                Debug.Log($"Received from {sender}");
            });

            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(m_FirstClientMetrics.Dispatcher, MetricNames.NamedMessageReceived);

            m_Server.CustomMessagingManager.SendNamedMessage(messageName, m_FirstClient.LocalClientId, memoryStream);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var namedMessageReceivedValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, namedMessageReceivedValues.Count);

            var namedMessageReceived = namedMessageReceivedValues.First();
            Assert.AreEqual(messageName, namedMessageReceived.Name);
            Assert.AreEqual(m_Server.LocalClientId, namedMessageReceived.Connection.Id);
            Assert.AreEqual(messageName.Length + MessageReceivedOverhead, namedMessageReceived.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetric()
        {
            var message = Guid.NewGuid().ToString();
            using var buffer = new NetworkBuffer();
            buffer.Write(Encoding.UTF8.GetBytes(message));

            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(m_ServerMetrics.Dispatcher, MetricNames.UnnamedMessageSent);
            m_Server.CustomMessagingManager.SendUnnamedMessage(m_FirstClient.LocalClientId, buffer);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var unnamedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, unnamedMessageSentMetricValues.Count);

            var unnamedMessageSent = unnamedMessageSentMetricValues.First();
            Assert.AreEqual(m_FirstClient.LocalClientId, unnamedMessageSent.Connection.Id);
            Assert.AreEqual(message.Length, unnamedMessageSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetricToMultipleClients()
        {
            var message = Guid.NewGuid().ToString();
            using var buffer = new NetworkBuffer();
            buffer.Write(Encoding.UTF8.GetBytes(message));

            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(m_ServerMetrics.Dispatcher, MetricNames.UnnamedMessageSent);
            m_Server.CustomMessagingManager.SendUnnamedMessage(new List<ulong> { m_FirstClient.LocalClientId, m_SecondClient.LocalClientId }, buffer);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var unnamedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, unnamedMessageSentMetricValues.Count);
            Assert.That(unnamedMessageSentMetricValues, Has.All.Matches<UnnamedMessageEvent>(x => x.BytesCount == message.Length));

            var clientIds = unnamedMessageSentMetricValues.Select(x => x.Connection.Id).ToList();
            Assert.Contains(m_FirstClient.LocalClientId, clientIds);
            Assert.Contains(m_SecondClient.LocalClientId, clientIds);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageReceivedMetric()
        {
            var message = Guid.NewGuid().ToString();
            using var buffer = new NetworkBuffer();
            buffer.Write(Encoding.UTF8.GetBytes(message));

            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(m_FirstClientMetrics.Dispatcher, MetricNames.UnnamedMessageReceived);

            m_Server.CustomMessagingManager.SendUnnamedMessage(m_FirstClient.LocalClientId, buffer);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var unnamedMessageReceivedValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, unnamedMessageReceivedValues.Count);

            var unnamedMessageReceived = unnamedMessageReceivedValues.First();
            Assert.AreEqual(m_Server.LocalClientId, unnamedMessageReceived.Connection.Id);
            Assert.AreEqual(message.Length + MessageTypeLength, unnamedMessageReceived.BytesCount);
        }
    }
}
