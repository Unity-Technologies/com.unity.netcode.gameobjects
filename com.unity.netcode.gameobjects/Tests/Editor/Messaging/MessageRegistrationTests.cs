
using NUnit.Framework;
using Unity.Netcode.Transports.UNET;

namespace Unity.Netcode.EditorTests
{
    public class MessageRegistrationTests
    {
        class MessagingSystemOwnerOne
        {
            
        }

        class MessagingSystemOwnerTwo
        {
            
        }
        
        [Bind(typeof(MessagingSystemOwnerOne))]
        struct TestMessageOne : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            
            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(ref FastBufferReader reader, NetworkContext context)
            {
                
            }
        }
        
        [Bind(typeof(MessagingSystemOwnerOne))]
        struct TestMessageTwo : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            
            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(ref FastBufferReader reader, NetworkContext context)
            {
                
            }
        }
        
        [Bind(typeof(MessagingSystemOwnerTwo))]
        struct TestMessageThree : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            
            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(ref FastBufferReader reader, NetworkContext context)
            {
                
            }
        }
        
        [Bind(null)]
        struct TestMessageFour : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            
            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(ref FastBufferReader reader, NetworkContext context)
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

        #region WhenCreatingMessageSystem_MissingReceiveHandlerThrowsException
        class BrokenSystemOwnerOne
        {
            
        }
        
        [Bind(typeof(BrokenSystemOwnerOne))]
        struct TestMessageFive : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            
            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }
        }

        [Test]
        public void WhenCreatingMessageSystem_MissingReceiveHandlerThrowsException()
        {
            var owner = new BrokenSystemOwnerOne();
            var sender = new NopMessageSender();
            Assert.Throws<InvalidMessageStructureException>(() => new MessagingSystem(sender, owner));
        }
        #endregion

        #region WhenCreatingMessageSystem_ReceiveHandlerWithIncorrectParametersThrowsException
        class BrokenSystemOwnerTwo
        {
            
        }
        
        [Bind(typeof(BrokenSystemOwnerTwo))]
        struct TestMessageSix : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            
            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(ref FastBufferReader reader)
            {
                
            }
        }

        [Test]
        public void WhenCreatingMessageSystem_ReceiveHandlerWithIncorrectParametersThrowsException()
        {
            var owner = new BrokenSystemOwnerTwo();
            var sender = new NopMessageSender();
            Assert.Throws<InvalidMessageStructureException>(() => new MessagingSystem(sender, owner));
        }
        #endregion

        #region WhenCreatingMessageSystem_ReceiveHandlerWithMissingRefSpecifierThrowsException
        class BrokenSystemOwnerThree
        {
            
        }
        
        [Bind(typeof(BrokenSystemOwnerThree))]
        struct TestMessageSeven : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            
            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValue(this);
            }

            public static void Receive(FastBufferReader reader, NetworkContext context)
            {
                
            }
        }

        [Test]
        public void WhenCreatingMessageSystem_ReceiveHandlerWithMissingRefSpecifierThrowsException()
        {
            var owner = new BrokenSystemOwnerThree();
            var sender = new NopMessageSender();
            Assert.Throws<InvalidMessageStructureException>(() => new MessagingSystem(sender, owner));
        }
        #endregion
    }
}