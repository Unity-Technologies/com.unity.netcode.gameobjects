#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.TestHelpers.Runtime.Metrics;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class ServerLogsMetricTests : SingleClientMetricTestBase
    {
        // Header is dynamically sized due to packing, will be 3 bytes for all test messages.
        private const int k_MessageHeaderSize = 3;

        protected override IEnumerator OnSetup()
        {
            m_CreateServerFirst = false;
            return base.OnSetup();
        }


        private int GetWriteSizeForLog(NetworkLog.LogType logType, string logMessage)
        {
            var message = new ServerLogMessage
            {
                LogType = logType,
                Message = logMessage
            };
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            message.Serialize(writer, message.Version);
            return writer.Length;
        }

        [UnityTest]
        public IEnumerator TrackServerLogSentMetric()
        {
            // Set the client NetworkManager to assure the log is sent
            NetworkLog.NetworkManagerOverride = Client;
            var waitForSentMetric = new WaitForEventMetricValues<ServerLogEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.ServerLogSent);

            var message = Guid.NewGuid().ToString();
            Client.LogLevel = LogLevel.Developer;
            Server.LogLevel = LogLevel.Developer;
            NetworkLog.LogWarningServer(message);
            yield return s_DefaultWaitForTick;

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual((uint)NetworkLog.LogType.Warning, (uint)sentMetric.LogLevel);

            var serializedLength = GetWriteSizeForLog(NetworkLog.LogType.Warning, message);
            Assert.AreEqual(serializedLength + k_MessageHeaderSize, sentMetric.BytesCount);
        }

        [UnityTest]
        public IEnumerator TrackServerLogReceivedMetric()
        {
            var waitForReceivedMetric = new WaitForEventMetricValues<ServerLogEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.ServerLogReceived);

            var message = Guid.NewGuid().ToString();
            Client.LogLevel = LogLevel.Developer;
            Server.LogLevel = LogLevel.Developer;
            NetworkLog.LogWarningServer(message);

            yield return s_DefaultWaitForTick;

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual((uint)NetworkLog.LogType.Warning, (uint)receivedMetric.LogLevel);

            var serializedLength = GetWriteSizeForLog(NetworkLog.LogType.Warning, message);
            Assert.AreEqual(serializedLength, receivedMetric.BytesCount);
        }

        protected override IEnumerator OnTearDown()
        {
            NetworkLog.NetworkManagerOverride = null;
            return base.OnTearDown();
        }
    }
}
#endif
