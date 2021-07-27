using System;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Messaging
{
    public class NamedMessageTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        private NetworkManager Client => m_ClientNetworkManagers[0];

        [UnityTest]
        public IEnumerator NamedMessageIsReceivedOnClientWithContent()
        {
            var messageName = Guid.NewGuid().ToString();
            var messageContent = Guid.NewGuid().ToString();
            using var messageStream = new MemoryStream(Encoding.UTF8.GetBytes(messageContent));

            m_ServerNetworkManager.CustomMessagingManager.SendNamedMessage(messageName, Client.LocalClientId, messageStream);

            ulong receivedMessageSender = 0;
            string receivedMessageContent = null;
            m_ClientNetworkManagers[0].CustomMessagingManager.RegisterNamedMessageHandler(
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
    }
}