#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class ServerLogsMetricTests : SingleClientMetricTestBase
    {
        private static readonly int k_ServerLogSentMessageOverhead = 2 + FastBufferWriter.GetWriteSize<MessageHeader>();
        private static readonly int k_ServerLogReceivedMessageOverhead = 2;

        [UnityTest]
        public IEnumerator TrackServerLogSentMetric()
        {
            var waitForSentMetric = new WaitForMetricValues<ServerLogEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.ServerLogSent);

            var message = Guid.NewGuid().ToString();
            NetworkLog.LogWarningServer(message);

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual((uint)NetworkLog.LogType.Warning, (uint)sentMetric.LogLevel);
            Assert.AreEqual(message.Length + k_ServerLogSentMessageOverhead, sentMetric.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackServerLogReceivedMetric()
        {
            var waitForReceivedMetric = new WaitForMetricValues<ServerLogEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.ServerLogReceived);

            var message = Guid.NewGuid().ToString();
            NetworkLog.LogWarningServer(message);

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual((uint)NetworkLog.LogType.Warning, (uint)receivedMetric.LogLevel);
            Assert.AreEqual(message.Length + k_ServerLogReceivedMessageOverhead, receivedMetric.BytesCount);
        }
    }
}
#endif
