using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(true)]
    [TestFixture(false)]
    public class ConnectionApprovalTimeoutTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        protected override bool CanStartServerAndClients() => false;

        private bool m_UseSceneManagement;
        public ConnectionApprovalTimeoutTests(bool useSceneManagement)
        {
            m_UseSceneManagement = useSceneManagement;
        }

        // Must be >= 2 since this is an int value and the test waits for timeout - 1 to try to verify it doesn't
        // time out early
        private const int k_TestTimeoutPeriod = 2;

        private void Start()
        {
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = m_UseSceneManagement;
            m_ClientNetworkManagers[0].NetworkConfig.EnableSceneManagement = m_UseSceneManagement;
            if (!NetcodeIntegrationTestHelpers.Start(false, m_ServerNetworkManager, m_ClientNetworkManagers))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }
        }

        [UnityTest]
        public IEnumerator WhenClientDoesntRequestApproval_ServerTimesOut()
        {
            Start();
            var hook = new MessageCatcher<ConnectionRequestMessage>(m_ServerNetworkManager);
            m_ServerNetworkManager.MessagingSystem.Hook(hook); ;

            m_ServerNetworkManager.NetworkConfig.ClientConnectionBufferTimeout = k_TestTimeoutPeriod;
            m_ServerNetworkManager.LogLevel = LogLevel.Developer;
            m_ClientNetworkManagers[0].LogLevel = LogLevel.Developer;

            yield return new WaitForSeconds(m_ServerNetworkManager.NetworkConfig.ClientConnectionBufferTimeout - 1);

            Assert.AreEqual(0, m_ServerNetworkManager.ConnectedClients.Count);
            Assert.AreEqual(1, m_ServerNetworkManager.PendingClients.Count);

            var expectedLogMessage = new Regex($"Client {m_ServerNetworkManager.PendingClients.FirstOrDefault().Key} Handshake Timed Out");

            NetcodeLogAssert.LogWasNotReceived(LogType.Log, expectedLogMessage);

            yield return new WaitForSeconds(2);

            NetcodeLogAssert.LogWasReceived(LogType.Log, expectedLogMessage);

            Assert.AreEqual(0, m_ServerNetworkManager.ConnectedClients.Count);
            Assert.AreEqual(0, m_ServerNetworkManager.PendingClients.Count);
        }

        [UnityTest]
        public IEnumerator WhenServerDoesntRespondWithApproval_ClientTimesOut()
        {
            Start();

            if (m_UseSceneManagement)
            {
                var sceneEventHook = new MessageCatcher<SceneEventMessage>(m_ClientNetworkManagers[0]);
                m_ClientNetworkManagers[0].MessagingSystem.Hook(sceneEventHook);
            }
            else
            {
                var approvalHook = new MessageCatcher<ConnectionApprovedMessage>(m_ClientNetworkManagers[0]);
                m_ClientNetworkManagers[0].MessagingSystem.Hook(approvalHook);
            }

            m_ClientNetworkManagers[0].NetworkConfig.ClientConnectionBufferTimeout = k_TestTimeoutPeriod;
            m_ServerNetworkManager.LogLevel = LogLevel.Developer;
            m_ClientNetworkManagers[0].LogLevel = LogLevel.Developer;

            yield return new WaitForSeconds(m_ClientNetworkManagers[0].NetworkConfig.ClientConnectionBufferTimeout - 1);

            Assert.IsFalse(m_ClientNetworkManagers[0].IsConnectedClient);
            Assert.IsTrue(m_ClientNetworkManagers[0].IsListening);

            var expectedLogMessage = new Regex("Server Handshake Timed Out");
            NetcodeLogAssert.LogWasNotReceived(LogType.Log, expectedLogMessage);

            yield return new WaitForSeconds(2);

            NetcodeLogAssert.LogWasReceived(LogType.Log, expectedLogMessage);

            Assert.IsFalse(m_ClientNetworkManagers[0].IsConnectedClient);
            Assert.IsFalse(m_ClientNetworkManagers[0].IsListening);
        }
    }
}
