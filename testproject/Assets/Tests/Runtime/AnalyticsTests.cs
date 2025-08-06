#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Editor;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// In-Editor only test<br />
    /// Validates the analytics event data collection process.<br />
    /// <see cref="ValidateCollectionProcess"/>
    /// </summary>
    internal class AnalyticsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private NetcodeAnalytics m_NetcodeAnalytics;

        protected override IEnumerator OnSetup()
        {
            NetcodeAnalytics.EnableIntegrationTestAnalytics = true;
            m_NetcodeAnalytics = Unity.Netcode.Editor.NetworkManagerHelper.Singleton.NetcodeAnalytics;
            yield return base.OnSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            NetcodeAnalytics.EnableIntegrationTestAnalytics = false;
            yield return base.OnTearDown();
        }

        private bool m_NetworkManagerStarted;
        private bool m_NetworkManagerStopped;
        private IEnumerator StartAndStopSession(int expectedCount, bool isHost, bool isDistributedAuthority)
        {
            m_NetworkManagerStarted = false;
            m_NetworkManagerStopped = false;

            m_ServerNetworkManager.NetworkConfig.NetworkTopology = isDistributedAuthority ? NetworkTopologyTypes.DistributedAuthority : NetworkTopologyTypes.ClientServer;

            if (!isHost)
            {
                m_ServerNetworkManager.StartServer();
            }
            else
            {
                m_ServerNetworkManager.StartHost();
            }
            yield return WaitForConditionOrTimeOut(() => m_NetworkManagerStarted);
            var serverOrHost = isHost ? "Host" : "Server";
            AssertOnTimeout($"Failed to start {nameof(NetworkManager)} as a {serverOrHost} using a {m_ServerNetworkManager.NetworkConfig.NetworkTopology} {nameof(NetworkConfig.NetworkTopology)}!");

            yield return StopNetworkManager();
        }

        private IEnumerator StopNetworkManager()
        {
            var serverOrHost = m_ServerNetworkManager.IsHost ? "Host" : "Server";

            m_ServerNetworkManager.Shutdown();
            yield return WaitForConditionOrTimeOut(() => m_NetworkManagerStopped);
            AssertOnTimeout($"Failed to stop {nameof(NetworkManager)} as a {serverOrHost} using a {m_ServerNetworkManager.NetworkConfig.NetworkTopology} {nameof(NetworkConfig.NetworkTopology)}!");
        }

        private void OnServerStarted()
        {
            m_NetworkManagerStarted = true;
        }

        private void OnServerStopped(bool wasHost)
        {
            m_NetworkManagerStopped = true;
        }

        /// <summary>
        /// Validates the NGO analytics gathering process: <br />
        /// - When entering play mode any previous analytics events should be cleared.<br />
        /// - Each session while in play mode should be tracked (minimal processing).<br />
        /// - When exiting play mode the sessions should be processed and cleared.<br />
        /// - There should only be one unique analytic data event per unique session configuration.<br />
        /// </summary>
        [UnityTest]
        public IEnumerator ValidateCollectionProcess()
        {
            var expectedCount = 0;
            var currentCount = 0;
            m_ServerNetworkManager.OnServerStarted += OnServerStarted;
            m_ServerNetworkManager.OnServerStopped += OnServerStopped;
            m_PlayerPrefab.SetActive(false);
            m_ServerNetworkManager.NetworkConfig.PlayerPrefab = null;
            yield return StopNetworkManager();
            Assert.True(m_NetcodeAnalytics.RecentSessions.Count == 1, $"Expected 1 session but found: {m_NetcodeAnalytics.RecentSessions.Count}!");
            Assert.True(m_NetcodeAnalytics.AnalyticsTestResults.Count == 0, $"Expected 0 analytics events but found: {m_NetcodeAnalytics.AnalyticsTestResults.Count}!");

            // Simulate exiting play mode
            m_NetcodeAnalytics.ModeChanged(UnityEditor.PlayModeStateChange.ExitingPlayMode, m_ServerNetworkManager);
            Assert.True(m_NetcodeAnalytics.AnalyticsTestResults.Count == 1, $"Expected 1 analytics event but found: {m_NetcodeAnalytics.AnalyticsTestResults.Count}!");

            // Simulate entering play mode
            m_NetcodeAnalytics.ModeChanged(UnityEditor.PlayModeStateChange.EnteredPlayMode, m_ServerNetworkManager);

            Assert.IsFalse(m_ServerNetworkManager.IsListening, $"Networkmanager should be stopped but is still listening!");
            // Client-Server
            // Start as a Server and then shutdown
            yield return StartAndStopSession(expectedCount, false, false);
            expectedCount++;
            Assert.True(m_NetcodeAnalytics.RecentSessions.Count == expectedCount, $"Expected {expectedCount} session but found: {m_NetcodeAnalytics.RecentSessions.Count}!");

            // Start as a server again with the same settings and then shutdown
            // (this should be excluded in the final analytics event data)
            yield return StartAndStopSession(expectedCount, false, false);
            expectedCount++;
            Assert.True(m_NetcodeAnalytics.RecentSessions.Count == expectedCount, $"Expected {expectedCount} session but found: {m_NetcodeAnalytics.RecentSessions.Count}!");

            // Start as a Host and then shutdown
            yield return StartAndStopSession(expectedCount, true, false);
            expectedCount++;
            Assert.True(m_NetcodeAnalytics.RecentSessions.Count == expectedCount, $"Expected {expectedCount} session but found: {m_NetcodeAnalytics.RecentSessions.Count}!");

            // Start as a DAHost and then shutdown
            yield return StartAndStopSession(expectedCount, true, true);
            expectedCount++;
            Assert.True(m_NetcodeAnalytics.RecentSessions.Count == expectedCount, $"Expected {expectedCount} session but found: {m_NetcodeAnalytics.RecentSessions.Count}!");

            // Start as a Host again and then shutdown
            // (this should be excluded in the final analytics event data)
            yield return StartAndStopSession(expectedCount, true, false);
            expectedCount++;
            Assert.True(m_NetcodeAnalytics.RecentSessions.Count == expectedCount, $"Expected {expectedCount} session but found: {m_NetcodeAnalytics.RecentSessions.Count}!");
            Assert.True(m_NetcodeAnalytics.AnalyticsTestResults.Count == 0, $"Expected 0 analytics events but found: {m_NetcodeAnalytics.AnalyticsTestResults.Count}!");

            //////////////////////////////////////////////////
            // Validating that each session's configuration is tracked for generating the analytics data events.
            // Verify session 1
            var session = m_NetcodeAnalytics.RecentSessions[currentCount];
            Assert.True(session.WasServer && !session.WasClient, $"Expected session to be started as a server session but it was not! Server: {session.WasServer} Client: {session.WasClient}");
            Assert.True(session.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer, $"Expected session to be a {NetworkTopologyTypes.ClientServer} session but it was {session.NetworkConfig.NetworkTopology}!");

            // Verify session 2
            currentCount++;
            session = m_NetcodeAnalytics.RecentSessions[currentCount];
            Assert.True(session.WasServer && !session.WasClient, $"Expected session to be started as a server session but it was not! Server: {session.WasServer} Client: {session.WasClient}");
            Assert.True(session.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer, $"Expected session to be a {NetworkTopologyTypes.ClientServer} session but it was {session.NetworkConfig.NetworkTopology}!");

            // Verify session 3
            currentCount++;
            session = m_NetcodeAnalytics.RecentSessions[currentCount];
            Assert.True(session.WasServer && session.WasClient, $"Expected session to be started as a host session but it was not! Server: {session.WasServer}Client: {session.WasClient}");
            Assert.True(session.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer, $"Expected session to be a {NetworkTopologyTypes.ClientServer} session but it was {session.NetworkConfig.NetworkTopology}!");

            // Verify session 4
            currentCount++;
            session = m_NetcodeAnalytics.RecentSessions[currentCount];
            Assert.True(session.WasServer && session.WasClient, $"Expected session to be started as a host session but it was not! Server: {session.WasServer}Client: {session.WasClient}");
            Assert.True(session.NetworkConfig.NetworkTopology == NetworkTopologyTypes.DistributedAuthority, $"Expected session to be a {NetworkTopologyTypes.DistributedAuthority} session but it was {session.NetworkConfig.NetworkTopology}!");

            // Verify session 5
            currentCount++;
            session = m_NetcodeAnalytics.RecentSessions[currentCount];
            Assert.True(session.WasServer == true && session.WasClient, $"Expected session to be started as a host session but it was not! Server: {session.WasServer}Client: {session.WasClient}");
            Assert.True(session.NetworkConfig.NetworkTopology == NetworkTopologyTypes.ClientServer, $"Expected session to be a {NetworkTopologyTypes.ClientServer} session but it was {session.NetworkConfig.NetworkTopology}!");

            //////////////////////////////////////////////////
            // Validating that the analytics event data that would be sent should only contain information on the 3 unique session configurations
            // Client-Server as a Server
            // Client-Server as a Host
            // Distributed Authority as a DAHost
            m_NetcodeAnalytics.ModeChanged(UnityEditor.PlayModeStateChange.ExitingPlayMode, m_ServerNetworkManager);

            // Verify that we cleared out the NetcodeAnalytics.RecentSessions when we exited play mode
            Assert.True(m_NetcodeAnalytics.RecentSessions.Count == 0, $"After exiting play mode we expected 0 RecentSessions but had {m_NetcodeAnalytics.RecentSessions.Count}");

            // Adjust our expected count to be 2 less than the RecentSessions since we intentionally included two identical sessions.
            // (There should only be unique session configurations included in the final analytics event data entries)
            expectedCount -= 2;

            Assert.True(m_NetcodeAnalytics.AnalyticsTestResults.Count == expectedCount, $"Expected {expectedCount} analytics event but found: {m_NetcodeAnalytics.AnalyticsTestResults.Count}!");
            currentCount = 0;
            // Verify event data 1
            var eventData = m_NetcodeAnalytics.AnalyticsTestResults[currentCount].Data;
            Assert.True(eventData.WasServer && !eventData.WasClient, $"Expected session to be started as a server session but it was not! WasServer: {eventData.WasServer} WasClient: {eventData.WasClient}");
            Assert.True(!eventData.IsDistributedAuthority, $"Expected IsDistributedAuthority to be false but it was true!");

            // Verify event data 2
            currentCount++;
            eventData = m_NetcodeAnalytics.AnalyticsTestResults[currentCount].Data;
            Assert.True(eventData.WasServer && eventData.WasClient, $"Expected session to be started as a Host session but it was not! WasServer: {eventData.WasServer} WasClient: {eventData.WasClient}");
            Assert.True(!eventData.IsDistributedAuthority, $"Expected IsDistributedAuthority to be false but it was true!");

            // Verify event data 3
            currentCount++;
            eventData = m_NetcodeAnalytics.AnalyticsTestResults[currentCount].Data;
            Assert.True(eventData.WasServer && eventData.WasClient, $"Expected session to be started as a host session but it was not! WasServer: {eventData.WasServer} WasClient: {eventData.WasClient}");
            Assert.True(eventData.IsDistributedAuthority, $"Expected IsDistributedAuthority to be true but it was false!");
            yield return null;
        }
    }
}
#endif
