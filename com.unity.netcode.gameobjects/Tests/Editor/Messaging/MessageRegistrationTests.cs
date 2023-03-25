using System.Collections.Generic;
using NUnit.Framework;

namespace Unity.Netcode.EditorTests
{
    public class MessageRegistrationTests
    {
        private struct TestMessageOne : INetworkMessage, INetworkSerializeByMemcpy
        {
            public int A;
            public int B;
            public int C;
            public void Serialize(FastBufferWriter writer, int targetVersion)
            {
                writer.WriteValue(this);
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

        private struct TestMessageTwo : INetworkMessage, INetworkSerializeByMemcpy
        {
            public int A;
            public int B;
            public int C;
            public void Serialize(FastBufferWriter writer, int targetVersion)
            {
                writer.WriteValue(this);
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
        private class TestMessageProviderOne : IMessageProvider
        {
            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                return new List<MessagingSystem.MessageWithHandler>
                {
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessageOne),
                        Handler = MessagingSystem.ReceiveMessage<TestMessageOne>,
                        GetVersion = MessagingSystem.CreateMessageAndGetVersion<TestMessageOne>
                    },
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessageTwo),
                        Handler = MessagingSystem.ReceiveMessage<TestMessageTwo>,
                        GetVersion = MessagingSystem.CreateMessageAndGetVersion<TestMessageTwo>
                    }
                };
            }
        }

        private struct TestMessageThree : INetworkMessage, INetworkSerializeByMemcpy
        {
            public int A;
            public int B;
            public int C;
            public void Serialize(FastBufferWriter writer, int targetVersion)
            {
                writer.WriteValue(this);
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
        private class TestMessageProviderTwo : IMessageProvider
        {
            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                return new List<MessagingSystem.MessageWithHandler>
                {
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessageThree),
                        Handler = MessagingSystem.ReceiveMessage<TestMessageThree>,
                        GetVersion = MessagingSystem.CreateMessageAndGetVersion<TestMessageThree>
                    }
                };
            }
        }
        private struct TestMessageFour : INetworkMessage, INetworkSerializeByMemcpy
        {
            public int A;
            public int B;
            public int C;
            public void Serialize(FastBufferWriter writer, int targetVersion)
            {
                writer.WriteValue(this);
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
        private class TestMessageProviderThree : IMessageProvider
        {
            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                return new List<MessagingSystem.MessageWithHandler>
                {
                    new MessagingSystem.MessageWithHandler
                    {
                        MessageType = typeof(TestMessageFour),
                        Handler = MessagingSystem.ReceiveMessage<TestMessageFour>,
                        GetVersion = MessagingSystem.CreateMessageAndGetVersion<TestMessageFour>
                    }
                };
            }
        }

        [Test]
        public void WhenCreatingMessageSystem_OnlyProvidedTypesAreRegistered()
        {
            var sender = new NopMessageSender();

            using var systemOne = new MessagingSystem(sender, null, new TestMessageProviderOne());
            using var systemTwo = new MessagingSystem(sender, null, new TestMessageProviderTwo());
            using var systemThree = new MessagingSystem(sender, null, new TestMessageProviderThree());

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

            using var systemOne = new MessagingSystem(sender, null, new TestMessageProviderOne());
            using var systemTwo = new MessagingSystem(sender, null, new TestMessageProviderTwo());
            using var systemThree = new MessagingSystem(sender, null, new TestMessageProviderThree());

            using (systemOne)
            using (systemTwo)
            using (systemThree)
            {
                MessagingSystem.MessageHandler handlerOne = MessagingSystem.ReceiveMessage<TestMessageOne>;
                MessagingSystem.MessageHandler handlerTwo = MessagingSystem.ReceiveMessage<TestMessageTwo>;
                MessagingSystem.MessageHandler handlerThree = MessagingSystem.ReceiveMessage<TestMessageThree>;
                MessagingSystem.MessageHandler handlerFour = MessagingSystem.ReceiveMessage<TestMessageFour>;

                Assert.AreEqual(handlerOne, systemOne.MessageHandlers[systemOne.GetMessageType(typeof(TestMessageOne))]);
                Assert.AreEqual(handlerTwo, systemOne.MessageHandlers[systemOne.GetMessageType(typeof(TestMessageTwo))]);
                Assert.AreEqual(handlerThree, systemTwo.MessageHandlers[systemTwo.GetMessageType(typeof(TestMessageThree))]);
                Assert.AreEqual(handlerFour, systemThree.MessageHandlers[systemThree.GetMessageType(typeof(TestMessageFour))]);
            }
        }

        internal class AAAEarlyLexicographicNetworkMessage : INetworkMessage
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

#pragma warning disable IDE1006
        internal class zzzLateLexicographicNetworkMessage : AAAEarlyLexicographicNetworkMessage
        {
        }
#pragma warning restore IDE1006

        internal class OrderingMessageProvider : IMessageProvider
        {
            public List<MessagingSystem.MessageWithHandler> GetMessages()
            {
                var listMessages = new List<MessagingSystem.MessageWithHandler>();

                var messageWithHandler = new MessagingSystem.MessageWithHandler
                {
                    MessageType = typeof(zzzLateLexicographicNetworkMessage),
                    GetVersion = MessagingSystem.CreateMessageAndGetVersion<zzzLateLexicographicNetworkMessage>
                };
                listMessages.Add(messageWithHandler);

                messageWithHandler.MessageType = typeof(ConnectionRequestMessage);
                messageWithHandler.GetVersion = MessagingSystem.CreateMessageAndGetVersion<ConnectionRequestMessage>;
                listMessages.Add(messageWithHandler);

                messageWithHandler.MessageType = typeof(ConnectionApprovedMessage);
                messageWithHandler.GetVersion = MessagingSystem.CreateMessageAndGetVersion<ConnectionApprovedMessage>;
                listMessages.Add(messageWithHandler);

                messageWithHandler.MessageType = typeof(AAAEarlyLexicographicNetworkMessage);
                messageWithHandler.GetVersion = MessagingSystem.CreateMessageAndGetVersion<AAAEarlyLexicographicNetworkMessage>;
                listMessages.Add(messageWithHandler);

                return listMessages;
            }
        }

        [Test]
        public void MessagesGetPrioritizedCorrectly()
        {
            var sender = new NopMessageSender();
            var provider = new OrderingMessageProvider();
            using var messagingSystem = new MessagingSystem(sender, null, provider);

            // the 2 priority messages should appear first, in lexicographic order
            Assert.AreEqual(messagingSystem.MessageTypes[0], typeof(ConnectionApprovedMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[1], typeof(ConnectionRequestMessage));

            // the other should follow after
            Assert.AreEqual(messagingSystem.MessageTypes[2], typeof(AAAEarlyLexicographicNetworkMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[3], typeof(zzzLateLexicographicNetworkMessage));

            // there should not be any extras
            Assert.AreEqual(messagingSystem.MessageHandlerCount, 4);
        }
    }
}
