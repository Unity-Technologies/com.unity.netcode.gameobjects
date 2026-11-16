#if UNIFIED_NETCODE
using System.Collections;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using EntitiesNetcode = Unity.Netcode.Netcode;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Validates that a hybrid instance's ghost spawns in the spawning <see cref="NetworkManager"/>'s world,
    /// even when another world is N4E's active world.
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

        /// <summary>
        /// Disables the harness check for each client's player when the client connects.
        /// </summary>
        /// <remarks>
        /// A hybrid player spawns on a client only once its ghost arrives. <see cref="LateJoinPlayerSpawnsGhostInServerWorld"/> waits for it instead.
        /// </remarks>
        /// <returns>false</returns>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MakeWorldActive(NetworkManager networkManager)
        {
            EntitiesNetcode.Instance.m_ActiveWorld = networkManager.NetcodeWorld;
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
            MakeWorldActive(m_ClientNetworkManagers[0]);
            var serverInstance = m_ServerNetworkManager.SpawnManager.InstantiateAndSpawn(m_HybridPrefab.GetComponent<NetworkObject>());
            yield return AssertGhostSpawnedInServerWorld(serverInstance, nameof(NetworkSpawnManager.InstantiateAndSpawn));
        }

        [UnityTest]
        public IEnumerator InstantiateThenSpawnSpawnsGhostInServerWorld()
        {
            MakeWorldActive(m_ClientNetworkManagers[0]);
            var instance = Object.Instantiate(m_HybridPrefab).GetComponent<NetworkObject>();
            Assert.IsFalse(instance.GhostObject.IsSpawned, "The ghost was spawned when it was instantiated, before its NetworkObject was spawned!");
            instance.NetworkManagerOwner = m_ServerNetworkManager;
            instance.Spawn();
            yield return AssertGhostSpawnedInServerWorld(instance, nameof(NetworkObject.Spawn));
        }

        /// <summary>
        /// Makes a client's world active right before the server instantiates a player.
        /// </summary>
        /// <remarks>
        /// N4E makes the server world active again after each client world update. The server approves a connection outside
        /// of any world update. Making the client world active from the test body would therefore not reach the player spawn.
        /// </remarks>
        private class ClientWorldActivePlayerHandler : INetworkPrefabInstanceHandler
        {
            private readonly HybridSpawnWorldSelectionTests m_Test;

            public ClientWorldActivePlayerHandler(HybridSpawnWorldSelectionTests test)
            {
                m_Test = test;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                MakeWorldActive(m_Test.m_ClientNetworkManagers[0]);
                return Object.Instantiate(m_Test.m_PlayerPrefab, position, rotation).GetComponent<NetworkObject>();
            }

            public void Destroy(NetworkObject networkObject)
            {
                Object.Destroy(networkObject.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator LateJoinPlayerSpawnsGhostInServerWorld()
        {
            m_ServerNetworkManager.PrefabHandler.AddHandler(m_PlayerPrefab, new ClientWorldActivePlayerHandler(this));
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
