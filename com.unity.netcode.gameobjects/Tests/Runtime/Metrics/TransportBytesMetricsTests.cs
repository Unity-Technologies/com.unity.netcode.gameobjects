#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class TransportBytesMetricsTests : SingleClientMetricTestBase
    {
        static readonly long MessageOverhead = 8 + FastBufferWriter.GetWriteSize<BatchHeader>() + FastBufferWriter.GetWriteSize<MessageHeader>();

        [UnityTest]
        public IEnumerator TrackTotalNumberOfBytesSent()
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

            var nbFrames = 0;
            while (!observer.Found || nbFrames < 10)
            {
                yield return null;
                nbFrames++;
            }

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



            var nbFrames = 0;
            while (!observer.Found || nbFrames < 10)
            {
                yield return null;
                nbFrames++;
            }

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
