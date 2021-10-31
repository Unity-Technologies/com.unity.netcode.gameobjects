#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    public class MessagingMetricsTests : DualClientMetricTestBase
    {
        private const uint k_MessageNameHashSize = 8;
        private static readonly int k_NamedMessageOverhead = (int)k_MessageNameHashSize + FastBufferWriter.GetWriteSize<MessageHeader>();
        private static readonly int k_UnnamedMessageOverhead = FastBufferWriter.GetWriteSize<MessageHeader>();

        protected override int NbClients => 2;

        [UnityTest]
        public IEnumerator TrackNetworkMessageSentMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<NetworkMessageEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.NetworkMessageSent);

            var messageName = Guid.NewGuid();
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.ToString(), FirstClient.LocalClientId, writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var networkMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, networkMessageSentMetricValues.Count);

            var networkMessageEvent = networkMessageSentMetricValues.First();
            Assert.AreEqual(nameof(NamedMessage), networkMessageEvent.Name);
            Assert.AreEqual(FirstClient.LocalClientId, networkMessageEvent.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackNetworkMessageSentMetricToMultipleClients()
        {
            var waitForMetricValues = new WaitForMetricValues<NetworkMessageEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.NetworkMessageSent);
            var messageName = Guid.NewGuid();
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.ToString(), new List<ulong> { FirstClient.LocalClientId, SecondClient.LocalClientId }, writer);
            }


            yield return waitForMetricValues.WaitForMetricsReceived();

            var networkMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, networkMessageSentMetricValues.Count(x => x.Name.Equals(nameof(NamedMessage))));
        }

        [UnityTest]
        public IEnumerator TrackNetworkMessageReceivedMetric()
        {
            var messageName = Guid.NewGuid();

            LogAssert.Expect(LogType.Log, $"Received from {Server.LocalClientId}");
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(messageName.ToString(), (ulong sender, FastBufferReader payload) =>
            {
                Debug.Log($"Received from {sender}");
            });
            var waitForMetricValues = new WaitForMetricValues<NetworkMessageEvent>(FirstClientMetrics.Dispatcher, NetworkMetricTypes.NetworkMessageReceived);
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.ToString(), FirstClient.LocalClientId, writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var networkMessageReceivedValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, networkMessageReceivedValues.Count(x => x.Name.Equals(nameof(NamedMessage))));

            var namedMessageReceived = networkMessageReceivedValues.First();
            Assert.AreEqual(Server.LocalClientId, namedMessageReceived.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageSentMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.NamedMessageSent);

            var messageName = Guid.NewGuid();
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.ToString(), FirstClient.LocalClientId, writer);
            }


            yield return waitForMetricValues.WaitForMetricsReceived();

            var namedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, namedMessageSentMetricValues.Count);

            var namedMessageSent = namedMessageSentMetricValues.First();
            Assert.AreEqual(messageName.ToString(), namedMessageSent.Name);
            Assert.AreEqual(FirstClient.LocalClientId, namedMessageSent.Connection.Id);
            Assert.AreEqual(FastBufferWriter.GetWriteSize(messageName) + k_NamedMessageOverhead, namedMessageSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageSentMetricToMultipleClients()
        {
            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.NamedMessageSent);
            var messageName = Guid.NewGuid();
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.ToString(), new List<ulong> { FirstClient.LocalClientId, SecondClient.LocalClientId }, writer);
            }


            yield return waitForMetricValues.WaitForMetricsReceived();

            var namedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, namedMessageSentMetricValues.Count);
            Assert.That(namedMessageSentMetricValues.Select(x => x.Name), Has.All.EqualTo(messageName.ToString()));
            Assert.That(namedMessageSentMetricValues.Select(x => x.BytesCount), Has.All.EqualTo(FastBufferWriter.GetWriteSize(messageName) + k_NamedMessageOverhead));
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageSentMetricToSelf()
        {
            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.NamedMessageSent);
            var messageName = Guid.NewGuid();
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.ToString(), Server.LocalClientId, writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            waitForMetricValues.AssertMetricValuesHaveNotBeenFound();
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageReceivedMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(FirstClientMetrics.Dispatcher, NetworkMetricTypes.NamedMessageReceived);

            var messageName = Guid.NewGuid();

            LogAssert.Expect(LogType.Log, $"Received from {Server.LocalClientId}");
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(messageName.ToString(), (ulong sender, FastBufferReader payload) =>
            {
                Debug.Log($"Received from {sender}");
            });

            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.ToString(), FirstClient.LocalClientId, writer);
            }


            yield return waitForMetricValues.WaitForMetricsReceived();

            var namedMessageReceivedValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, namedMessageReceivedValues.Count);

            var namedMessageReceived = namedMessageReceivedValues.First();
            Assert.AreEqual(messageName.ToString(), namedMessageReceived.Name);
            Assert.AreEqual(Server.LocalClientId, namedMessageReceived.Connection.Id);
            Assert.AreEqual(FastBufferWriter.GetWriteSize(messageName) + k_NamedMessageOverhead, namedMessageReceived.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetric()
        {
            var message = Guid.NewGuid();
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(message);

                Server.CustomMessagingManager.SendUnnamedMessage(FirstClient.LocalClientId, writer);
            }


            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.UnnamedMessageSent);

            yield return waitForMetricValues.WaitForMetricsReceived();

            var unnamedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, unnamedMessageSentMetricValues.Count);

            var unnamedMessageSent = unnamedMessageSentMetricValues.First();
            Assert.AreEqual(FirstClient.LocalClientId, unnamedMessageSent.Connection.Id);
            Assert.AreEqual(FastBufferWriter.GetWriteSize(message) + k_UnnamedMessageOverhead, unnamedMessageSent.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetricToMultipleClients()
        {
            var message = Guid.NewGuid();
            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.UnnamedMessageSent);
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(message);

                Server.CustomMessagingManager.SendUnnamedMessage(new List<ulong> { FirstClient.LocalClientId, SecondClient.LocalClientId }, writer);
            }


            yield return waitForMetricValues.WaitForMetricsReceived();

            var unnamedMessageSentMetricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(2, unnamedMessageSentMetricValues.Count);
            Assert.That(unnamedMessageSentMetricValues.Select(x => x.BytesCount), Has.All.EqualTo(FastBufferWriter.GetWriteSize(message) + k_UnnamedMessageOverhead));

            var clientIds = unnamedMessageSentMetricValues.Select(x => x.Connection.Id).ToList();
            Assert.Contains(FirstClient.LocalClientId, clientIds);
            Assert.Contains(SecondClient.LocalClientId, clientIds);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageSentMetricToSelf()
        {
            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.UnnamedMessageSent);
            var messageName = Guid.NewGuid();
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendUnnamedMessage(Server.LocalClientId, writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            waitForMetricValues.AssertMetricValuesHaveNotBeenFound();
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageReceivedMetric()
        {
            var message = Guid.NewGuid();
            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(FirstClientMetrics.Dispatcher, NetworkMetricTypes.UnnamedMessageReceived);
            using (var writer = new FastBufferWriter(1300, Allocator.Temp))
            {
                writer.WriteValueSafe(message);

                Server.CustomMessagingManager.SendUnnamedMessage(FirstClient.LocalClientId, writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var unnamedMessageReceivedValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, unnamedMessageReceivedValues.Count);

            var unnamedMessageReceived = unnamedMessageReceivedValues.First();
            Assert.AreEqual(Server.LocalClientId, unnamedMessageReceived.Connection.Id);
            Assert.AreEqual(FastBufferWriter.GetWriteSize(message) + k_UnnamedMessageOverhead, unnamedMessageReceived.BytesCount);
        }
    }
}
#endif
