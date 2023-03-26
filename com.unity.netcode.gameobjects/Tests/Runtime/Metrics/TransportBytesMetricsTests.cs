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
        // private const int k_MessageHeaderSize = 2;
        // TODO 2023-Q2: Talk to tools & Kitty about this header calculation for a named message (is packing impacting this?)
        // private static readonly long k_MessageOverhead = 8 + FastBufferWriter.GetWriteSize<BatchHeader>() + k_MessageHeaderSize;

        /// <summary>
        /// <see cref="NamedMessage"/> which the size is the version (uint) + the hash (ulong) + the payload ?
        /// </summary>
        private static readonly long k_MessageOverhead = +sizeof(uint) + sizeof(ulong);

        [UnityTest]
        public IEnumerator TrackTotalNumberOfBytesSent()
        {
            var messageName = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            // NSS: Changed observer from Client TotalBytesReceived to Server TotalBytesSent
            var observer = new TotalBytesObserver(ServerMetrics.Dispatcher, NetworkMetricTypes.TotalBytesSent);
            m_ReceivedMessage = false;
            Client.CustomMessagingManager.RegisterNamedMessageHandler(messageName.Value.ToString(), TrackTotalNumberOfBytesSentNamedMessage);
            try
            {
                writer.WriteValueSafe(messageName);

                Server.CustomMessagingManager.SendNamedMessage(messageName.Value.ToString(), Client.LocalClientId, writer);
            }
            finally
            {
                writer.Dispose();
            }

            //var nbFrames = 0;
            //while (!observer.Found || nbFrames < 10)
            //{
            //    yield return null;
            //    nbFrames++;
            //}
            // Wait for the client to receive the message before checking how many bytes were sent
            yield return WaitForConditionOrTimeOut(() => m_ReceivedMessage);
            AssertOnTimeout($"Client failed to receive named message {messageName}!");

            Assert.True(observer.Found);
            var overhead = k_MessageOverhead;
            var writeSize = FastBufferWriter.GetWriteSize(messageName);

            // This values changes due to packing? (see notes at top of test)
            Assert.True(writeSize + overhead == observer.Value || ((writeSize - 1) + overhead) == observer.Value);
        }
        private bool m_ReceivedMessage;
        private void TrackTotalNumberOfBytesSentNamedMessage(ulong senderClientId, FastBufferReader messagePayload)
        {
            m_ReceivedMessage = true;
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
            var writeSize = FastBufferWriter.GetWriteSize(messageName);
            Assert.True(observer.Found);
            // This values changes due to packing? (see notes at top of test)
            Assert.True(writeSize + k_MessageOverhead == observer.Value || ((writeSize-1) + k_MessageOverhead) == observer.Value);
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
