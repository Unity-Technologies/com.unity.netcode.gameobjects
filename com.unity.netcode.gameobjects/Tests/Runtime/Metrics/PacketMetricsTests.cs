#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class PacketMetricsTests
    {
        [UnityTest]
        public IEnumerator TrackPacketSentMetric()
        {
            MultiInstanceHelpers.Create(1, out var server, out var clients, 60, MultiInstanceHelpers.InstanceTransport.UTP);

            server.StartServer();
            clients[0].StartClient();

            var serverMetrics = (NetworkMetrics)server.NetworkMetrics;
            var waitForMetricValues = new WaitForMetricValues<PacketEvent>(serverMetrics.Dispatcher, NetworkMetricTypes.PacketEventSent, metric => { return metric.PacketCount > 0; });

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            var totalPacketCount = metricValues.Sum(x => x.PacketCount);
            Assert.That(totalPacketCount, Is.GreaterThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator TrackPacketReceivedMetric()
        {
            MultiInstanceHelpers.Create(1, out var server, out var clients, 60, MultiInstanceHelpers.InstanceTransport.UTP);

            server.StartServer();
            clients[0].StartClient();

            var clientMetrics = (NetworkMetrics)clients[0].NetworkMetrics;
            var waitForMetricValues = new WaitForMetricValues<PacketEvent>(clientMetrics.Dispatcher, NetworkMetricTypes.PacketEventReceived, metric => { return metric.PacketCount > 0; });

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var metricValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            var totalPacketCount = metricValues.Sum(x => x.PacketCount);
            Assert.That(totalPacketCount, Is.GreaterThanOrEqualTo(1));
        }
    }
}
#endif
