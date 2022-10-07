using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TestTools;
using Random = System.Random;

namespace Unity.Netcode.EditorTests
{
    public class MessageSendingTests
    {
        private struct TestMessage : INetworkMessage, INetworkSerializeByMemcpy
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

            public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
            {
                return true;
            }

            public void Handle(ref NetworkContext context)
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

        private class TestMessageProvider : IMessageProvider, IDisposable
        {
            // Keep track of what we sent
            private List<List<MessagingSystem.MessageWithHandler>> m_CachedMessages = new List<List<MessagingSystem.MessageWithHandler>>();

            public void Dispose()
            {
                foreach (var cachedItem in m_CachedMessages)
                {
                    // Clear out any references to MessagingSystem.MessageWithHandlers
                    cachedItem.Clear();
                }
                m_CachedMessages.Clear();
            }

            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                var messageList = new List<MessagingSystem.MessageWithHandler>
                {
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessage),
                        Handler = MessagingSystem.ReceiveMessage<TestMessage>
                    }
                };
                // Track messages sent
                m_CachedMessages.Add(messageList);
                return messageList;
            }
        }

        private TestMessageProvider m_TestMessageProvider;
        private TestMessageSender m_MessageSender;
        private MessagingSystem m_MessagingSystem;
        private ulong[] m_Clients = { 0 };

        [SetUp]
        public void SetUp()
        {
            TestMessage.Serialized = false;
            m_MessageSender = new TestMessageSender();
            m_TestMessageProvider = new TestMessageProvider();
            m_MessagingSystem = new MessagingSystem(m_MessageSender, this, m_TestMessageProvider);
            m_MessagingSystem.ClientConnected(0);
        }

        [TearDown]
        public void TearDown()
        {
            m_TestMessageProvider.Dispose();
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
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            Assert.IsTrue(TestMessage.Serialized);
        }

        [Test]
        public void WhenSendingMessage_NothingIsSentBeforeProcessingSendQueue()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            Assert.IsEmpty(m_MessageSender.MessageQueue);
        }

        [Test]
        public void WhenProcessingSendQueue_MessageIsSent()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenSendingMultipleMessages_MessagesAreBatched()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenNotExceedingBatchSize_NewBatchesAreNotCreated()
        {
            var message = GetMessage();
            var size = UnsafeUtility.SizeOf<TestMessage>() + 2; // MessageHeader packed with this message will be 2 bytes
            for (var i = 0; i < 1300 / size; ++i)
            {
                m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            }

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenExceedingBatchSize_NewBatchesAreCreated()
        {
            var message = GetMessage();
            var size = UnsafeUtility.SizeOf<TestMessage>() + 2; // MessageHeader packed with this message will be 2 bytes
            for (var i = 0; i < (1300 / size) + 1; ++i)
            {
                m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            }

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(2, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenExceedingMTUSizeWithFragmentedDelivery_NewBatchesAreNotCreated()
        {
            var message = GetMessage();
            var size = UnsafeUtility.SizeOf<TestMessage>() + 2; // MessageHeader packed with this message will be 2 bytes
            for (var i = 0; i < (1300 / size) + 1; ++i)
            {
                m_MessagingSystem.SendMessage(ref message, NetworkDelivery.ReliableFragmentedSequenced, m_Clients);
            }

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenSwitchingDelivery_NewBatchesAreCreated()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Unreliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(2, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenSwitchingChannel_NewBatchesAreNotCreated()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            Assert.AreEqual(1, m_MessageSender.MessageQueue.Count);
        }

        [Test]
        public void WhenSendingMessaged_SentDataIsCorrect()
        {
            var message = GetMessage();
            var message2 = GetMessage();
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);
            m_MessagingSystem.SendMessage(ref message2, NetworkDelivery.Reliable, m_Clients);

            m_MessagingSystem.ProcessSendQueues();
            var reader = new FastBufferReader(m_MessageSender.MessageQueue[0], Allocator.Temp);
            using (reader)
            {
                reader.ReadValueSafe(out BatchHeader header);
                Assert.AreEqual(2, header.BatchSize);

                MessageHeader messageHeader;

                ByteUnpacker.ReadValueBitPacked(reader, out messageHeader.MessageType);
                ByteUnpacker.ReadValueBitPacked(reader, out messageHeader.MessageSize);

                Assert.AreEqual(m_MessagingSystem.GetMessageType(typeof(TestMessage)), messageHeader.MessageType);
                Assert.AreEqual(UnsafeUtility.SizeOf<TestMessage>(), messageHeader.MessageSize);
                reader.ReadValueSafe(out TestMessage receivedMessage);
                Assert.AreEqual(message, receivedMessage);

                ByteUnpacker.ReadValueBitPacked(reader, out messageHeader.MessageType);
                ByteUnpacker.ReadValueBitPacked(reader, out messageHeader.MessageSize);

                Assert.AreEqual(m_MessagingSystem.GetMessageType(typeof(TestMessage)), messageHeader.MessageType);
                Assert.AreEqual(UnsafeUtility.SizeOf<TestMessage>(), messageHeader.MessageSize);
                reader.ReadValueSafe(out TestMessage receivedMessage2);
                Assert.AreEqual(message2, receivedMessage2);
            }
        }

        private class TestNoHandlerMessageProvider : IMessageProvider
        {
            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                return new List<MessagingSystem.MessageWithHandler>
                {
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessage),
                        Handler = null
                    }
                };
            }
        }

        [Test]
        public void WhenReceivingAMessageWithoutAHandler_ExceptionIsLogged()
        {
            // If a MessagingSystem already exists then dispose of it before creating a new MessagingSystem (otherwise memory leak)
            if (m_MessagingSystem != null)
            {
                m_MessagingSystem.Dispose();
                m_MessagingSystem = null;
            }

            // Since m_MessagingSystem is disposed during teardown we don't need to worry about that here.
            m_MessagingSystem = new MessagingSystem(new NopMessageSender(), this, new TestNoHandlerMessageProvider());
            m_MessagingSystem.ClientConnected(0);

            var messageHeader = new MessageHeader
            {
                MessageSize = (ushort)UnsafeUtility.SizeOf<TestMessage>(),
                MessageType = m_MessagingSystem.GetMessageType(typeof(TestMessage)),
            };
            var message = GetMessage();

            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.TryBeginWrite(FastBufferWriter.GetWriteSize(message));
                writer.WriteValue(message);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    m_MessagingSystem.HandleMessage(messageHeader, reader, 0, 0, 0);
                    LogAssert.Expect(LogType.Exception, new Regex(".*HandlerNotRegisteredException.*"));
                }
            }
        }
    }
}
