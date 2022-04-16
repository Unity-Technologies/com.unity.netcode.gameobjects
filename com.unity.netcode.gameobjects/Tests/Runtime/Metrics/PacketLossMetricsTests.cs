#if MULTIPLAYER_TOOLS
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7

using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.TestHelpers.Runtime.Metrics;
using Unity.Netcode.Transports.UTP;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    public class PacketLossMetricsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private readonly int m_PacketLossRate = 25;
        private static string s_TimedoutMessage = $"timed out waiting for {nameof(WaitForGaugeMetricValues)} to receive any {nameof(NetworkMetricTypes.PacketLoss)} metrics!";

        public PacketLossMetricsTests()
            : base(HostOrServer.Server)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            var clientTransport = (UnityTransport)m_ClientNetworkManagers[0].NetworkConfig.NetworkTransport;

            clientTransport.SetDebugSimulatorParameters(0, 0, m_PacketLossRate);
            // Setting scene management to false to avoid any issues with client
            // synchronization getting in the way of timing to detect the packet loss metric
            m_ClientNetworkManagers[0].NetworkConfig.EnableSceneManagement = false;
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = false;

            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator TrackPacketLossAsServer()
        {
            var waitForPacketLossMetric = new WaitForGaugeMetricValues((m_ServerNetworkManager.NetworkMetrics as NetworkMetrics).Dispatcher,
                NetworkMetricTypes.PacketLoss,
                metric => metric == 0.0d);

            for (int i = 0; i < 1000; ++i)
            {
                using var writer = new FastBufferWriter(sizeof(byte), Allocator.Persistent);
                writer.WriteByteSafe(42);
                m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage("Test", m_ServerNetworkManager.ConnectedClientsIds, writer);
            }

            //yield return waitForPacketLossMetric.WaitForMetricsReceived();
            // MTT-Tools: This is a more RTT/latency tolerant way to wait for a metric to be found (especially for high-packet count integration tests)
            yield return WaitForConditionOrTimeOut(() => waitForPacketLossMetric.MetricFound());
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Server {s_TimedoutMessage}");

            var packetLossValue = waitForPacketLossMetric.AssertMetricValueHaveBeenFound();
            Assert.AreEqual(0d, packetLossValue);
        }

        [UnityTest]
        public IEnumerator TrackPacketLossAsClient()
        {
            double packetLossRate = m_PacketLossRate/100d;
            var clientNetworkManager = m_ClientNetworkManagers[0];
            var waitForPacketLossMetric = new WaitForGaugeMetricValues((clientNetworkManager.NetworkMetrics as NetworkMetrics).Dispatcher,
                NetworkMetricTypes.PacketLoss,
                metric => Math.Abs(metric - packetLossRate) < double.Epsilon);

            for (int i = 0; i < 1000; ++i)
            {
                using var writer = new FastBufferWriter(sizeof(byte), Allocator.Persistent);
                writer.WriteByteSafe(42);
                m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage("Test", m_ServerNetworkManager.ConnectedClientsIds, writer);
            }

            //yield return waitForPacketLossMetric.WaitForMetricsReceived();
            // MTT-Tools: This is a more RTT/latency tolerant way to wait for a metric to be found (especially for high-packet count integration tests)
            yield return WaitForConditionOrTimeOut(() => waitForPacketLossMetric.MetricFound());
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Client {s_TimedoutMessage}");

            var packetLossValue = waitForPacketLossMetric.AssertMetricValueHaveBeenFound();
            Assert.AreEqual(packetLossRate, packetLossValue);
        }
    }
}

#endif
#endif
