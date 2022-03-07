using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class PlayerObjectTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        protected GameObject m_NewPlayerToSpawn;

        public PlayerObjectTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override void OnServerAndClientsCreated()
        {
            m_NewPlayerToSpawn = CreateNetworkObjectPrefab("NewPlayerInstance");
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator SpawnAndReplaceExistingPlayerObject()
        {
            // Get the server-side player NetworkObject
            var originalPlayer = m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][m_ClientNetworkManagers[0].LocalClientId];
            // Get the client-side player NetworkObject
            var playerLocalClient = m_ClientNetworkManagers[0].LocalClient.PlayerObject;

            // Create a new player prefab instance
            var newPlayer = Object.Instantiate(m_NewPlayerToSpawn);
            var newPlayerNetworkObject = newPlayer.GetComponent<NetworkObject>();
            newPlayerNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            // Spawn this instance as a new player object for the client who already has an assigned player object
            newPlayerNetworkObject.SpawnAsPlayerObject(m_ClientNetworkManagers[0].LocalClientId);

            // Make sure server-side changes are detected
            yield return WaitForConditionOrTimeOut(() => !originalPlayer.IsPlayerObject && newPlayerNetworkObject.IsPlayerObject);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for server-side player object to change!");

            // Make sure client-side changes are detected
            yield return WaitForConditionOrTimeOut(() => m_ClientNetworkManagers[0].LocalClient.PlayerObject != playerLocalClient && !playerLocalClient.IsPlayerObject
            && m_ClientNetworkManagers[0].LocalClient.PlayerObject.IsPlayerObject);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for client0side player object to change!");
        }
    }
}
