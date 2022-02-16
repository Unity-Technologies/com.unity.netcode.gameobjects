using System.Collections.Generic;
using NUnit.Framework;

namespace Unity.Netcode.EditorTests
{
    public class MessageRegistrationTests
    {
        private struct TestMessageOne : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            public void Serialize(FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
            {
                return true;
            }

            public void Handle(ref NetworkContext context)
            {
            }
        }

        private struct TestMessageTwo : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            public void Serialize(FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
            {
                return true;
            }

            public void Handle(ref NetworkContext context)
            {
            }
        }
        private class TestMessageProviderOne : IMessageProvider
        {
            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                return new List<MessagingSystem.MessageWithHandler>
                {
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessageOne),
                        Handler = MessagingSystem.ReceiveMessage<TestMessageOne>
                    },
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessageTwo),
                        Handler = MessagingSystem.ReceiveMessage<TestMessageTwo>
                    }
                };
            }
        }

        private struct TestMessageThree : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            public void Serialize(FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
            {
                return true;
            }

            public void Handle(ref NetworkContext context)
            {
            }
        }
        private class TestMessageProviderTwo : IMessageProvider
        {
            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                return new List<MessagingSystem.MessageWithHandler>
                {
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessageThree),
                        Handler = MessagingSystem.ReceiveMessage<TestMessageThree>
                    }
                };
            }
        }
        private struct TestMessageFour : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            public void Serialize(FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
            {
                return true;
            }

            public void Handle(ref NetworkContext context)
            {
            }
        }
        private class TestMessageProviderThree : IMessageProvider
        {
            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                return new List<MessagingSystem.MessageWithHandler>
                {
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessageFour),
                        Handler = MessagingSystem.ReceiveMessage<TestMessageFour>
                    }
                };
            }
        }

        [Test]
        public void WhenCreatingMessageSystem_OnlyProvidedTypesAreRegistered()
        {
            var sender = new NopMessageSender();

            var systemOne = new MessagingSystem(sender, null, new TestMessageProviderOne());
            var systemTwo = new MessagingSystem(sender, null, new TestMessageProviderTwo());
            var systemThree = new MessagingSystem(sender, null, new TestMessageProviderThree());

            using (systemOne)
            using (systemTwo)
            using (systemThree)
            {
                Assert.AreEqual(2, systemOne.MessageHandlerCount);
                Assert.AreEqual(1, systemTwo.MessageHandlerCount);
                Assert.AreEqual(1, systemThree.MessageHandlerCount);

                Assert.Contains(typeof(TestMessageOne), systemOne.MessageTypes);
                Assert.Contains(typeof(TestMessageTwo), systemOne.MessageTypes);
                Assert.Contains(typeof(TestMessageThree), systemTwo.MessageTypes);
                Assert.Contains(typeof(TestMessageFour), systemThree.MessageTypes);
            }
        }

        [Test]
        public void WhenCreatingMessageSystem_BoundTypeMessageHandlersAreRegistered()
        {
            var sender = new NopMessageSender();

            var systemOne = new MessagingSystem(sender, null, new TestMessageProviderOne());
            var systemTwo = new MessagingSystem(sender, null, new TestMessageProviderTwo());
            var systemThree = new MessagingSystem(sender, null, new TestMessageProviderThree());

            using (systemOne)
            using (systemTwo)
            using (systemThree)
            {
                MessagingSystem.MessageHandler handlerOne = MessagingSystem.ReceiveMessage<TestMessageOne>;
                MessagingSystem.MessageHandler handlerTwo = MessagingSystem.ReceiveMessage<TestMessageTwo>;
                MessagingSystem.MessageHandler handlerThree = MessagingSystem.ReceiveMessage<TestMessageThree>;
                MessagingSystem.MessageHandler handlerFour = MessagingSystem.ReceiveMessage<TestMessageFour>;

                var foundHandlerOne = systemOne.MessageHandlers[systemOne.GetMessageType(typeof(TestMessageOne))];

                Assert.AreEqual(handlerOne, systemOne.MessageHandlers[systemOne.GetMessageType(typeof(TestMessageOne))]);
                Assert.AreEqual(handlerTwo, systemOne.MessageHandlers[systemOne.GetMessageType(typeof(TestMessageTwo))]);
                Assert.AreEqual(handlerThree, systemTwo.MessageHandlers[systemTwo.GetMessageType(typeof(TestMessageThree))]);
                Assert.AreEqual(handlerFour, systemThree.MessageHandlers[systemThree.GetMessageType(typeof(TestMessageFour))]);
            }
        }
    }
}
