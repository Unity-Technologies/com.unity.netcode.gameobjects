using System.Collections;
using System.Linq;
using MLAPI.Logging;
using MLAPI.Metrics;
using MLAPI.RuntimeTests.Metrics.Utility;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics
{
    public class ServerLogsMetricTests
    {
        NetworkManager m_Server;
        NetworkManager m_Client;
        NetworkMetrics m_ClientMetrics;
        NetworkMetrics m_ServerMetrics;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var initializer = new SingleClientMetricTestInitializer();

            yield return initializer.Initialize();

            m_Server = initializer.Server;
            m_Client = initializer.Client;
            m_ClientMetrics = initializer.ClientMetrics;
            m_ServerMetrics = initializer.ServerMetrics;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MultiInstanceHelpers.Destroy();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TrackServerLogSentMetric()
        {
            var waitForSentMetric = new WaitForMetricValues<ServerLogEvent>(m_ClientMetrics.Dispatcher, MetricNames.ServerLogSent);

            NetworkLog.LogWarningServer("log message");

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(m_Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual((uint)NetworkLog.LogType.Warning, (uint)sentMetric.LogLevel);
        }

        [UnityTest]
        public IEnumerator TrackServerLogReceivedMetric()
        {
            var waitForReceivedMetric = new WaitForMetricValues<ServerLogEvent>(m_ServerMetrics.Dispatcher, MetricNames.ServerLogReceived);

            NetworkLog.LogWarningServer("log message");

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(m_Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual((uint)NetworkLog.LogType.Warning, (uint)receivedMetric.LogLevel);
        }
    }
}
