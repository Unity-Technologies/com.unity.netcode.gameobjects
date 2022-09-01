#if MULTIPLAYER_TOOLS
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime.Metrics;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class PacketMetricsTests : SingleClientMetricTestBase
    {
        [UnityTest]
        public IEnumerator TrackPacketSentMetric()
        {
            var waitForMetricValues = new WaitForCounterMetricValue(ServerMetrics.Dispatcher, NetworkMetricTypes.PacketsSent, metric => metric > 0);

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                Server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var totalPacketCount = waitForMetricValues.AssertMetricValueHaveBeenFound();
            Assert.That(totalPacketCount, Is.InRange(1, 4));
        }

        [UnityTest]
        public IEnumerator TrackPacketReceivedMetric()
        {
            var waitForMetricValues = new WaitForCounterMetricValue(ClientMetrics.Dispatcher, NetworkMetricTypes.PacketsReceived, metric => metric > 0);

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                Server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var totalPacketCount = waitForMetricValues.AssertMetricValueHaveBeenFound();
            Assert.That(totalPacketCount, Is.InRange(1, 4));
        }
    }
}
#endif
#endif
