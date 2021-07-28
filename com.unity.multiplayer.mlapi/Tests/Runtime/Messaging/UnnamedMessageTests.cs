using System;
using System.Collections;
using System.IO;
using System.Text;
using MLAPI.Serialization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Messaging
{
    public class UnnamedMessageTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;
        
        private NetworkManager Client => m_ClientNetworkManagers[0];

        [UnityTest]
        public IEnumerator UnnamedMessageIsReceivedOnClientWithContent()
        {
            var messageContent = Guid.NewGuid().ToString();
            var buffer = new NetworkBuffer(Encoding.UTF8.GetBytes(messageContent));

            m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessage(Client.LocalClientId, buffer);

            ulong receivedMessageSender = 0;
            string receivedMessageContent = null;
            m_ClientNetworkManagers[0].CustomMessagingManager.OnUnnamedMessage += (sender, stream) =>
            {
                receivedMessageSender = sender;

                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                receivedMessageContent = Encoding.UTF8.GetString(memoryStream.ToArray());
            };

            yield return new WaitForSeconds(0.2f);

            Assert.NotNull(receivedMessageContent);
            Assert.AreEqual(messageContent, receivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMessageSender);
        }
    }
}