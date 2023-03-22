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
    [TestFixture(ApprovalTimedOutTypes.ClientIsNotApproved)]
    public class ConnectionApprovalTimeoutTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        public enum ApprovalTimedOutTypes
        {
            ClientDoesNotRequest,
            ServerDoesNotRespond,
            ClientIsNotApproved,
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
            m_ServerNetworkManager.LogLevel = LogLevel.Developer;
            m_ServerNetworkManager.NetworkConfig.ConnectionApproval = m_ApprovalFailureType == ApprovalTimedOutTypes.ClientIsNotApproved;
            if (m_ApprovalFailureType == ApprovalTimedOutTypes.ClientIsNotApproved)
            {
                m_ServerNetworkManager.ConnectionApprovalCallback = NetworkManagerObject_ConnectionApprovalCallback;
            }
            else
            {
                m_ServerNetworkManager.NetworkConfig.ClientConnectionBufferTimeout = k_TestTimeoutPeriod;
                m_ClientNetworkManagers[0].NetworkConfig.ClientConnectionBufferTimeout = k_TestTimeoutPeriod;
            }
            m_ClientNetworkManagers[0].NetworkConfig.ConnectionApproval = m_ApprovalFailureType == ApprovalTimedOutTypes.ClientIsNotApproved;
            m_ClientNetworkManagers[0].LogLevel = LogLevel.Developer;
            base.OnServerAndClientsCreated();
        }

        private bool m_ClientWasNotApproved;
        private void NetworkManagerObject_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = request.ClientNetworkId == NetworkManager.ServerClientId;
            m_ClientWasNotApproved = !response.Approved;
            if (!response.Approved)
            {
                Assert.IsTrue(m_ServerNetworkManager.ApprovalTimeouts.Count > 0, $"There are ({m_ServerNetworkManager.ApprovalTimeouts.Count}) approval timeout coroutines when there should at least be 1!");
            }
            response.Reason = "Testing approval failure and TimedOut coroutine.";
            VerboseDebug($"Client {request.ClientNetworkId} was approved {response.Approved}");
        }

        protected override IEnumerator OnStartedServerAndClients()
        {
            if (m_ApprovalFailureType == ApprovalTimedOutTypes.ServerDoesNotRespond)
            {
                // We catch (don't process) the incoming approval message to simulate the server not sending the approved message in time
                m_ClientNetworkManagers[0].MessagingSystem.Hook(new MessageCatcher<ConnectionApprovedMessage>(m_ClientNetworkManagers[0]));
                m_ExpectedLogMessage = new Regex("Timed out waiting for the server to approve the connection request.");
                m_LogType = LogType.Log;
            }
            else if (m_ApprovalFailureType == ApprovalTimedOutTypes.ClientDoesNotRequest)
            {
                // We catch (don't process) the incoming connection request message to simulate a transport connection but the client never
                // sends (or takes too long to send) the connection request.
                m_ServerNetworkManager.MessagingSystem.Hook(new MessageCatcher<ConnectionRequestMessage>(m_ServerNetworkManager));

                // For this test, we know the timed out client will be Client-1
                m_ExpectedLogMessage = new Regex("Server detected a transport connection from Client-1, but timed out waiting for the connection request message.");
                m_LogType = LogType.Warning;
            }
            // Otherwise the not approved test doesn't intercept any messages
            yield return null;
        }

        [UnityTest]
        public IEnumerator ValidateApprovalTimeout()
        {
            if (m_ApprovalFailureType != ApprovalTimedOutTypes.ClientIsNotApproved)
            {
                // Delay for half of the wait period
                yield return new WaitForSeconds(k_TestTimeoutPeriod * 0.5f);

                // Verify we haven't received the time out message yet
                NetcodeLogAssert.LogWasNotReceived(LogType.Log, m_ExpectedLogMessage);

                // Wait for 3/4s of the time out period to pass (totaling 1.25x the wait period)
                yield return new WaitForSeconds(k_TestTimeoutPeriod * 0.75f);

                // We should have the test relative log message by this time.
                NetcodeLogAssert.LogWasReceived(m_LogType, m_ExpectedLogMessage);
            }
            else
            {
                // Make sure a client was not approved
                yield return WaitForConditionOrTimeOut(() => m_ClientWasNotApproved);
                AssertOnTimeout($"Did not detect that the client was not approved while waiting!");

                // Make sure that there are no timed out coroutines left running on the server after a client is not approved
                yield return WaitForConditionOrTimeOut(() => m_ServerNetworkManager.ApprovalTimeouts.Count == 0);
                AssertOnTimeout($"Waited for {nameof(NetworkManager.ApprovalTimeouts)} to reach a count of 0 but is still at {m_ServerNetworkManager.ApprovalTimeouts.Count}!");
            }

            // It should only have the host client connected
            Assert.AreEqual(1, m_ServerNetworkManager.ConnectedClients.Count, $"Expected only one client when there were {m_ServerNetworkManager.ConnectedClients.Count} clients connected!");
            Assert.AreEqual(0, m_ServerNetworkManager.PendingClients.Count, $"Expected no pending clients when there were {m_ServerNetworkManager.PendingClients.Count} pending clients!");
            Assert.True(!m_ClientNetworkManagers[0].IsApproved, $"Expected the client to not have been approved, but it was!");
        }
    }
}
