using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class PlayerObjectTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

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
            yield return WaitForConditionOrTimeOut(() => m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId].ContainsKey(m_ClientNetworkManagers[0].LocalClientId));
            AssertOnTimeout("Timed out waiting for client-side player object to spawn!");
            // Get the server-side player NetworkObject
            var originalPlayer = m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][m_ClientNetworkManagers[0].LocalClientId];
            // Get the client-side player NetworkObject
            var playerLocalClient = m_ClientNetworkManagers[0].LocalClient.PlayerObject;

            // Create a new player prefab instance
            var newPlayer = Object.Instantiate(m_NewPlayerToSpawn);
            var newPlayerNetworkObject = newPlayer.GetComponent<NetworkObject>();
            // In distributed authority mode, the client owner spawns its new player
            newPlayerNetworkObject.NetworkManagerOwner = m_DistributedAuthority ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;
            // Spawn this instance as a new player object for the client who already has an assigned player object
            newPlayerNetworkObject.SpawnAsPlayerObject(m_ClientNetworkManagers[0].LocalClientId);

            // Make sure server-side changes are detected, the original player object should no longer be marked as a player
            // and the local new player object should.
            yield return WaitForConditionOrTimeOut(() => !originalPlayer.IsPlayerObject && newPlayerNetworkObject.IsPlayerObject);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for server-side player object to change!");

            // Make sure the client-side changes are the same
            yield return WaitForConditionOrTimeOut(() => m_ClientNetworkManagers[0].LocalClient.PlayerObject != playerLocalClient && !playerLocalClient.IsPlayerObject
            && m_ClientNetworkManagers[0].LocalClient.PlayerObject.IsPlayerObject);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for client-side player object to change!");
        }
    }

    /// <summary>
    /// Validate that when auto-player spawning but SpawnWithObservers is disabled,
    /// the player instantiated is only spawned on the authority side.
    /// </summary>
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class PlayerSpawnNoObserversTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        public PlayerSpawnNoObserversTest(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool ShouldCheckForSpawnedPlayers()
        {
            return false;
        }

        protected override void OnCreatePlayerPrefab()
        {
            var playerNetworkObject = m_PlayerPrefab.GetComponent<NetworkObject>();
            playerNetworkObject.SpawnWithObservers = false;
            base.OnCreatePlayerPrefab();
        }

        [UnityTest]
        public IEnumerator SpawnWithNoObservers()
        {
            yield return s_DefaultWaitForTick;

            if (!m_DistributedAuthority)
            {
                // Make sure clients did not spawn their player object on any of the clients including the owner.
                foreach (var client in m_ClientNetworkManagers)
                {
                    foreach (var playerObject in m_ServerNetworkManager.SpawnManager.PlayerObjects)
                    {
                        Assert.IsFalse(client.SpawnManager.SpawnedObjects.ContainsKey(playerObject.NetworkObjectId), $"Client-{client.LocalClientId} spawned player object for Client-{playerObject.NetworkObjectId}!");
                    }
                }
            }
            else
            {
                // For distributed authority, we want to make sure the player object is only spawned on the authority side and all non-authority instances did not spawn it.
                var playerObjectId = m_ServerNetworkManager.LocalClient.PlayerObject.NetworkObjectId;
                foreach (var client in m_ClientNetworkManagers)
                {
                    Assert.IsFalse(client.SpawnManager.SpawnedObjects.ContainsKey(playerObjectId), $"Client-{client.LocalClientId} spawned player object for Client-{m_ServerNetworkManager.LocalClientId}!");
                }

                foreach (var clientPlayer in m_ClientNetworkManagers)
                {
                    playerObjectId = clientPlayer.LocalClient.PlayerObject.NetworkObjectId;
                    Assert.IsFalse(m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(playerObjectId), $"Client-{m_ServerNetworkManager.LocalClientId} spawned player object for Client-{clientPlayer.LocalClientId}!");
                    foreach (var client in m_ClientNetworkManagers)
                    {
                        if (clientPlayer == client)
                        {
                            continue;
                        }
                        Assert.IsFalse(client.SpawnManager.SpawnedObjects.ContainsKey(playerObjectId), $"Client-{client.LocalClientId} spawned player object for Client-{clientPlayer.LocalClientId}!");
                    }
                }

            }
        }
    }
}
