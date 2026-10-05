#if UNIFIED_NETCODE
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using EntitiesNetcode = Unity.Netcode.Netcode;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// N4E spawns a ghost into its active world, which is whichever world updated last when several worlds share a process.
    /// Validates that a hybrid instance's ghost is spawned in the spawning <see cref="NetworkManager"/>'s world anyway.
    /// </summary>
    [TestFixture(HostOrServer.UnifiedHost)]
    [TestFixture(HostOrServer.UnifiedServer)]
    internal class HybridSpawnWorldSelectionTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_HybridPrefab;

        public HybridSpawnWorldSelectionTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        // The harness checks for a client's player as soon as it connects, and a hybrid player spawns on the client only once its ghost arrives.
        // LateJoinPlayerSpawnsGhostInServerWorld waits for it instead.
        protected override bool ShouldCheckForSpawnedPlayers()
        {
            return false;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_HybridPrefab = CreateNetworkObjectPrefab("SpawnWorldPrefab");

            // The default player prefab is not hybrid. Late joining clients also use m_PlayerPrefab.
            Object.DestroyImmediate(m_PlayerPrefab);
            m_PlayerPrefab = CreateNetworkObjectPrefab("HybridPlayer");
            foreach (var networkManager in m_NetworkManagers)
            {
                networkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// Makes a client's world N4E's active world, as it is when that client's world updated last.
        /// </summary>
        private void MakeClientWorldActive()
        {
            EntitiesNetcode.Instance.m_ActiveWorld = m_ClientNetworkManagers[0].NetcodeWorld;
        }

        private IEnumerator AssertGhostSpawnedInServerWorld(NetworkObject serverInstance, string context)
        {
            Assert.IsTrue(serverInstance.HasGhost, $"[{context}] {serverInstance.name} is not a hybrid instance!");
            var ghostObject = serverInstance.GhostObject;
            Assert.IsTrue(ghostObject.IsSpawned, $"[{context}] The ghost of {serverInstance.name} was not spawned!");
            Assert.AreEqual(m_ServerNetworkManager.NetcodeWorld, ghostObject.World, $"[{context}] The ghost of {serverInstance.name} was spawned in {ghostObject.World.Name} instead of the server's world!");
            yield return WaitForSpawnedOnAllOrTimeOut(serverInstance);
            AssertOnTimeout($"[{context}] Timed out waiting for {serverInstance.name} to spawn on all clients!");
        }

        [UnityTest]
        public IEnumerator InstantiateAndSpawnSpawnsGhostInServerWorld()
        {
            MakeClientWorldActive();
            var serverInstance = m_ServerNetworkManager.SpawnManager.InstantiateAndSpawn(m_HybridPrefab.GetComponent<NetworkObject>());
            yield return AssertGhostSpawnedInServerWorld(serverInstance, nameof(NetworkSpawnManager.InstantiateAndSpawn));
        }

        [UnityTest]
        public IEnumerator InstantiateThenSpawnSpawnsGhostInServerWorld()
        {
            MakeClientWorldActive();
            var instance = Object.Instantiate(m_HybridPrefab).GetComponent<NetworkObject>();
            Assert.IsFalse(instance.GhostObject.IsSpawned, "The ghost was spawned when it was instantiated, before its NetworkObject was spawned!");
            instance.NetworkManagerOwner = m_ServerNetworkManager;
            instance.Spawn();
            yield return AssertGhostSpawnedInServerWorld(instance, nameof(NetworkObject.Spawn));
        }

        [UnityTest]
        public IEnumerator LateJoinPlayerSpawnsGhostInServerWorld()
        {
            MakeClientWorldActive();
            yield return CreateAndStartNewClient();
            var newClient = m_ClientNetworkManagers[m_ClientNetworkManagers.Length - 1];
            var playerObject = m_ServerNetworkManager.ConnectionManager.ConnectedClients[newClient.LocalClientId].PlayerObject;
            Assert.IsNotNull(playerObject, $"The server did not spawn a player for client {newClient.LocalClientId}!");
            yield return AssertGhostSpawnedInServerWorld(playerObject, "Player");
            yield return WaitForConditionOrTimeOut(() => newClient.LocalClient.PlayerObject != null);
            AssertOnTimeout($"Client {newClient.LocalClientId} never had its player assigned!");
        }
    }
}
#endif
