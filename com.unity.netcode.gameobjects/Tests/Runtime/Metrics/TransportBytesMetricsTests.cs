#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
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
        private static readonly long k_MessageOverhead = 8 + FastBufferWriter.GetWriteSize<NetworkBatchHeader>() + k_MessageHeaderSize;

        [UnityTest]
        public IEnumerator TrackTotalNumberOfBytesSent()
        {
            var messageName = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            var observer = new TotalBytesObserver(ClientMetrics.Dispatcher, NetworkMetricTypes.TotalBytesReceived);
            try
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.Value.ToString(), Client.LocalClientId, writer);
            }
            finally
            {
                writer.Dispose();
            }

            var nbFrames = 0;
            while (!observer.Found || nbFrames < 10)
            {
                yield return null;
                nbFrames++;
            }

            Assert.True(observer.Found);
            Assert.AreEqual(((FastBufferWriter.GetWriteSize(messageName) + k_MessageOverhead) + 7) & ~7, observer.Value);
        }

        [UnityTest]
        public IEnumerator TrackTotalNumberOfBytesReceived()
        {
            var messageName = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            var observer = new TotalBytesObserver(ClientMetrics.Dispatcher, NetworkMetricTypes.TotalBytesReceived);
            try
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.Value.ToString(), Client.LocalClientId, writer);
            }
            finally
            {
                writer.Dispose();
            }

            var nbFrames = 0;
            while (!observer.Found || nbFrames < 10)
            {
                yield return null;
                nbFrames++;
            }

            Assert.True(observer.Found);
            Assert.AreEqual(((FastBufferWriter.GetWriteSize(messageName) + k_MessageOverhead) + 7) & ~7, observer.Value);
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

            private int m_BytesFoundCounter;
            private long m_TotalBytes;

            public void Observe(MetricCollection collection)
            {
                if (collection.TryGetCounter(m_MetricInfo.Id, out var counter) && counter.Value > 0)
                {
                    // Don't assign another observed value once one is already observed
                    if (!Found)
                    {
                        Found = true;
                        Value = counter.Value;
                        m_TotalBytes += ((counter.Value + 7) & ~7);
                        m_BytesFoundCounter++;
                        UnityEngine.Debug.Log($"[{m_BytesFoundCounter}] Bytes Observed {counter.Value} | Total Bytes Observed: {m_TotalBytes}");
                    }
                }
            }
        }
    }
}
#endif
