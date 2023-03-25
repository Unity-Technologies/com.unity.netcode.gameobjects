using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class SenderIdTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private NetworkManager FirstClient => m_ClientNetworkManagers[0];
        private NetworkManager SecondClient => m_ClientNetworkManagers[1];

        [UnityTest]
        public IEnumerator WhenSendingMessageFromServerToClient_SenderIdIsCorrect()
        {
            var messageContent = new ForceNetworkSerializeByMemcpy<Guid> { Value = Guid.NewGuid() };
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
                    Assert.AreEqual(sender, NetworkManager.ServerClientId);
                    Assert.AreNotEqual(sender, FirstClient.LocalClientId);
                };

            bool secondReceived = false;
            SecondClient.CustomMessagingManager.OnUnnamedMessage +=
                (sender, reader) =>
                {
                    secondReceived = true;
                    Assert.AreEqual(sender, NetworkManager.ServerClientId);
                    Assert.AreNotEqual(sender, FirstClient.LocalClientId);
                };

            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(firstReceived);
            Assert.IsTrue(secondReceived);
        }
        [UnityTest]
        public IEnumerator WhenSendingMessageFromClientToServer_SenderIdIsCorrect()
        {
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                FirstClient.CustomMessagingManager.SendNamedMessage("FirstClient", NetworkManager.ServerClientId, writer);
                SecondClient.CustomMessagingManager.SendNamedMessage("SecondClient", NetworkManager.ServerClientId, writer);

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

        [UnityTest]
        public IEnumerator WhenClientDisconnectsFromServer_ClientIdIsCorrect()
        {
            var firstClientId = FirstClient.LocalClientId;
            bool received = false;
            void firstCallback(ulong id)
            {
                Assert.AreEqual(firstClientId, id);
                received = true;
            }
            m_ServerNetworkManager.OnClientDisconnectCallback += firstCallback;
            FirstClient.Shutdown();

            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(received);
            var secondClientId = SecondClient.LocalClientId;
            received = false;

            m_ServerNetworkManager.OnClientDisconnectCallback -= firstCallback;
            m_ServerNetworkManager.OnClientDisconnectCallback += id =>
            {
                Assert.AreEqual(secondClientId, id);
                received = true;
            };
            SecondClient.Shutdown();

            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(received);
        }
    }
}
