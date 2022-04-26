using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class UnnamedMessageTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private NetworkManager FirstClient => m_ClientNetworkManagers[0];
        private NetworkManager SecondClient => m_ClientNetworkManagers[1];

        [UnityTest]
        public IEnumerator UnnamedMessageIsReceivedOnClientWithContent()
        {
            var messageContent = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessage(
                    FirstClient.LocalClientId,
                    writer);
            }

            ulong receivedMessageSender = 0;
            var receivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            FirstClient.CustomMessagingManager.OnUnnamedMessage +=
                (ulong sender, FastBufferReader reader) =>
                {
                    receivedMessageSender = sender;

                    reader.ReadValueSafe(out receivedMessageContent);
                };

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(messageContent.Value, receivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMessageSender);
        }

        [UnityTest]
        public IEnumerator UnnamedMessageIsReceivedOnMultipleClientsWithContent()
        {
            var messageContent = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessage(
                    new List<ulong> { FirstClient.LocalClientId, SecondClient.LocalClientId },
                    writer);
            }

            ulong firstReceivedMessageSender = 0;
            var firstReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            FirstClient.CustomMessagingManager.OnUnnamedMessage +=
                (ulong sender, FastBufferReader reader) =>
                {
                    firstReceivedMessageSender = sender;

                    reader.ReadValueSafe(out firstReceivedMessageContent);
                };

            ulong secondReceivedMessageSender = 0;
            var secondReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            SecondClient.CustomMessagingManager.OnUnnamedMessage +=
                (ulong sender, FastBufferReader reader) =>
                {
                    secondReceivedMessageSender = sender;

                    reader.ReadValueSafe(out secondReceivedMessageContent);
                };

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(messageContent.Value, firstReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, firstReceivedMessageSender);

            Assert.AreEqual(messageContent.Value, secondReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, secondReceivedMessageSender);
        }

        [UnityTest]
        public IEnumerator WhenSendingUnnamedMessageToAll_AllClientsReceiveIt()
        {
            var messageContent = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            ulong firstReceivedMessageSender = 0;
            var firstReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            FirstClient.CustomMessagingManager.OnUnnamedMessage +=
                (ulong sender, FastBufferReader reader) =>
                {
                    firstReceivedMessageSender = sender;

                    reader.ReadValueSafe(out firstReceivedMessageContent);
                };

            ulong secondReceivedMessageSender = 0;
            var secondReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            SecondClient.CustomMessagingManager.OnUnnamedMessage +=
                (ulong sender, FastBufferReader reader) =>
                {
                    secondReceivedMessageSender = sender;

                    reader.ReadValueSafe(out secondReceivedMessageContent);
                };

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(messageContent.Value, firstReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, firstReceivedMessageSender);

            Assert.AreEqual(messageContent.Value, secondReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, secondReceivedMessageSender);
        }

        [Test]
        public void WhenSendingNamedMessageToNullClientList_ArgumentNullExceptionIsThrown()
        {
            var messageContent = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                Assert.Throws<ArgumentNullException>(
                    () =>
                    {
                        m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessage(null, writer);
                    });
            }
        }
    }
}
