using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Generic <see cref="NetworkManager"/> test to validate clients can connect
    /// without having set a player prefab.
    /// </summary>
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkManagerPlayerPrefab : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        public NetworkManagerPlayerPrefab(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        /// <summary>
        /// Assure no player prefab is assigned.
        /// </summary>
        protected override void OnServerAndClientsCreated()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                networkManager.NetworkConfig.PlayerPrefab = null;
            }
            base.OnServerAndClientsCreated();
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.PlayerPrefab = null;
            base.OnNewClientCreated(networkManager);
        }

        /// <summary>
        /// Do not wait for spawned players as there are none.
        /// </summary>
        /// <returns></returns>
        protected override bool ShouldCheckForSpawnedPlayers()
        {
            return false;
        }

        /// <summary>
        /// Validates NetworkManager can start as a host and/or clients
        /// can join when there is no player prefab assigned to the
        /// NetworkManager.
        /// </summary>
        [UnityTest]
        public IEnumerator VerifyNetworkManagerHandlesNoPlayerPrefab()
        {
            // If we make it to here, then the 1st client and the authority
            // connected with no exceptions.
            // Now just late join a 2nd client.
            yield return CreateAndStartNewClient();
            // If it makes it to here without an exception then the test passes.
        }
    }
}
