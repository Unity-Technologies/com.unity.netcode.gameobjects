using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class StopStartRuntimeTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        protected override void OnOneTimeSetup()
        {
            m_UseHost = false;
            base.OnOneTimeSetup();
        }

        [UnityTest]
        public IEnumerator WhenShuttingDownAndRestarting_SDKRestartsSuccessfullyAndStaysRunning()
        {
            // shutdown the server
            m_ServerNetworkManager.Shutdown();

            // wait 1 frame because shutdowns are delayed
            var nextFrameNumber = Time.frameCount + 1;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

            // Verify the shutdown occurred
            Assert.IsFalse(m_ServerNetworkManager.IsServer);
            Assert.IsFalse(m_ServerNetworkManager.IsListening);
            Assert.IsFalse(m_ServerNetworkManager.IsHost);
            Assert.IsFalse(m_ServerNetworkManager.IsClient);

            m_ServerNetworkManager.StartServer();
            // Verify the server started
            Assert.IsTrue(m_ServerNetworkManager.IsServer);
            Assert.IsTrue(m_ServerNetworkManager.IsListening);

            // Wait several frames / one full network tick
            yield return s_DefaultWaitForTick;

            // Verify the server is still running
            Assert.IsTrue(m_ServerNetworkManager.IsServer);
            Assert.IsTrue(m_ServerNetworkManager.IsListening);
        }

        [UnityTest]
        public IEnumerator WhenShuttingDownTwiceAndRestarting_SDKRestartsSuccessfullyAndStaysRunning()
        {
            // shutdown the server
            m_ServerNetworkManager.Shutdown();

            // wait 1 frame because shutdowns are delayed
            var nextFrameNumber = Time.frameCount + 1;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

            // Verify the shutdown occurred
            Assert.IsFalse(m_ServerNetworkManager.IsServer);
            Assert.IsFalse(m_ServerNetworkManager.IsListening);
            Assert.IsFalse(m_ServerNetworkManager.IsHost);
            Assert.IsFalse(m_ServerNetworkManager.IsClient);

            // Shutdown the server again.
            m_ServerNetworkManager.Shutdown();

            m_ServerNetworkManager.StartServer();
            // Verify the server started
            Assert.IsTrue(m_ServerNetworkManager.IsServer);
            Assert.IsTrue(m_ServerNetworkManager.IsListening);

            // Wait several frames / one full network tick
            yield return s_DefaultWaitForTick;

            // Verify the server is still running
            Assert.IsTrue(m_ServerNetworkManager.IsServer);
            Assert.IsTrue(m_ServerNetworkManager.IsListening);
        }
    }
}
