using System;
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(StartType.Server)]
    [TestFixture(StartType.Host)]
    [TestFixture(StartType.Client)]
    internal class NetworkManagerStartExceptionTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        public enum StartType
        {
            Server,
            Host,
            Client
        }

        private StartType m_StartType;
        public NetworkManagerStartExceptionTests(StartType startType) : base(startType == StartType.Server ? HostOrServer.Server : HostOrServer.Host)
        {
            m_StartType = startType;
        }

        private const string k_ExceptionText = "This is a test exception";

        private void ThrowExceptionAction()
        {
            throw new Exception(k_ExceptionText);
        }

        private SessionConfig SessionConfigExceptionThrower()
        {
            throw new Exception(k_ExceptionText);
        }

        [UnityTest]
        public IEnumerator VerifyNetworkManagerHandlesExceptionDuringStart()
        {
            var startType = m_StartType;
            NetworkManager toTest;
            if (startType == StartType.Client)
            {
                toTest = CreateNewClient();
            }
            else
            {
                toTest = GetAuthorityNetworkManager();
                yield return StopOneClient(toTest);
            }

            var transport = toTest.NetworkConfig.NetworkTransport as UnityTransport;
            Assert.That(transport, Is.Not.Null, "Transport should not be null");

            var isListening = true;

            /*
             * Test exception being thrown during NetworkManager.Initialize()
             */

            // It's not possible to throw an exception during server initialization
            if (startType != StartType.Server)
            {
                // OnSessionConfig is called within Initialize only in DAMode
                toTest.NetworkConfig.NetworkTopology = NetworkTopologyTypes.DistributedAuthority;

                toTest.OnGetSessionConfig += SessionConfigExceptionThrower;

                LogAssert.Expect(LogType.Exception, $"Exception: {k_ExceptionText}");
                isListening = startType == StartType.Host ? toTest.StartHost() : toTest.StartClient();

                Assert.That(isListening, Is.False, "Should not have started after exception during Initialize()");
                Assert.That(transport.GetNetworkDriver().IsCreated, Is.False, "NetworkDriver should not be created.");

                toTest.OnGetSessionConfig -= SessionConfigExceptionThrower;
                toTest.NetworkConfig.NetworkTopology = NetworkTopologyTypes.ClientServer;
            }

            /*
             * Test exception being thrown after NetworkTransport.StartClient()
             */

            toTest.OnClientStarted += ThrowExceptionAction;
            toTest.OnServerStarted += ThrowExceptionAction;

            LogAssert.Expect(LogType.Exception, $"Exception: {k_ExceptionText}");
            isListening = startType switch
            {
                StartType.Server => toTest.StartServer(),
                StartType.Host => toTest.StartHost(),
                StartType.Client => toTest.StartClient(),
                _ => true
            };

            Assert.That(isListening, Is.False, "Should not have started after exception during startup");
            Assert.That(transport.GetNetworkDriver().IsCreated, Is.False, "NetworkDriver should not be created.");
            Assert.False(toTest.IsServer, "IsServer should be false when NetworkManager failed to start");
            Assert.False(toTest.IsClient, "IsClient should be false when NetworkManager failed to start");

            toTest.OnClientStarted -= ThrowExceptionAction;
            toTest.OnServerStarted -= ThrowExceptionAction;

            if (startType == StartType.Client)
            {
                // Start the client fully to ensure startup still works with no exceptions
                yield return StartClient(toTest);

                Assert.That(toTest.IsListening, Is.True, "Client failed to start");
                Assert.That(transport.GetNetworkDriver().IsCreated, Is.True, "NetworkDriver should be created.");
            }
            else
            {
                var isHost = startType == StartType.Host;
                NetcodeIntegrationTestHelpers.StartServer(isHost, toTest);
                var hostOrServer = isHost ? "Host" : "Server";
                Assert.That(toTest.IsListening, Is.True, $"{hostOrServer} failed to start");
                Assert.That(transport.GetNetworkDriver().IsCreated, Is.True, "NetworkDriver should be created.");

                yield return CreateAndStartNewClient();
            }
        }

    }
}
