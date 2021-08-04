using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
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
            var messageContent = Guid.NewGuid().ToString();
            using var messageStream = new MemoryStream(Encoding.UTF8.GetBytes(messageContent));

            m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage(
                messageName,
                FirstClient.LocalClientId,
                messageStream);

            ulong receivedMessageSender = 0;
            string receivedMessageContent = null;
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (sender, stream) =>
                {
                    receivedMessageSender = sender;

                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    receivedMessageContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                });

            yield return new WaitForSeconds(0.2f);

            Assert.NotNull(receivedMessageContent);
            Assert.AreEqual(messageContent, receivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMessageSender);
        }

        [UnityTest]
        public IEnumerator NamedMessageIsReceivedOnMultipleClientsWithContent()
        {
            var messageName = Guid.NewGuid().ToString();
            var messageContent = Guid.NewGuid().ToString();
            using var messageStream = new MemoryStream(Encoding.UTF8.GetBytes(messageContent));

            m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage(
                messageName,
                new List<ulong> { FirstClient.LocalClientId, SecondClient.LocalClientId },
                messageStream);

            ulong firstReceivedMessageSender = 0;
            string firstReceivedMessageContent = null;
            FirstClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (sender, stream) =>
                {
                    firstReceivedMessageSender = sender;

                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    firstReceivedMessageContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                });

            ulong secondReceivedMessageSender = 0;
            string secondReceivedMessageContent = null;
            SecondClient.CustomMessagingManager.RegisterNamedMessageHandler(
                messageName,
                (sender, stream) =>
                {
                    secondReceivedMessageSender = sender;

                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    secondReceivedMessageContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                });

            yield return new WaitForSeconds(0.2f);

            Assert.NotNull(firstReceivedMessageContent);
            Assert.AreEqual(messageContent, firstReceivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, firstReceivedMessageSender);

            Assert.NotNull(secondReceivedMessageContent);
            Assert.AreEqual(messageContent, secondReceivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, secondReceivedMessageSender);
        }
    }
}
