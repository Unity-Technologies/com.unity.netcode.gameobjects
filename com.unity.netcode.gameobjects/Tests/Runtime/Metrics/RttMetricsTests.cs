#if MULTIPLAYER_TOOLS
#if MULTIPLAYER_TOOLS_1_0_0_PRE_3

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
            var waitForMetricValues = new WaitForEventMetricValues<RTTEvent>(serverMetrics.Dispatcher, NetworkMetricTypes.Rtt);

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var rttValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.That(rttValues.Count, Is.GreaterThanOrEqualTo(1));

            var average = rttValues.Average(rtt => rtt.RTT);
            Assert.That(average, Is.GreaterThanOrEqualTo(1));
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
            var waitForMetricValues = new WaitForEventMetricValues<RTTEvent>(serverMetrics.Dispatcher, NetworkMetricTypes.Rtt);

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var rttValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.That(rttValues.Count, Is.GreaterThanOrEqualTo(1));

            var valuesClient1 = rttValues.Where(rtt => rtt.Connection.Id == clients[0].ServerClientId);
            Assert.That(valuesClient1.Count(), Is.GreaterThanOrEqualTo(1));
            var averageClient1 = valuesClient1.Average(rtt => rtt.RTT);
            Assert.That(averageClient1, Is.GreaterThanOrEqualTo(1));

            var valuesClient2 = rttValues.Where(rtt => rtt.Connection.Id == clients[1].ServerClientId);
            Assert.That(valuesClient2.Count(), Is.GreaterThanOrEqualTo(1));
            var averageClient2 = valuesClient2.Average(rtt => rtt.RTT);
            Assert.That(averageClient2, Is.GreaterThanOrEqualTo(1));
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
            var waitForMetricValues = new WaitForEventMetricValues<RTTEvent>(clientMetrics.Dispatcher, NetworkMetricTypes.Rtt);

            using (var writer = new FastBufferWriter(sizeof(uint), Allocator.Temp))
            {
                writer.WriteValueSafe(1337);
                server.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            yield return waitForMetricValues.WaitForMetricsReceived();

            var rttValues = waitForMetricValues.AssertMetricValuesHaveBeenFound();
            Assert.That(rttValues.Count, Is.GreaterThanOrEqualTo(1));

            var average = rttValues.Average(rtt => rtt.RTT);
            Assert.That(average, Is.GreaterThanOrEqualTo(1));
        }
    }
}
#endif
#endif
