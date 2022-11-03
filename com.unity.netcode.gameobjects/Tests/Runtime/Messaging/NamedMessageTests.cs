using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NamedMessageTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private NetworkManager FirstClient => m_ClientNetworkManagers[0];
        private NetworkManager SecondClient => m_ClientNetworkManagers[1];

        protected override NetworkManagerInstatiationMode OnSetIntegrationTestMode()
        {
            // Don't spin up and shutdown NetworkManager instances for each test
            // within this set of integration tests.
            return NetworkManagerInstatiationMode.AllTests;
        }

        [UnityTest]
        public IEnumerator NamedMessageIsReceivedOnClientWithContent()
        {
            var messageName = Guid.NewGuid().ToString();

            ulong receivedMessageSender = 0;
            var receivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    receivedMessageSender = sender;

                    reader.ReadValueSafe(out receivedMessageContent);
                });

            var messageContent = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage(
                    messageName,
                    FirstClient.LocalClientId,
                    writer);
            }

            yield return WaitForMessageReceived<NamedMessage>(new List<NetworkManager> { FirstClient });

            Assert.AreEqual(messageContent.Value, receivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMessageSender);
        }

        [Test]
        public void NamedMessageIsReceivedOnHostWithContent()
        {
            var messageName = Guid.NewGuid().ToString();

            ulong receivedMessageSender = 0;
            var receivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            m_ServerNetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    receivedMessageSender = sender;

                    reader.ReadValueSafe(out receivedMessageContent);
                });

            var messageContent = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage(
                    messageName,
                    m_ServerNetworkManager.LocalClientId,
                    writer);
            }

            Assert.AreEqual(messageContent.Value, receivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMessageSender);
        }

        [UnityTest]
        public IEnumerator NamedMessageIsReceivedOnMultipleClientsWithContent()
        {
            var messageName = Guid.NewGuid().ToString();

            ulong firstReceivedMessageSender = 0;
            var firstReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    firstReceivedMessageSender = sender;

                    reader.ReadValueSafe(out firstReceivedMessageContent);
                });

            ulong secondReceivedMessageSender = 0;
            var secondReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            SecondClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    secondReceivedMessageSender = sender;

                    reader.ReadValueSafe(out secondReceivedMessageContent);
                });

            ulong thirdReceivedMessageSender = 0;
            var thirdReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            m_ServerNetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    thirdReceivedMessageSender = sender;

                    reader.ReadValueSafe(out thirdReceivedMessageContent);
                });

            var messageContent = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage(
                    messageName,
                    new List<ulong> { m_ServerNetworkManager.LocalClientId, FirstClient.LocalClientId, SecondClient.LocalClientId },
                    writer);
            }

            yield return WaitForMessageReceived<NamedMessage>(new List<NetworkManager> { FirstClient, SecondClient });

            Assert.AreEqual(messageContent.Value, firstReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, firstReceivedMessageSender);

            Assert.AreEqual(messageContent.Value, secondReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, secondReceivedMessageSender);

            Assert.AreEqual(messageContent.Value, thirdReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, thirdReceivedMessageSender);
        }

        [UnityTest]
        public IEnumerator WhenSendingNamedMessageToAll_AllClientsReceiveIt()
        {
            var messageName = Guid.NewGuid().ToString();

            ulong firstReceivedMessageSender = 0;
            var firstReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    firstReceivedMessageSender = sender;

                    reader.ReadValueSafe(out firstReceivedMessageContent);
                });

            ulong secondReceivedMessageSender = 0;
            var secondReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            SecondClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    secondReceivedMessageSender = sender;

                    reader.ReadValueSafe(out secondReceivedMessageContent);
                });

            ulong thirdReceivedMessageSender = 0;
            var thirdReceivedMessageContent = new ForceNetworkSerializeByMemcpy<Guid>(new Guid());
            m_ServerNetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (ulong sender, FastBufferReader reader) =>
                {
                    thirdReceivedMessageSender = sender;

                    reader.ReadValueSafe(out thirdReceivedMessageContent);
                });

            var messageContent = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendNamedMessageToAll(messageName, writer);
            }

            yield return WaitForMessageReceived<NamedMessage>(new List<NetworkManager> { FirstClient, SecondClient });

            Assert.AreEqual(messageContent.Value, firstReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, firstReceivedMessageSender);

            Assert.AreEqual(messageContent.Value, secondReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, secondReceivedMessageSender);

            Assert.AreEqual(messageContent.Value, thirdReceivedMessageContent.Value);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, thirdReceivedMessageSender);
        }

        [Test]
        public void WhenSendingNamedMessageToNullClientList_ArgumentNullExceptionIsThrown()
        {
            var messageName = Guid.NewGuid().ToString();
            var messageContent = new ForceNetworkSerializeByMemcpy<Guid>(Guid.NewGuid());
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
