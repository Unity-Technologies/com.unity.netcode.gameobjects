using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NamedMessageTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        private NetworkManager FirstClient => m_ClientNetworkManagers[0];
        private NetworkManager SecondClient => m_ClientNetworkManagers[1];

        [UnityTest]
        public IEnumerator NamedMessageIsReceivedOnClientWithContent()
        {
            var messageName = Guid.NewGuid().ToString();
            var messageContent = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage(
                    messageName,
                    FirstClient.LocalClientId,
                    writer);
            }

            ulong receivedMessageSender = 0;
            var receivedMessageContent = new Guid();
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    receivedMessageSender = sender;

                    reader.ReadValueSafe(out receivedMessageContent);
                });

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(messageContent, receivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMessageSender);
        }

        [UnityTest]
        public IEnumerator NamedMessageIsReceivedOnMultipleClientsWithContent()
        {
            var messageName = Guid.NewGuid().ToString();
            var messageContent = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage(
                    messageName,
                    new List<ulong> { FirstClient.LocalClientId, SecondClient.LocalClientId },
                    writer);
            }

            ulong firstReceivedMessageSender = 0;
            var firstReceivedMessageContent = new Guid();
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    firstReceivedMessageSender = sender;

                    reader.ReadValueSafe(out firstReceivedMessageContent);
                });

            ulong secondReceivedMessageSender = 0;
            var secondReceivedMessageContent = new Guid();
            SecondClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    secondReceivedMessageSender = sender;

                    reader.ReadValueSafe(out secondReceivedMessageContent);
                });

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(messageContent, firstReceivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, firstReceivedMessageSender);

            Assert.AreEqual(messageContent, secondReceivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, secondReceivedMessageSender);
        }

        [UnityTest]
        public IEnumerator WhenSendingNamedMessageToAll_AllClientsReceiveIt()
        {
            var messageName = Guid.NewGuid().ToString();
            var messageContent = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendNamedMessageToAll(messageName, writer);
            }

            ulong firstReceivedMessageSender = 0;
            var firstReceivedMessageContent = new Guid();
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    firstReceivedMessageSender = sender;

                    reader.ReadValueSafe(out firstReceivedMessageContent);
                });

            ulong secondReceivedMessageSender = 0;
            var secondReceivedMessageContent = new Guid();
            SecondClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    secondReceivedMessageSender = sender;

                    reader.ReadValueSafe(out secondReceivedMessageContent);
                });

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(messageContent, firstReceivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, firstReceivedMessageSender);

            Assert.AreEqual(messageContent, secondReceivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, secondReceivedMessageSender);
        }

        [Test]
        public void WhenSendingNamedMessageToNullClientList_ArgumentNullExceptionIsThrown()
        {
            var messageName = Guid.NewGuid().ToString();
            var messageContent = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                Assert.Throws<ArgumentNullException>(
                    () =>
                    {
                        m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage(messageName, null, writer);
                    });
            }
        }
    }
}
