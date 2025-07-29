using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(ApprovalTimedOutTypes.ServerDoesNotRespond)]
    [TestFixture(ApprovalTimedOutTypes.ClientDoesNotRequest)]
    public class ConnectionApprovalTimeoutTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        public enum ApprovalTimedOutTypes
        {
            ClientDoesNotRequest,
            ServerDoesNotRespond
        }

        private ApprovalTimedOutTypes m_ApprovalFailureType;

        public ConnectionApprovalTimeoutTests(ApprovalTimedOutTypes approvalFailureType)
        {
            m_ApprovalFailureType = approvalFailureType;
        }

        // Must be >= 2 since this is an int value and the test waits for timeout - 1 to try to verify it doesn't
        // time out early
        private const int k_TestTimeoutPeriod = 1;

        private Regex m_ExpectedLogMessage;
        private LogType m_LogType;


        protected override IEnumerator OnSetup()
        {
            m_BypassConnectionTimeout = true;
            return base.OnSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            m_BypassConnectionTimeout = false;
            return base.OnTearDown();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ServerNetworkManager.NetworkConfig.ClientConnectionBufferTimeout = k_TestTimeoutPeriod;
            m_ServerNetworkManager.LogLevel = LogLevel.Developer;
            m_ClientNetworkManagers[0].NetworkConfig.ClientConnectionBufferTimeout = k_TestTimeoutPeriod;
            m_ClientNetworkManagers[0].LogLevel = LogLevel.Developer;
            base.OnServerAndClientsCreated();
        }

        private MessageCatcher<ConnectionRequestMessage> m_ConnectionRequestCatcher;
        private MessageCatcher<ConnectionApprovedMessage> m_ConnectionApprovedCatcher;
        protected override IEnumerator OnStartedServerAndClients()
        {
            m_ClientStopped = false;
            m_ServerStopped = false;
            m_ClientNetworkManagers[0].OnClientStopped += OnClientStopped;
            m_ServerNetworkManager.OnServerStopped += OnServerStopped;
            if (m_ApprovalFailureType == ApprovalTimedOutTypes.ServerDoesNotRespond)
            {
                // We catch (don't process) the incoming approval message to simulate the server not sending the approved message in time
                m_ConnectionApprovedCatcher = new MessageCatcher<ConnectionApprovedMessage>(m_ClientNetworkManagers[0], m_EnableVerboseDebug);
                m_ClientNetworkManagers[0].ConnectionManager.MessageManager.Hook(m_ConnectionApprovedCatcher);
                m_ExpectedLogMessage = new Regex("Timed out waiting for the server to approve the connection request.");
                m_LogType = LogType.Log;
            }
            else
            {
                // We catch (don't process) the incoming connection request message to simulate a transport connection but the client never
                // sends (or takes too long to send) the connection request.
                m_ConnectionRequestCatcher = new MessageCatcher<ConnectionRequestMessage>(m_ServerNetworkManager, m_EnableVerboseDebug);
                m_ServerNetworkManager.ConnectionManager.MessageManager.Hook(m_ConnectionRequestCatcher);

                // For this test, we know the timed out client will be Client-1
                m_ExpectedLogMessage = new Regex("Server detected a transport connection from Client-1, but timed out waiting for the connection request message.");
                m_LogType = LogType.Warning;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ValidateApprovalTimeout()
        {
            // Delay for half of the wait period
            yield return new WaitForSeconds(k_TestTimeoutPeriod * 0.25f);

            // Verify we haven't received the time out message yet
            NetcodeLogAssert.LogWasNotReceived(LogType.Log, m_ExpectedLogMessage);

            yield return new WaitForSeconds(k_TestTimeoutPeriod * 1.5f);

            // We should have the test relative log message by this time.
            NetcodeLogAssert.LogWasReceived(m_LogType, m_ExpectedLogMessage);

            Debug.Log("Checking connected client count");
            // It should only have the host client connected
            Assert.AreEqual(1, m_ServerNetworkManager.ConnectedClients.Count, $"Expected only one client when there were {m_ServerNetworkManager.ConnectedClients.Count} clients connected!");


            Assert.AreEqual(0, m_ServerNetworkManager.ConnectionManager.PendingClients.Count, $"Expected no pending clients when there were {m_ServerNetworkManager.ConnectionManager.PendingClients.Count} pending clients!");
            Assert.True(!m_ClientNetworkManagers[0].LocalClient.IsApproved, $"Expected the client to not have been approved, but it was!");

            if (m_ApprovalFailureType == ApprovalTimedOutTypes.ServerDoesNotRespond)
            {
                m_ConnectionApprovedCatcher.ClearMessages();
            }
            else
            {
                m_ConnectionRequestCatcher.ClearMessages();
            }

            if (!m_ClientStopped)
            {
                m_ClientNetworkManagers[0].Shutdown();
            }

            if (!m_ServerStopped)
            {
                m_ServerNetworkManager.Shutdown();
            }

            yield return WaitForConditionOrTimeOut(() => m_ClientStopped && m_ServerStopped);
            AssertOnTimeout($"Timed out waiting for the client or server to stop!");
        }

        private bool m_ClientStopped;
        private void OnClientStopped(bool obj)
        {
            m_ClientNetworkManagers[0].OnClientStopped -= OnClientStopped;
            m_ClientStopped = true;
        }

        private bool m_ServerStopped;
        private void OnServerStopped(bool obj)
        {
            m_ServerNetworkManager.OnServerStopped -= OnServerStopped;
            m_ServerStopped = true;
        }

    }
}
