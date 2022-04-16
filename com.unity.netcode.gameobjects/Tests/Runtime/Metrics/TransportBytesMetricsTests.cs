#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;
using Unity.Netcode.TestHelpers.Runtime.Metrics;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class TransportBytesMetricsTests : SingleClientMetricTestBase
    {
        // Header is dynamically sized due to packing, will be 2 bytes for all test messages.
        private const int k_MessageHeaderSize = 2;
        static readonly long MessageOverhead = 8 + FastBufferWriter.GetWriteSize<BatchHeader>() + k_MessageHeaderSize;

        protected override void OnServerAndClientsCreated()
        {
            // Setting scene management to false to avoid any issues with client
            // synchronization getting in the way of potentially causing the
            // total bytes sent to be larger than expected (i.e. resynchronization)
            m_ClientNetworkManagers[0].NetworkConfig.EnableSceneManagement = false;
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = false;
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator TrackTotalNumberOfBytesSent()
        {
            var messageName = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            var observer = new TotalBytesObserver(ServerMetrics.Dispatcher, NetworkMetricTypes.TotalBytesSent);
            try
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.ToString(), Client.LocalClientId, writer);
            }
            finally
            {
                writer.Dispose();
            }

            yield return WaitForConditionOrTimeOut(() => observer.Found);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for observer to receive {nameof(NetworkMetricTypes.TotalBytesSent)} metrics!");

            Assert.True(observer.Found);
            Assert.AreEqual(FastBufferWriter.GetWriteSize(messageName) + MessageOverhead, observer.Value);
        }

        [UnityTest]
        public IEnumerator TrackTotalNumberOfBytesReceived()
        {
            var messageName = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            var observer = new TotalBytesObserver(ClientMetrics.Dispatcher, NetworkMetricTypes.TotalBytesReceived);
            try
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.ToString(), Client.LocalClientId, writer);
            }
            finally
            {
                writer.Dispose();
            }

            yield return WaitForConditionOrTimeOut(() => observer.Found);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for observer to receive {nameof(NetworkMetricTypes.TotalBytesReceived)} metrics!");

            Assert.True(observer.Found);
            Assert.AreEqual(FastBufferWriter.GetWriteSize(messageName) + MessageOverhead, observer.Value);
        }

        private class TotalBytesObserver : IMetricObserver
        {
            private readonly DirectionalMetricInfo m_MetricInfo;

            public TotalBytesObserver(IMetricDispatcher dispatcher, DirectionalMetricInfo metricInfo)
            {
                m_MetricInfo = metricInfo;

                dispatcher.RegisterObserver(this);
            }

            public bool Found { get; private set; }

            public long Value { get; private set; }

            public void Observe(MetricCollection collection)
            {
                if (collection.TryGetCounter(m_MetricInfo.Id, out var counter) && counter.Value > 0)
                {
                    Found = true;
                    Value = counter.Value;
                }
            }
        }
    }
}
#endif
