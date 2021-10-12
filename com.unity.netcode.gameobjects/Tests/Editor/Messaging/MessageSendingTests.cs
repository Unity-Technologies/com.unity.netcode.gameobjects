using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Netcode.EditorTests
{
    public class MessageSendingTests
    {
        private struct TestMessage : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            public static bool Serialized;

            public void Serialize(FastBufferWriter writer)
            {
                Serialized = true;
                writer.WriteValueSafe(this);
            }

            public static void Receive(FastBufferReader reader, in NetworkContext context)
            {
            }
        }

        private class TestMessageSender : IMessageSender
        {
            public List<byte[]> MessageQueue = new List<byte[]>();

            public void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData)
            {
                MessageQueue.Add(batchData.ToArray());
            }
        }

        private class TestMessageProvider : IMessageProvider
        {
            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                return new List<MessagingSystem.MessageWithHandler>
                {
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessage),
                        Handler = TestMessage.Receive
                    }
                };
            }
        }

        private TestMessageSender m_MessageSender;
        private MessagingSystem m_MessagingSystem;
        private ulong[] m_Clients = { 0 };

        [SetUp]
        public void SetUp()
        {
            TestMessage.Serialized = false;

            m_MessageSender = new TestMessageSender();
            m_MessagingSystem = new MessagingSystem(m_MessageSender, this, new TestMessageProvider());
            m_MessagingSystem.ClientConnected(0);
        }

        [TearDown]
        public void TearDown()
        {
            m_MessagingSystem.Dispose();
        }

        private TestMessage GetMessage()
        {
            var random = new Random();
            return new TestMessage
            {
                A = random.Next(),
                B = random.Next(),
                C = random.Next(),
            };
        }

        [Test]
        public void WhenSendingMessage_SerializeIsCalled()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            Assert.IsTrue(TestMessage.Serialized);
        }

        [Test]
        public void WhenSendingMessage_NothingIsSentBeforeProcessingSendQueue()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            Assert.IsEmpty(m_MessageSender.MessageQueue);
        }

        [Test]
        public void WhenProcessingSendQueue_MessageIsSent()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenSendingMultipleMessages_MessagesAreBatched()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenNotExceedingBatchSize_NewBatchesAreNotCreated()
        {
            var message = GetMessage();
            var size = UnsafeUtility.SizeOf<TestMessage>() + UnsafeUtility.SizeOf<MessageHeader>();
            for (var i = 0; i < 1300 / size; ++i)
            {
                m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            }

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenExceedingBatchSize_NewBatchesAreCreated()
        {
            var message = GetMessage();
            var size = UnsafeUtility.SizeOf<TestMessage>() + UnsafeUtility.SizeOf<MessageHeader>();
            for (var i = 0; i < (1300 / size) + 1; ++i)
            {
                m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            }

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(2, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenExceedingMTUSizeWithFragmentedDelivery_NewBatchesAreNotCreated()
        {
            var message = GetMessage();
            var size = UnsafeUtility.SizeOf<TestMessage>() + UnsafeUtility.SizeOf<MessageHeader>();
            for (var i = 0; i < (1300 / size) + 1; ++i)
            {
                m_MessagingSystem.SendMessage(message, NetworkDelivery.ReliableFragmentedSequenced, m_Clients);
            }

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenSwitchingDelivery_NewBatchesAreCreated()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Unreliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(2, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenSwitchingChannel_NewBatchesAreNotCreated()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenSendingMessaged_SentDataIsCorrect()
        {
            var message = GetMessage();
            var message2 = GetMessage();
            m_MessagingSystem.SendMessage(message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(message2, NetworkDelivery.Reliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            var reader = new FastBufferReader(m_MessageSender.MessageQueue[0], Allocator.Temp);
            using (reader)
            {
                reader.TryBeginRead(
                    FastBufferWriter.GetWriteSize<BatchHeader>() +
                    FastBufferWriter.GetWriteSize<MessageHeader>() * 2 +
                    FastBufferWriter.GetWriteSize<TestMessage>() * 2
                );
                reader.ReadValue(out BatchHeader header);
                Assert.AreEqual(2, header.BatchSize);

                reader.ReadValue(out MessageHeader messageHeader);
                Assert.AreEqual(m_MessagingSystem.GetMessageType(typeof(TestMessage)), messageHeader.MessageType);
                Assert.AreEqual(UnsafeUtility.SizeOf<TestMessage>(), messageHeader.MessageSize);
                reader.ReadValue(out TestMessage receivedMessage);
                Assert.AreEqual(message, receivedMessage);

                reader.ReadValue(out MessageHeader messageHeader2);
                Assert.AreEqual(m_MessagingSystem.GetMessageType(typeof(TestMessage)), messageHeader2.MessageType);
                Assert.AreEqual(UnsafeUtility.SizeOf<TestMessage>(), messageHeader2.MessageSize);
                reader.ReadValue(out TestMessage receivedMessage2);
                Assert.AreEqual(message2, receivedMessage2);
            }
        }
    }
}
