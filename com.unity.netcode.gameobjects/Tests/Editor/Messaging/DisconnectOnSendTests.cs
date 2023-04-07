using System.Collections.Generic;
using NUnit.Framework;

namespace Unity.Netcode.EditorTests
{
    public class DisconnectOnSendTests
    {
        private struct TestMessage : INetworkMessage, INetworkSerializeByMemcpy
        {
            public void Serialize(FastBufferWriter writer, int targetVersion)
            {
            }

            public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
            {
                return true;
            }

            public void Handle(ref NetworkContext context)
            {
            }

            public int Version => 0;
        }

        private class DisconnectOnSendMessageSender : IMessageSender
        {
            public MessagingSystem MessagingSystem;

            public void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData)
            {
                MessagingSystem.ClientDisconnected(clientId);
            }
        }
        private class TestMessageProvider : IMessageProvider
        {
            // Keep track of what we sent
            private List<MessagingSystem.MessageWithHandler> m_MessageList = new List<MessagingSystem.MessageWithHandler>{
                new MessagingSystem.MessageWithHandler
                {
                    MessageType = typeof(TestMessage),
                    Handler = MessagingSystem.ReceiveMessage<TestMessage>,
                    GetVersion = MessagingSystem.CreateMessageAndGetVersion<TestMessage>
                }
            };

            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                return m_MessageList;
            }
        }

        private TestMessageProvider m_TestMessageProvider;
        private DisconnectOnSendMessageSender m_MessageSender;
        private MessagingSystem m_MessagingSystem;
        private ulong[] m_Clients = { 0 };

        [SetUp]
        public void SetUp()
        {
            m_MessageSender = new DisconnectOnSendMessageSender();
            m_TestMessageProvider = new TestMessageProvider();
            m_MessagingSystem = new MessagingSystem(m_MessageSender, this, m_TestMessageProvider);
            m_MessageSender.MessagingSystem = m_MessagingSystem;
            m_MessagingSystem.ClientConnected(0);
            m_MessagingSystem.SetVersion(0, XXHash.Hash32(typeof(TestMessage).FullName), 0);
        }

        [TearDown]
        public void TearDown()
        {
            m_MessagingSystem.Dispose();
        }

        private TestMessage GetMessage()
        {
            return new TestMessage();
        }

        [Test]
        public void WhenDisconnectIsCalledDuringSend_NoErrorsOccur()
        {
            var message = GetMessage();
            m_MessagingSystem.SendMessage(ref message, NetworkDelivery.Reliable, m_Clients);

            // This is where an exception would be thrown and logged.
            m_MessagingSystem.ProcessSendQueues();
        }
    }
}
