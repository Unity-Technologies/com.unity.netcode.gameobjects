using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class MessageVersionDataTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private NetworkManager m_SessionOwner;

        public MessageVersionDataTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        private void ValidateConnectionRequest(NetworkManager networkManager)
        {
            var message = networkManager.ConnectionManager.GenerateConnectionRequestMessage();

            foreach (var messageVersionData in message.MessageVersions)
            {
                Assert.True(messageVersionData.SendMessageType == m_DistributedAuthority, $"Include {nameof(MessageVersionData.SendMessageType)} is {messageVersionData.SendMessageType} and distributed authority is {m_DistributedAuthority}!");
                if (m_DistributedAuthority)
                {
                    var type = networkManager.ConnectionManager.MessageManager.GetMessageForHash(messageVersionData.Hash);
                    var networkMessageType = ILPPMessageProvider.TypeToNetworkMessageType[type];
                    Assert.True(messageVersionData.NetworkMessageType == (uint)networkMessageType, $"{nameof(MessageVersionData.NetworkMessageType)} is {messageVersionData.NetworkMessageType} but the hash type derived value is {networkMessageType}!");
                }
            }
        }

        [UnityTest]
        public IEnumerator MessageVersionDataTest()
        {
            m_SessionOwner = UseCMBService() ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;

            ValidateConnectionRequest(m_SessionOwner);

            foreach (var client in m_ClientNetworkManagers)
            {
                ValidateConnectionRequest(client);
            }

            yield return null;
        }
    }
}
