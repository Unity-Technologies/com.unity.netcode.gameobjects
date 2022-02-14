#if MULTIPLAYER_TOOLS
#if MULTIPLAYER_TOOLS_1_0_0_PRE_4
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.TestHelpers.Runtime.Metrics;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class PacketMetricsTests
    {
        [UnityTest]
        public IEnumerator TrackPacketSentMetric()
        {
            NetcodeIntegrationTestHelpers.Create(
                clientCount: 1,
                out var server,
                out var clients,
                targetFrameRate: 60,
#if UTP_ADAPTER
                NetcodeIntegrationTestHelpers.InstanceTransport.UTP);
#else
                NetcodeIntegrationTestHelpers.InstanceTransport.SIP);
#endif

            server.StartServer();
            clients[0].StartClient();

            var serverMetrics = (NetworkMetrics)server.NetworkMetrics;
            var waitForMetricValues = new WaitForCounterMetricValue(serverMetrics.Dispatcher, NetworkMetricTypes.PacketsSent, metric => metric > 0);

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var totalPacketCount = waitForMetricValues.AssertMetricValueHaveBeenFound();
            Assert.That(totalPacketCount, Is.InRange(1, 4));
        }

        [UnityTest]
        public IEnumerator TrackPacketReceivedMetric()
        {
            NetcodeIntegrationTestHelpers.Create(
                clientCount: 1,
                out var server,
                out var clients,
                targetFrameRate: 60,
#if UTP_ADAPTER
                NetcodeIntegrationTestHelpers.InstanceTransport.UTP);
#else
                NetcodeIntegrationTestHelpers.InstanceTransport.SIP);
#endif
            server.StartServer();
            clients[0].StartClient();

            var clientMetrics = (NetworkMetrics)clients[0].NetworkMetrics;
            var waitForMetricValues = new WaitForCounterMetricValue(clientMetrics.Dispatcher, NetworkMetricTypes.PacketsReceived, metric => metric > 0);

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var totalPacketCount = waitForMetricValues.AssertMetricValueHaveBeenFound();
            Assert.That(totalPacketCount, Is.InRange(1, 4));
        }
    }
}
#endif
#endif
