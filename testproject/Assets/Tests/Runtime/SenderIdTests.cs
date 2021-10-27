using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class SenderIdTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        private NetworkManager FirstClient => m_ClientNetworkManagers[0];
        private NetworkManager SecondClient => m_ClientNetworkManagers[1];

        [UnityTest]
        public IEnumerator WhenSendingMessageFromServerToClient_SenderIdIsCorrect()
        {
            var messageContent = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessageToAll(writer);
            }

            bool firstReceived = false;
            FirstClient.CustomMessagingManager.OnUnnamedMessage +=
                (sender, reader) =>
                {
                    firstReceived = true;
                    Assert.AreEqual(sender, FirstClient.ServerClientId);
                    Assert.AreNotEqual(sender, FirstClient.LocalClientId);
                };

            bool secondReceived = false;
            SecondClient.CustomMessagingManager.OnUnnamedMessage +=
                (sender, reader) =>
                {
                    secondReceived = true;
                    Assert.AreEqual(sender, FirstClient.ServerClientId);
                    Assert.AreNotEqual(sender, FirstClient.LocalClientId);
                };

            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(firstReceived);
            Assert.IsTrue(secondReceived);
        }
        [UnityTest]
        public IEnumerator WhenSendingMessageFromClientToServer_SenderIdIsCorrect()
        {
            var messageContent = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                FirstClient.CustomMessagingManager.SendNamedMessage("FirstClient", FirstClient.ServerClientId, writer);
                SecondClient.CustomMessagingManager.SendNamedMessage("SecondClient", SecondClient.ServerClientId, writer);

            }

            bool firstReceived = false;
            m_ServerNetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                "FirstClient",
                (sender, reader) =>
                {
                    firstReceived = true;
                    Assert.AreEqual(sender, FirstClient.LocalClientId);
                    Assert.AreNotEqual(sender, SecondClient.LocalClientId);
                    Assert.AreNotEqual(sender, m_ServerNetworkManager.LocalClientId);
                });

            bool secondReceived = false;
            m_ServerNetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(
                "SecondClient",
                (sender, reader) =>
                {
                    secondReceived = true;
                    Assert.AreNotEqual(sender, FirstClient.LocalClientId);
                    Assert.AreEqual(sender, SecondClient.LocalClientId);
                    Assert.AreNotEqual(sender, m_ServerNetworkManager.LocalClientId);
                });

            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(firstReceived);
            Assert.IsTrue(secondReceived);
        }
    }
}
