using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.Host)]
    internal class TransportTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        protected override bool m_UseMockTransport => true;

        public TransportTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        [UnityTest]
        public IEnumerator MultipleDisconnectEventsNoop()
        {
            var clientToDisconnect = GetNonAuthorityNetworkManager(0);
            var clientTransport = clientToDisconnect.NetworkConfig.NetworkTransport;

            var otherClient = GetNonAuthorityNetworkManager(1);

            // Send multiple disconnect events
            clientTransport.DisconnectLocalClient();
            clientTransport.DisconnectLocalClient();

            // completely stop and clean up the client
            yield return StopOneClient(clientToDisconnect);

            var expectedConnectedClients = m_UseHost ? NumberOfClients : NumberOfClients - 1;
            yield return WaitForConditionOrTimeOut(() => otherClient.ConnectedClientsIds.Count == expectedConnectedClients);
            AssertOnTimeout($"Incorrect number of connected clients. Expected: {expectedConnectedClients}, have: {otherClient.ConnectedClientsIds.Count}");

            // Start a new client to ensure everything is still working
            yield return CreateAndStartNewClient();

            var newExpectedClients = m_UseHost ? NumberOfClients + 1 : NumberOfClients;
            yield return WaitForConditionOrTimeOut(() => otherClient.ConnectedClientsIds.Count == newExpectedClients);
            AssertOnTimeout($"Incorrect number of connected clients. Expected: {newExpectedClients}, have: {otherClient.ConnectedClientsIds.Count}");


        }
    }
}
