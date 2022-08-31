using System;
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

        private struct TestMessageTwo : INetworkMessage, INetworkSerializeByMemcpy
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

        private struct TestMessageThree : INetworkMessage, INetworkSerializeByMemcpy
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
        private struct TestMessageFour : INetworkMessage, INetworkSerializeByMemcpy
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
            public void Serialize(FastBufferWriter writer)
            {
            }

            public bool Deserialize(FastBufferReader reader, ref NetworkContext context)
            {
                return true;
            }

            public void Handle(ref NetworkContext context)
            {
            }
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

                var messageWithHandler = new MessagingSystem.MessageWithHandler();

                messageWithHandler.MessageType = typeof(zzzLateLexicographicNetworkMessage);
                listMessages.Add(messageWithHandler);

                messageWithHandler.MessageType = typeof(ConnectionRequestMessage);
                listMessages.Add(messageWithHandler);

                messageWithHandler.MessageType = typeof(ConnectionApprovedMessage);
                listMessages.Add(messageWithHandler);

                messageWithHandler.MessageType = typeof(OrderingMessage);
                listMessages.Add(messageWithHandler);

                messageWithHandler.MessageType = typeof(AAAEarlyLexicographicNetworkMessage);
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

            // the 3 priority messages should appear first, in lexicographic order
            Assert.AreEqual(messagingSystem.MessageTypes[0], typeof(ConnectionApprovedMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[1], typeof(ConnectionRequestMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[2], typeof(OrderingMessage));

            // the other should follow after
            Assert.AreEqual(messagingSystem.MessageTypes[3], typeof(AAAEarlyLexicographicNetworkMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[4], typeof(zzzLateLexicographicNetworkMessage));

            // there should not be any extras
            Assert.AreEqual(messagingSystem.MessageHandlerCount, 5);

            // reorder the zzz one to position 3
            messagingSystem.ReorderMessage(3, XXHash.Hash32(typeof(zzzLateLexicographicNetworkMessage).FullName));

            // the 3 priority messages should still appear first, in lexicographic order
            Assert.AreEqual(messagingSystem.MessageTypes[0], typeof(ConnectionApprovedMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[1], typeof(ConnectionRequestMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[2], typeof(OrderingMessage));

            // the other should follow after, but reordered
            Assert.AreEqual(messagingSystem.MessageTypes[3], typeof(zzzLateLexicographicNetworkMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[4], typeof(AAAEarlyLexicographicNetworkMessage));

            // there should still not be any extras
            Assert.AreEqual(messagingSystem.MessageHandlerCount, 5);

            // verify we get an exception when asking for an invalid position
            try
            {
                messagingSystem.ReorderMessage(-1, XXHash.Hash32(typeof(zzzLateLexicographicNetworkMessage).FullName));
                Assert.Fail();
            }
            catch (ArgumentException)
            {
            }

            // reorder the zzz one to position 3, again, to check nothing bad happens
            messagingSystem.ReorderMessage(3, XXHash.Hash32(typeof(zzzLateLexicographicNetworkMessage).FullName));

            // the two non-priority should not have moved
            Assert.AreEqual(messagingSystem.MessageTypes[3], typeof(zzzLateLexicographicNetworkMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[4], typeof(AAAEarlyLexicographicNetworkMessage));

            // there should still not be any extras
            Assert.AreEqual(messagingSystem.MessageHandlerCount, 5);

            // 4242 is a random hash that should not match anything
            messagingSystem.ReorderMessage(3, 4242);

            // that should result in an extra entry
            Assert.AreEqual(messagingSystem.MessageHandlerCount, 6);

            // with a null handler
            Assert.AreEqual(messagingSystem.MessageHandlers[3], null);

            // and it should have bumped the previous messages down
            Assert.AreEqual(messagingSystem.MessageTypes[4], typeof(zzzLateLexicographicNetworkMessage));
            Assert.AreEqual(messagingSystem.MessageTypes[5], typeof(AAAEarlyLexicographicNetworkMessage));
        }
    }
}
