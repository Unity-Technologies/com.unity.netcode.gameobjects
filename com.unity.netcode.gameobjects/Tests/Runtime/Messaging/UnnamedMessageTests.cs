using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class UnnamedMessageTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        private NetworkManager FirstClient => m_ClientNetworkManagers[0];
        private NetworkManager SecondClient => m_ClientNetworkManagers[1];

        [UnityTest]
        public IEnumerator UnnamedMessageIsReceivedOnClientWithContent()
        {
            var messageContent = Guid.NewGuid().ToString();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessage(
                    FirstClient.LocalClientId,
                    ref writer);
            }

            ulong receivedMessageSender = 0;
            string receivedMessageContent = null;
            FirstClient.CustomMessagingManager.OnUnnamedMessage += 
                (ulong sender, ref FastBufferReader reader) =>
                {
                    receivedMessageSender = sender;

                    reader.ReadValueSafe(out receivedMessageContent);
                };

            yield return new WaitForSeconds(0.2f);

            Assert.NotNull(receivedMessageContent);
            Assert.AreEqual(messageContent, receivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMessageSender);
        }

        [UnityTest]
        public IEnumerator UnnamedMessageIsReceivedOnMultipleClientsWithContent()
        {
            var messageContent = Guid.NewGuid().ToString();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessage(
                    new List<ulong> { FirstClient.LocalClientId, SecondClient.LocalClientId },
                    ref writer);
            }

            ulong firstReceivedMessageSender = 0;
            string firstReceivedMessageContent = null;
            FirstClient.CustomMessagingManager.OnUnnamedMessage += 
                (ulong sender, ref FastBufferReader reader) =>
                {
                    firstReceivedMessageSender = sender;

                    reader.ReadValueSafe(out firstReceivedMessageContent);
                };

            ulong secondReceivedMessageSender = 0;
            string secondReceivedMessageContent = null;
            SecondClient.CustomMessagingManager.OnUnnamedMessage += 
                (ulong sender, ref FastBufferReader reader) =>
                {
                    secondReceivedMessageSender = sender;

                    reader.ReadValueSafe(out firstReceivedMessageContent);
                };

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
