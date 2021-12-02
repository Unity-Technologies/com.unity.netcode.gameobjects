using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Netcode.UTP.Utilities;

namespace Unity.Netcode.UTP.EditorTests
{
    public class BatchedReceiveQueueTests
    {
        [Test]
        public void BatchedReceiveQueue_EmptyReader()
        {
            using (var data = new NativeArray<byte>(0, Allocator.Temp))
            {
                var reader = new DataStreamReader(data);
                var q = new BatchedReceiveQueue(reader);
                Assert.AreEqual(default(ArraySegment<byte>), q.PopMessage());
                Assert.True(q.IsEmpty);
            }
        }

        [Test]
        public void BatchedReceiveQueue_SingleMessage()
        {
            var dataLength = sizeof(int) + 1;

            using (var data = new NativeArray<byte>(dataLength, Allocator.Temp))
            {
                var writer = new DataStreamWriter(data);
                writer.WriteInt(1);
                writer.WriteByte((byte)42);

                var reader = new DataStreamReader(data);
                var q = new BatchedReceiveQueue(reader);

                Assert.False(q.IsEmpty);

                var message = q.PopMessage();
                Assert.AreEqual(1, message.Count);
                Assert.AreEqual((byte)42, message.Array[message.Offset]);

                Assert.AreEqual(default(ArraySegment<byte>), q.PopMessage());
                Assert.True(q.IsEmpty);
            }
        }

        [Test]
        public void BatchedReceiveQueue_MultipleMessages()
        {
            var dataLength = (sizeof(int) + 1) * 2;

            using (var data = new NativeArray<byte>(dataLength, Allocator.Temp))
            {
                var writer = new DataStreamWriter(data);
                writer.WriteInt(1);
                writer.WriteByte((byte)42);
                writer.WriteInt(1);
                writer.WriteByte((byte)142);

                var reader = new DataStreamReader(data);
                var q = new BatchedReceiveQueue(reader);

                Assert.False(q.IsEmpty);

                var message1 = q.PopMessage();
                Assert.AreEqual(1, message1.Count);
                Assert.AreEqual((byte)42, message1.Array[message1.Offset]);

                var message2 = q.PopMessage();
                Assert.AreEqual(1, message2.Count);
                Assert.AreEqual((byte)142, message2.Array[message2.Offset]);

                Assert.AreEqual(default(ArraySegment<byte>), q.PopMessage());
                Assert.True(q.IsEmpty);
            }
        }
    }
}