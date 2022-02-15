#if MULTIPLAYER_TOOLS
#if MULTIPLAYER_TOOLS_1_0_0_PRE_4

using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class RttMetricsTests
    {
        [UnityTest]
        public IEnumerator TrackRttMetricServerToSingleClient()
        {
            MultiInstanceHelpers.Create(
                clientCount: 1,
                out var server,
                out var clients,
                targetFrameRate: 60,
                MultiInstanceHelpers.InstanceTransport.UTP);

            server.StartServer();
            clients[0].StartClient();

            var serverMetrics = (NetworkMetrics)server.NetworkMetrics;
            var waitForMetricValues = new WaitForGaugeMetricValues(serverMetrics.Dispatcher, NetworkMetricTypes.RttToServer);

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var rttValue = waitForMetricValues.AssertMetricValueHaveBeenFound();
            Assert.AreEqual(0f, rttValue);
        }

        [UnityTest]
        public IEnumerator TrackRttMetricServerToMultipleClients()
        {
            MultiInstanceHelpers.Create(
                clientCount: 2,
                out var server,
                out var clients,
                targetFrameRate: 60,
                MultiInstanceHelpers.InstanceTransport.UTP);

            server.StartServer();
            clients[0].StartClient();
            clients[1].StartClient();

            var serverMetrics = (NetworkMetrics)server.NetworkMetrics;
            var waitForMetricValues = new WaitForGaugeMetricValues(serverMetrics.Dispatcher, NetworkMetricTypes.RttToServer);

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var rttValue = waitForMetricValues.AssertMetricValueHaveBeenFound();
            Assert.AreEqual(0f, rttValue);
        }

        [UnityTest]
        public IEnumerator TrackRttMetricClientToServer()
        {
            MultiInstanceHelpers.Create(
                clientCount: 1,
                out var server,
                out var clients,
                targetFrameRate: 60,
                MultiInstanceHelpers.InstanceTransport.UTP);

            server.StartServer();
            clients[0].StartClient();

            var clientMetrics = (NetworkMetrics)clients[0].NetworkMetrics;
            var waitForMetricValues = new WaitForGaugeMetricValues(clientMetrics.Dispatcher, NetworkMetricTypes.RttToServer, metric => metric > 0f) ;

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var rttValue = waitForMetricValues.AssertMetricValueHaveBeenFound();
            Assert.That(rttValue, Is.GreaterThanOrEqualTo(1f));
        }
    }
}
#endif
#endif
