using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();

            yield return WaitForConditionOrTimeOut(() => m_PlayerNetworkObjects[authority.LocalClientId].ContainsKey(nonAuthority.LocalClientId));
            AssertOnTimeout("Timed out waiting for client-side player object to spawn!");
            // Get the server-side player NetworkObject
            var originalPlayer = m_PlayerNetworkObjects[authority.LocalClientId][nonAuthority.LocalClientId];
            // Get the client-side player NetworkObject
            var playerLocalClient = nonAuthority.LocalClient.PlayerObject;

            // Create a new player prefab instance
            var newPlayer = Object.Instantiate(m_NewPlayerToSpawn);
            var newPlayerNetworkObject = newPlayer.GetComponent<NetworkObject>();
            // In distributed authority mode, the client owner spawns its new player
            newPlayerNetworkObject.NetworkManagerOwner = m_DistributedAuthority ? nonAuthority : authority;
            // Spawn this instance as a new player object for the client who already has an assigned player object
            newPlayerNetworkObject.SpawnAsPlayerObject(nonAuthority.LocalClientId);

            // Make sure server-side changes are detected, the original player object should no longer be marked as a player
            // and the local new player object should.
            yield return WaitForConditionOrTimeOut(() => !originalPlayer.IsPlayerObject && newPlayerNetworkObject.IsPlayerObject);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for server-side player object to change!");

            // Make sure the client-side changes are the same
            yield return WaitForConditionOrTimeOut(() => nonAuthority.LocalClient.PlayerObject != playerLocalClient && !playerLocalClient.IsPlayerObject
            && nonAuthority.LocalClient.PlayerObject.IsPlayerObject);
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
                foreach (var clientPlayer in m_NetworkManagers)
                {
                    var playerObjectId = clientPlayer.LocalClient.PlayerObject.NetworkObjectId;
                    foreach (var client in m_NetworkManagers)
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

    /// <summary>
    /// This test validates the player position and rotation is correct
    /// relative to the prefab's initial settings if no changes are applied.
    /// </summary>
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class PlayerSpawnPositionTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 2;

        public PlayerSpawnPositionTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        private Vector3 m_PlayerPosition;
        private Quaternion m_PlayerRotation;

        protected override void OnCreatePlayerPrefab()
        {
            var playerNetworkObject = m_PlayerPrefab.GetComponent<NetworkObject>();
            m_PlayerPosition = GetRandomVector3(-10.0f, 10.0f);
            m_PlayerRotation = Quaternion.Euler(GetRandomVector3(-180.0f, 180.0f));
            playerNetworkObject.transform.position = m_PlayerPosition;
            playerNetworkObject.transform.rotation = m_PlayerRotation;
            base.OnCreatePlayerPrefab();
        }

        private void PlayerTransformMatches(NetworkObject player)
        {
            var position = player.transform.position;
            var rotation = player.transform.rotation;
            Assert.True(Approximately(m_PlayerPosition, position), $"Client-{player.OwnerClientId} position {position} does not match the prefab position {m_PlayerPosition}!");
            Assert.True(Approximately(m_PlayerRotation, rotation), $"Client-{player.OwnerClientId} rotation {rotation.eulerAngles} does not match the prefab rotation {m_PlayerRotation.eulerAngles}!");
        }

        [UnityTest]
        public IEnumerator PlayerSpawnPosition()
        {
            var authority = GetAuthorityNetworkManager();
            if (authority.IsHost)
            {
                PlayerTransformMatches(authority.LocalClient.PlayerObject);

                foreach (var client in m_ClientNetworkManagers)
                {
                    yield return WaitForConditionOrTimeOut(() => client.SpawnManager.SpawnedObjects.ContainsKey(authority.LocalClient.PlayerObject.NetworkObjectId));
                    AssertOnTimeout($"Client-{client.LocalClientId} does not contain a player prefab instance for client-{authority.LocalClientId}!");
                    PlayerTransformMatches(client.SpawnManager.SpawnedObjects[authority.LocalClient.PlayerObject.NetworkObjectId]);
                }
            }

            foreach (var client in m_ClientNetworkManagers)
            {
                yield return WaitForConditionOrTimeOut(() => authority.SpawnManager.SpawnedObjects.ContainsKey(client.LocalClient.PlayerObject.NetworkObjectId));
                AssertOnTimeout($"Client-{authority.LocalClientId} does not contain a player prefab instance for client-{client.LocalClientId}!");
                PlayerTransformMatches(authority.SpawnManager.SpawnedObjects[client.LocalClient.PlayerObject.NetworkObjectId]);
                foreach (var subClient in m_ClientNetworkManagers)
                {
                    yield return WaitForConditionOrTimeOut(() => subClient.SpawnManager.SpawnedObjects.ContainsKey(client.LocalClient.PlayerObject.NetworkObjectId));
                    AssertOnTimeout($"Client-{subClient.LocalClientId} does not contain a player prefab instance for client-{client.LocalClientId}!");
                    PlayerTransformMatches(subClient.SpawnManager.SpawnedObjects[client.LocalClient.PlayerObject.NetworkObjectId]);
                }
            }
        }
    }

    /// <summary>
    /// This test validates that when a player is spawned it has all observers
    /// properly set and the spawned and player object lists are properly populated.
    /// It also validates that when a player is despawned and the client is still connected
    /// that the client maintains its observers for other players.
    /// </summary>
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class PlayerSpawnAndDespawnTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 4;

        private StringBuilder m_ErrorLog = new StringBuilder();

        public PlayerSpawnAndDespawnTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        private bool ValidateObservers(ulong playerClientId, NetworkObject player, ref List<NetworkManager> networkManagers)
        {
            foreach (var networkManager in networkManagers)
            {
                if (player != null && player.IsSpawned)
                {
                    if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(player.NetworkObjectId))
                    {
                        m_ErrorLog.AppendLine($"Client-{networkManager.LocalClientId} does not contain a spawned object entry {nameof(NetworkObject)}-{player.NetworkObjectId} for Client-{playerClientId}!");
                        return false;
                    }

                    var playerClone = networkManager.SpawnManager.SpawnedObjects[player.NetworkObjectId];
                    foreach (var clientId in networkManager.ConnectedClientsIds)
                    {
                        if (!playerClone.IsNetworkVisibleTo(clientId))
                        {
                            m_ErrorLog.AppendLine($"Client-{networkManager.LocalClientId} failed visibility check for Client-{clientId} on {nameof(NetworkObject)}-{player.NetworkObjectId} for Client-{playerClientId}!");
                            return false;
                        }
                    }

                    var foundPlayerClone = (NetworkObject)null;
                    foreach (var playerObject in networkManager.SpawnManager.PlayerObjects)
                    {
                        if (playerObject.OwnerClientId == playerClientId && playerObject.NetworkObjectId == player.NetworkObjectId)
                        {
                            foundPlayerClone = playerObject;
                            break;
                        }
                    }
                    if (!foundPlayerClone)
                    {
                        m_ErrorLog.AppendLine($"Client-{networkManager.LocalClientId} does not contain a player entry for {nameof(NetworkObject)}-{player.NetworkObjectId} for Client-{playerClientId}!");
                        return false;
                    }

                }
                else
                {
                    // If the player client in question is despawned, then no NetworkManager instance
                    // should contain a clone of that (or the client's NetworkManager instance as well)
                    foreach (var playerClone in networkManager.SpawnManager.PlayerObjects)
                    {
                        if (playerClone.OwnerClientId == playerClientId)
                        {
                            m_ErrorLog.AppendLine($"Client-{networkManager.LocalClientId} contains a player {nameof(NetworkObject)}-{playerClone.NetworkObjectId} for Client-{playerClientId} when it should be despawned!");
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool ValidateAllClientPlayerObservers()
        {
            var networkManagers = new List<NetworkManager>();

            m_ErrorLog.Clear();
            var success = true;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.IsServer && !networkManager.IsHost)
                {
                    continue;
                }

                var spawnedOrNot = networkManager.LocalClient.PlayerObject == null ? "despawned" : "spawned";
                m_ErrorLog.AppendLine($"Validating Client-{networkManager.LocalClientId} {spawnedOrNot} player.");
                if (networkManager.LocalClient == null)
                {
                    m_ErrorLog.AppendLine($"No {nameof(NetworkClient)} found for Client-{networkManager.LocalClientId}!");
                    success = false;
                    break;
                }
                if (!ValidateObservers(networkManager.LocalClientId, networkManager.LocalClient.PlayerObject, ref networkManagers))
                {
                    m_ErrorLog.AppendLine($"Client-{networkManager.LocalClientId} validation pass failed.");
                    success = false;
                    break;
                }
            }
            networkManagers.Clear();
            return success;
        }


        [UnityTest]
        public IEnumerator PlayerSpawnDespawn()
        {
            // Validate all observers are properly set with all players spawned
            yield return WaitForConditionOrTimeOut(ValidateAllClientPlayerObservers);
            AssertOnTimeout($"First Validation Failed:\n {m_ErrorLog}");
            var selectedClient = m_ClientNetworkManagers[Random.Range(0, m_ClientNetworkManagers.Count() - 1)];
            var playerSelected = selectedClient.LocalClient.PlayerObject;

            if (m_DistributedAuthority)
            {
                playerSelected.Despawn(false);
            }
            else
            {
                m_ServerNetworkManager.SpawnManager.SpawnedObjects[playerSelected.NetworkObjectId].Despawn(true);
            }

            // Validate all observers are properly set with one of the players despawned
            yield return WaitForConditionOrTimeOut(ValidateAllClientPlayerObservers);
            AssertOnTimeout($"Second Validation Failed:\n {m_ErrorLog}");

            if (m_DistributedAuthority)
            {
                playerSelected.SpawnAsPlayerObject(selectedClient.LocalClientId, false);
            }
            else
            {
                SpawnPlayerObject(m_ServerNetworkManager.NetworkConfig.PlayerPrefab, selectedClient);
            }

            // Validate all observers are properly set when the client's player is respawned.
            yield return WaitForConditionOrTimeOut(ValidateAllClientPlayerObservers);
            AssertOnTimeout($"Third Validation Failed:\n {m_ErrorLog}");
        }
    }
}
