#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class TransportBytesMetricsTests : SingleClientMetricTestBase
    {
        const long MessageOverhead = 9;

        [UnityTest]
        public IEnumerator TrackTotalNumberOfBytesSent()
        {
            var messageName = Guid.NewGuid().ToString();
            using var memoryStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(messageName);

            var observer = new TotalBytesObserver(ServerMetrics.Dispatcher, NetworkMetricTypes.TotalBytesSent);

            Server.CustomMessagingManager.SendNamedMessage(messageName, Client.LocalClientId, memoryStream);

            var nbFrames = 0;
            while (!observer.Found || nbFrames < 10)
            {
                yield return null;
                nbFrames++;
            }

            Assert.True(observer.Found);
            Assert.AreEqual(messageName.Length + MessageOverhead, observer.Value);
        }

        [UnityTest]
        public IEnumerator TrackTotalNumberOfBytesReceived()
        {
            var messageName = Guid.NewGuid().ToString();
            using var memoryStream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(messageName);

            var observer = new TotalBytesObserver(ClientMetrics.Dispatcher, NetworkMetricTypes.TotalBytesReceived);

            Server.CustomMessagingManager.SendNamedMessage(messageName, Client.LocalClientId, memoryStream);

            var nbFrames = 0;
            while (!observer.Found || nbFrames < 10)
            {
                yield return null;
                nbFrames++;
            }

            Assert.True(observer.Found);
            Assert.AreEqual(messageName.Length + MessageOverhead, observer.Value);
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
