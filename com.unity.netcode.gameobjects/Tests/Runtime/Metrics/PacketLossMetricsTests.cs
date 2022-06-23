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
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    public class PacketLossMetricsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private readonly int m_PacketLossRate = 25;
        private readonly int m_PacketLossRangeDelta = 5;

        public PacketLossMetricsTests()
            : base(HostOrServer.Server)
        {}

        protected override void OnServerAndClientsCreated()
        {
            var clientTransport = (UnityTransport)m_ClientNetworkManagers[0].NetworkConfig.NetworkTransport;
            clientTransport.SetDebugSimulatorParameters(0, 0, m_PacketLossRate);

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
                using (var writer = new FastBufferWriter(sizeof(byte), Allocator.Persistent))
                {
                    writer.WriteByteSafe(42);
                    m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage("Test", m_ServerNetworkManager.ConnectedClientsIds, writer);
                }
            }

            yield return waitForPacketLossMetric.WaitForMetricsReceived();

            var packetLossValue = waitForPacketLossMetric.AssertMetricValueHaveBeenFound();
            Assert.AreEqual(0d, packetLossValue);
        }

        [UnityTest]
        public IEnumerator TrackPacketLossAsClient()
        {
            double packetLossRateMinRange = (m_PacketLossRate-m_PacketLossRangeDelta) / 100d;
            double packetLossRateMaxrange = (m_PacketLossRate + m_PacketLossRangeDelta) / 100d;
            var clientNetworkManager = m_ClientNetworkManagers[0];
            var waitForPacketLossMetric = new WaitForGaugeMetricValues((clientNetworkManager.NetworkMetrics as NetworkMetrics).Dispatcher,
                NetworkMetricTypes.PacketLoss,
                metric => packetLossRateMinRange <= metric && metric <= packetLossRateMaxrange);

            for (int i = 0; i < 1000; ++i)
            {
                using (var writer = new FastBufferWriter(sizeof(byte), Allocator.Persistent))
                {
                    writer.WriteByteSafe(42);
                    m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage("Test", m_ServerNetworkManager.ConnectedClientsIds, writer);
                }
            }

            yield return waitForPacketLossMetric.WaitForMetricsReceived();

            var packetLossValue = waitForPacketLossMetric.AssertMetricValueHaveBeenFound();
            Assert.That(packetLossValue, Is.InRange(packetLossRateMinRange, packetLossRateMaxrange));
        }
    }
}

#endif
#endif
