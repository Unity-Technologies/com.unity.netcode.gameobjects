using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.EditorTests
{
    public class MessageCorruptionTests
    {

        private struct TestMessage : INetworkMessage, INetworkSerializeByMemcpy
        {
            public ForceNetworkSerializeByMemcpy<Guid> Value;
            public static bool Handled;
            public static bool Deserialized;

            public void Serialize(FastBufferWriter writer, int targetVersion)
            {
                writer.WriteValueSafe(Value);
            }

            public bool Deserialize(FastBufferReader reader, ref NetworkContext context, int receivedMessageVersion)
            {
                Deserialized = true;
                reader.ReadValueSafe(out Value);
                return true;
            }

            public void Handle(ref NetworkContext context)
            {
                Handled = true;
            }

            public int Version => 0;
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
                        Handler = MessagingSystem.ReceiveMessage<TestMessage>,
                        GetVersion = MessagingSystem.CreateMessageAndGetVersion<TestMessage>
                    }
                };
            }
        }

        public enum TypeOfCorruption
        {
            OffsetPlus,
            OffsetMinus,
            CorruptBytes,
            Truncated,
            AdditionalGarbageData,
        }

        private class TestMessageSender : IMessageSender
        {

            public TypeOfCorruption Corruption;
            public List<byte[]> MessageQueue = new List<byte[]>();

            public unsafe void Send(ulong clientId, NetworkDelivery delivery, FastBufferWriter batchData)
            {
                switch (Corruption)
                {
                    case TypeOfCorruption.OffsetPlus:
                        {
                            using var subWriter = new FastBufferWriter(batchData.Length + 1, Allocator.Temp);
                            subWriter.WriteByteSafe(0);
                            subWriter.WriteBytesSafe(batchData.GetUnsafePtr(), batchData.Length);
                            MessageQueue.Add(subWriter.ToArray());
                            break;
                        }
                    case TypeOfCorruption.OffsetMinus:
                        {
                            using var subWriter = new FastBufferWriter(batchData.Length - 1, Allocator.Temp);
                            subWriter.WriteBytesSafe(batchData.GetUnsafePtr() + 1, batchData.Length - 1);
                            MessageQueue.Add(subWriter.ToArray());
                            break;
                        }
                    case TypeOfCorruption.CorruptBytes:
                        batchData.Seek(batchData.Length - 2);
                        var currentByte = batchData.GetUnsafePtr()[0];
                        batchData.WriteByteSafe((byte)(currentByte == 0 ? 1 : 0));
                        MessageQueue.Add(batchData.ToArray());
                        break;
                    case TypeOfCorruption.Truncated:
                        batchData.Truncate(batchData.Length - 1);
                        MessageQueue.Add(batchData.ToArray());
                        break;
                    case TypeOfCorruption.AdditionalGarbageData:
                        batchData.Seek(batchData.Length);
                        batchData.WriteByteSafe(0);
                        MessageQueue.Add(batchData.ToArray());
                        break;
                }
            }
        }

        private MessagingSystem m_MessagingSystem;
        private TestMessageSender m_MessageSender;

        [SetUp]
        public void SetUp()
        {
            TestMessage.Handled = false;
            TestMessage.Deserialized = false;
            m_MessageSender = new TestMessageSender();

            m_MessagingSystem = new MessagingSystem(m_MessageSender, this, new TestMessageProvider());

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
            return new TestMessage
            {
                Value = Guid.NewGuid()
            };
        }

        [Test]
        public unsafe void WhenPacketsAreCorrupted_TheyDontGetProcessed([Values] TypeOfCorruption typeOfCorruption)
        {
            m_MessageSender.Corruption = typeOfCorruption;

            switch (typeOfCorruption)
            {
                case TypeOfCorruption.OffsetMinus:
                case TypeOfCorruption.OffsetPlus:
                    LogAssert.Expect(LogType.Error, new Regex("Received a packet with an invalid Magic Value\\."));
                    break;
                case TypeOfCorruption.Truncated:
                case TypeOfCorruption.AdditionalGarbageData:
                    LogAssert.Expect(LogType.Error, new Regex("Received a packet with an invalid Batch Size Value\\."));
                    break;
                case TypeOfCorruption.CorruptBytes:
                    LogAssert.Expect(LogType.Error, new Regex("Received a packet with an invalid Hash Value\\."));
                    break;
            }

            // Dummy batch header
            var batchHeader = new BatchHeader
            {
                BatchCount = 1
            };
            var messageHeader = new MessageHeader
            {
                MessageSize = (ushort)UnsafeUtility.SizeOf<TestMessage>(),
                MessageType = m_MessagingSystem.GetMessageType(typeof(TestMessage)),
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

                // Fill out the rest of the batch header
                writer.Seek(0);
                batchHeader = new BatchHeader
                {
                    Magic = BatchHeader.MagicValue,
                    BatchSize = writer.Length,
                    BatchHash = XXHash.Hash64(writer.GetUnsafePtr() + sizeof(BatchHeader), writer.Length - sizeof(BatchHeader)),
                    BatchCount = 1
                };
                writer.WriteValue(batchHeader);
                m_MessageSender.Send(0, NetworkDelivery.Reliable, writer);

                var receivedMessage = m_MessageSender.MessageQueue[0];
                m_MessageSender.MessageQueue.Clear();
                m_MessagingSystem.HandleIncomingData(0, new ArraySegment<byte>(receivedMessage), 0);
                Assert.IsFalse(TestMessage.Deserialized);
                Assert.IsFalse(TestMessage.Handled);
            }
        }
    }
}
