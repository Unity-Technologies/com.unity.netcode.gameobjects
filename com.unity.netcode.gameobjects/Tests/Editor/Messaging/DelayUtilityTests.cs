using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Netcode.EditorTests
{
    public class DelayUtilityTests
    {
        [Bind(typeof(DelayUtilityTests))]
        struct TestMessage : INetworkMessage
        {
            public int A;
            public int B;
            public int C;
            public static bool Deserialized;
            public static bool Delayed;
            public static DelayUtility DelayUtility;
            public static DelayUtility.DelayStage DelayStage;

            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValueSafe(this);
            }

            public static void Receive(ref FastBufferReader reader, NetworkContext context)
            {
                if (DelayUtility.DelayUntil(DelayStage, ref reader, context) == DelayUtility.DelayResult.Delay)
                {
                    Delayed = true;
                    return;
                }
                Deserialized = true;
                reader.ReadValueSafe(out TestMessage value);
            }
        }

        private MessagingSystem m_MessagingSystem;
        private DelayUtility m_DelayUtility;
        private NetworkUpdateStage m_PreviousStage;
        
        [SetUp]
        public void SetUp()
        {
            TestMessage.Deserialized = false;
            TestMessage.Delayed = false;
         
            m_MessagingSystem = new MessagingSystem(new NopMessageSender(), this);
            m_DelayUtility = new DelayUtility(m_MessagingSystem);
            TestMessage.DelayUtility = m_DelayUtility;
            m_PreviousStage = NetworkUpdateLoop.UpdateStage;
        }

        [TearDown]
        public void TearDown()
        {
            m_MessagingSystem.Dispose();
            m_DelayUtility.Dispose();
            NetworkUpdateLoop.UpdateStage = m_PreviousStage;
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
        public void WhenDelayingALambda_TheLambdaIsCalledAtTheCorrectStage([Values]DelayUtility.DelayStage stage)
        {
            bool called = false;
            m_DelayUtility.DelayUntil(stage, () => { called = true; });
            foreach (var networkStage in Enum.GetValues(typeof(DelayUtility.DelayStage)))
            {
                m_DelayUtility.NetworkUpdate((NetworkUpdateStage)networkStage);
                Assert.AreEqual((byte) networkStage == (byte) stage, called);
                called = false;
            }
        }

        [Test]
        public void AfterProcessingALambda_LambdaDoesNotGetProcessedAgain([Values]DelayUtility.DelayStage stage)
        {
            bool called = false;
            m_DelayUtility.DelayUntil(stage, () => { called = true; });
            foreach (var networkStage in Enum.GetValues(typeof(DelayUtility.DelayStage)))
            {
                m_DelayUtility.NetworkUpdate((NetworkUpdateStage)networkStage);
                Assert.AreEqual((byte) networkStage == (byte) stage, called);
                called = false;
            }
            foreach (var networkStage in Enum.GetValues(typeof(DelayUtility.DelayStage)))
            {
                m_DelayUtility.NetworkUpdate((NetworkUpdateStage)networkStage);
                Assert.IsFalse(called);
            }
        }

        [Test]
        public void WhenDelayingAMessage_TheMessageIsProcessedAtTheCorrectStage([Values]DelayUtility.DelayStage stage)
        {
            TestMessage.DelayStage = stage;
            var batchHeader = new BatchHeader
            {
                BatchSize = 1
            };
            var messageHeader = new MessageHeader
            {
                MessageSize = (short) UnsafeUtility.SizeOf<TestMessage>(),
                MessageType = m_MessagingSystem.GetMessageType(typeof(TestMessage)),
                NetworkChannel = NetworkChannel.Internal
            };
            var message = GetMessage();

            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.TryBeginWrite(FastBufferWriter.GetWriteSize(batchHeader) + 
                                     FastBufferWriter.GetWriteSize(messageHeader) + 
                                     FastBufferWriter.GetWriteSize(message));
                writer.WriteValue(batchHeader);
                writer.WriteValue(messageHeader);
                writer.WriteValue(message);
                
                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                NetworkUpdateLoop.UpdateStage = NetworkUpdateStage.Initialization;
                using (reader)
                {
                    m_MessagingSystem.HandleIncomingData(0, new ArraySegment<byte>(writer.ToArray()), 0);
                    m_MessagingSystem.ProcessIncomingMessageQueue();
                    Assert.IsFalse(TestMessage.Deserialized);
                    Assert.IsTrue(TestMessage.Delayed);
                }
                
                foreach (var networkStage in Enum.GetValues(typeof(DelayUtility.DelayStage)))
                {
                    NetworkUpdateLoop.UpdateStage = (NetworkUpdateStage)networkStage;
                    m_DelayUtility.NetworkUpdate((NetworkUpdateStage)networkStage);
                    Assert.AreEqual((byte) networkStage == (byte) stage, TestMessage.Deserialized);
                    TestMessage.Deserialized = false;
                }
            }
        }

        [Test]
        public void AfterProcessingAMessage_MessageDoesNotGetProcessedAgain([Values]DelayUtility.DelayStage stage)
        {
            TestMessage.DelayStage = stage;
            var batchHeader = new BatchHeader
            {
                BatchSize = 1
            };
            var messageHeader = new MessageHeader
            {
                MessageSize = (short) UnsafeUtility.SizeOf<TestMessage>(),
                MessageType = m_MessagingSystem.GetMessageType(typeof(TestMessage)),
                NetworkChannel = NetworkChannel.Internal
            };
            var message = GetMessage();

            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.TryBeginWrite(FastBufferWriter.GetWriteSize(batchHeader) + 
                                     FastBufferWriter.GetWriteSize(messageHeader) + 
                                     FastBufferWriter.GetWriteSize(message));
                writer.WriteValue(batchHeader);
                writer.WriteValue(messageHeader);
                writer.WriteValue(message);
                
                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                NetworkUpdateLoop.UpdateStage = NetworkUpdateStage.Initialization;
                using (reader)
                {
                    m_MessagingSystem.HandleIncomingData(0, new ArraySegment<byte>(writer.ToArray()), 0);
                    m_MessagingSystem.ProcessIncomingMessageQueue();
                    Assert.IsFalse(TestMessage.Deserialized);
                    Assert.IsTrue(TestMessage.Delayed);
                }
                
                foreach (var networkStage in Enum.GetValues(typeof(DelayUtility.DelayStage)))
                {
                    NetworkUpdateLoop.UpdateStage = (NetworkUpdateStage)networkStage;
                    m_DelayUtility.NetworkUpdate((NetworkUpdateStage)networkStage);
                    Assert.AreEqual((byte) networkStage == (byte) stage, TestMessage.Deserialized);
                    TestMessage.Deserialized = false;
                }
                
                foreach (var networkStage in Enum.GetValues(typeof(DelayUtility.DelayStage)))
                {
                    NetworkUpdateLoop.UpdateStage = (NetworkUpdateStage)networkStage;
                    m_DelayUtility.NetworkUpdate((NetworkUpdateStage)networkStage);
                    Assert.AreEqual(false, TestMessage.Deserialized);
                }
            }
        }
    }
}