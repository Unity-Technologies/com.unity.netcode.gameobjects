
using NUnit.Framework;

namespace Unity.Netcode.EditorTests
{
    public class MessageRegistrationTests
    {
        private class MessagingSystemOwnerOne
        {

        }

        private class MessagingSystemOwnerTwo
        {

        }

        [IgnoreMessageIfSystemOwnerIsNotOfType(typeof(MessagingSystemOwnerOne))]
        private struct TestMessageOne : INetworkMessage
        {
            public int A;
            public int B;
            public int C;

            public void Serialize(FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(FastBufferReader reader, in NetworkContext context)
            {

            }
        }

        [IgnoreMessageIfSystemOwnerIsNotOfType(typeof(MessagingSystemOwnerOne))]
        private struct TestMessageTwo : INetworkMessage
        {
            public int A;
            public int B;
            public int C;

            public void Serialize(FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(FastBufferReader reader, in NetworkContext context)
            {

            }
        }

        [IgnoreMessageIfSystemOwnerIsNotOfType(typeof(MessagingSystemOwnerTwo))]
        private struct TestMessageThree : INetworkMessage
        {
            public int A;
            public int B;
            public int C;

            public void Serialize(FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(FastBufferReader reader, in NetworkContext context)
            {

            }
        }

        [IgnoreMessageIfSystemOwnerIsNotOfType(null)]
        private struct TestMessageFour : INetworkMessage
        {
            public int A;
            public int B;
            public int C;

            public void Serialize(FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(FastBufferReader reader, in NetworkContext context)
            {

            }
        }

        [Test]
        public void WhenCreatingMessageSystem_OnlyBoundTypesAreRegistered()
        {
            var ownerOne = new MessagingSystemOwnerOne();
            var ownerTwo = new MessagingSystemOwnerTwo();
            var sender = new NopMessageSender();

            var systemOne = new MessagingSystem(sender, ownerOne);
            var systemTwo = new MessagingSystem(sender, ownerTwo);
            var systemThree = new MessagingSystem(sender, null);

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
            var ownerOne = new MessagingSystemOwnerOne();
            var ownerTwo = new MessagingSystemOwnerTwo();
            var sender = new NopMessageSender();

            var systemOne = new MessagingSystem(sender, ownerOne);
            var systemTwo = new MessagingSystem(sender, ownerTwo);
            var systemThree = new MessagingSystem(sender, null);

            using (systemOne)
            using (systemTwo)
            using (systemThree)
            {
                MessagingSystem.MessageHandler handlerOne = TestMessageOne.Receive;
                MessagingSystem.MessageHandler handlerTwo = TestMessageTwo.Receive;
                MessagingSystem.MessageHandler handlerThree = TestMessageThree.Receive;
                MessagingSystem.MessageHandler handlerFour = TestMessageFour.Receive;

                var foundHandlerOne = systemOne.MessageHandlers[systemOne.GetMessageType(typeof(TestMessageOne))];

                Assert.AreEqual(handlerOne,
                    systemOne.MessageHandlers[systemOne.GetMessageType(typeof(TestMessageOne))]);
                Assert.AreEqual(handlerTwo,
                    systemOne.MessageHandlers[systemOne.GetMessageType(typeof(TestMessageTwo))]);
                Assert.AreEqual(handlerThree,
                    systemTwo.MessageHandlers[systemTwo.GetMessageType(typeof(TestMessageThree))]);
                Assert.AreEqual(handlerFour,
                    systemThree.MessageHandlers[systemThree.GetMessageType(typeof(TestMessageFour))]);
            }
        }
    }
}
