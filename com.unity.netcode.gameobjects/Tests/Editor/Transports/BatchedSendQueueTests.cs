using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;

namespace Unity.Netcode.EditorTests
{
    public class BatchedSendQueueTests
    {
        private const int k_TestQueueCapacity = 1024;
        private const int k_TestMessageSize = 42;

        private ArraySegment<byte> m_TestMessage;

        private void AssertIsTestMessage(NativeArray<byte> data)
        {
            var reader = new DataStreamReader(data);
            Assert.AreEqual(k_TestMessageSize, reader.ReadInt());
            for (int i = 0; i < k_TestMessageSize; i++)
            {
                Assert.AreEqual(m_TestMessage.Array[i], reader.ReadByte());
            }
        }

        [OneTimeSetUp]
        public void InitializeTestMessage()
        {
            var data = new byte[k_TestMessageSize];
            for (int i = 0; i < k_TestMessageSize; i++)
            {
                data[i] = (byte)i;
            }
            m_TestMessage = new ArraySegment<byte>(data);
        }

        [Test]
        public void BatchedSendQueue_EmptyOnCreation()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);

            Assert.AreEqual(0, q.Length);
            Assert.True(q.IsEmpty);
        }

        [Test]
        public void BatchedSendQueue_NotCreatedAfterDispose()
        {
            var q = new BatchedSendQueue(k_TestQueueCapacity);
            q.Dispose();
            Assert.False(q.IsCreated);
        }

        [Test]
        public void BatchedSendQueue_PushMessageReturnValue()
        {
            // Will fit a single test message, but not two (with overhead included).
            var queueCapacity = (k_TestMessageSize * 2) + BatchedSendQueue.PerMessageOverhead;

            using var q = new BatchedSendQueue(queueCapacity);

            Assert.True(q.PushMessage(m_TestMessage));
            Assert.False(q.PushMessage(m_TestMessage));
        }

        [Test]
        public void BatchedSendQueue_LengthIncreasedAfterPush()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);

            q.PushMessage(m_TestMessage);
            Assert.AreEqual(k_TestMessageSize + BatchedSendQueue.PerMessageOverhead, q.Length);
        }

        [Test]
        public void BatchedSendQueue_PushedMessageGeneratesCopy()
        {
            var messageLength = k_TestMessageSize + BatchedSendQueue.PerMessageOverhead;
            var queueCapacity = messageLength * 2;

            using var q = new BatchedSendQueue(queueCapacity);
            using var data = new NativeArray<byte>(k_TestQueueCapacity, Allocator.Temp);

            q.PushMessage(m_TestMessage);
            q.PushMessage(m_TestMessage);

            q.Consume(messageLength);
            Assert.IsTrue(q.PushMessage(m_TestMessage));
            Assert.AreEqual(queueCapacity, q.Length);
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithMessages_ReturnValue()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(k_TestQueueCapacity, Allocator.Temp);

            q.PushMessage(m_TestMessage);

            var writer = new DataStreamWriter(data);
            var filled = q.FillWriterWithMessages(ref writer);
            Assert.AreEqual(k_TestMessageSize + BatchedSendQueue.PerMessageOverhead, filled);
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithMessages_NoopIfNoPushedMessages()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(k_TestQueueCapacity, Allocator.Temp);

            var writer = new DataStreamWriter(data);
            Assert.AreEqual(0, q.FillWriterWithMessages(ref writer));
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithMessages_NoopIfNotEnoughCapacity()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(2, Allocator.Temp);

            q.PushMessage(m_TestMessage);

            var writer = new DataStreamWriter(data);
            Assert.AreEqual(0, q.FillWriterWithMessages(ref writer));
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithMessages_SinglePushedMessage()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(k_TestQueueCapacity, Allocator.Temp);

            q.PushMessage(m_TestMessage);

            var writer = new DataStreamWriter(data);
            q.FillWriterWithMessages(ref writer);
            AssertIsTestMessage(data);
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithMessages_MultiplePushedMessages()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(k_TestQueueCapacity, Allocator.Temp);

            q.PushMessage(m_TestMessage);
            q.PushMessage(m_TestMessage);

            var writer = new DataStreamWriter(data);
            q.FillWriterWithMessages(ref writer);

            var messageLength = k_TestMessageSize + BatchedSendQueue.PerMessageOverhead;
            AssertIsTestMessage(data);
            AssertIsTestMessage(data.GetSubArray(messageLength, messageLength));
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithMessages_PartialPushedMessages()
        {
            var messageLength = k_TestMessageSize + BatchedSendQueue.PerMessageOverhead;

            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(messageLength, Allocator.Temp);

            q.PushMessage(m_TestMessage);
            q.PushMessage(m_TestMessage);

            var writer = new DataStreamWriter(data);
            Assert.AreEqual(messageLength, q.FillWriterWithMessages(ref writer));
            AssertIsTestMessage(data);
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithBytes_NoopIfNoData()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(k_TestQueueCapacity, Allocator.Temp);

            var writer = new DataStreamWriter(data);
            Assert.AreEqual(0, q.FillWriterWithBytes(ref writer));
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithBytes_WriterCapacityMoreThanLength()
        {
            var dataLength = k_TestMessageSize + BatchedSendQueue.PerMessageOverhead;

            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(k_TestQueueCapacity, Allocator.Temp);

            q.PushMessage(m_TestMessage);

            var writer = new DataStreamWriter(data);
            Assert.AreEqual(dataLength, q.FillWriterWithBytes(ref writer));
            AssertIsTestMessage(data);
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithBytes_WriterCapacityLessThanLength()
        {
            var dataLength = k_TestMessageSize + BatchedSendQueue.PerMessageOverhead;

            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(dataLength, Allocator.Temp);

            q.PushMessage(m_TestMessage);
            q.PushMessage(m_TestMessage);

            var writer = new DataStreamWriter(data);
            Assert.AreEqual(dataLength, q.FillWriterWithBytes(ref writer));
            AssertIsTestMessage(data);
        }

        [Test]
        public void BatchedSendQueue_FillWriterWithBytes_WriterCapacityEqualToLength()
        {
            var dataLength = k_TestMessageSize + BatchedSendQueue.PerMessageOverhead;

            using var q = new BatchedSendQueue(k_TestQueueCapacity);
            using var data = new NativeArray<byte>(dataLength, Allocator.Temp);

            q.PushMessage(m_TestMessage);

            var writer = new DataStreamWriter(data);
            Assert.AreEqual(dataLength, q.FillWriterWithBytes(ref writer));
            AssertIsTestMessage(data);
        }

        [Test]
        public void BatchedSendQueue_ConsumeLessThanLength()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);

            q.PushMessage(m_TestMessage);
            q.PushMessage(m_TestMessage);

            var messageLength = k_TestMessageSize + BatchedSendQueue.PerMessageOverhead;
            q.Consume(messageLength);
            Assert.AreEqual(messageLength, q.Length);
        }

        [Test]
        public void BatchedSendQueue_ConsumeExactLength()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);

            q.PushMessage(m_TestMessage);

            q.Consume(k_TestMessageSize + BatchedSendQueue.PerMessageOverhead);
            Assert.AreEqual(0, q.Length);
            Assert.True(q.IsEmpty);
        }

        [Test]
        public void BatchedSendQueue_ConsumeMoreThanLength()
        {
            using var q = new BatchedSendQueue(k_TestQueueCapacity);

            q.PushMessage(m_TestMessage);

            q.Consume(k_TestQueueCapacity);
            Assert.AreEqual(0, q.Length);
            Assert.True(q.IsEmpty);
        }
    }
}
