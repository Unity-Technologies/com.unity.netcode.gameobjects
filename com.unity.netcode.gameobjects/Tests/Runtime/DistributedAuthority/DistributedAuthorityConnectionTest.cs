using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class DistributedAuthorityConnectionTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private NetworkManager SessionOwner => m_ClientNetworkManagers[0];

        // Use the CMB Service for all tests
        protected override bool UseCMBService() => true;

        // Set the network topology to distributed authority for all tests
        protected override NetworkTopologyTypes OnGetNetworkTopologyType() => NetworkTopologyTypes.DistributedAuthority;


        public DistributedAuthorityConnectionTest() : base(NetworkTopologyTypes.DistributedAuthority, HostOrServer.DAHost) { }


        private GameObject m_SpawnObject;

        internal class TestNetworkComponent : NetworkBehaviour
        {
        }

        /// <summary>
        /// Add any additional components to default player prefab
        /// </summary>
        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<TestNetworkComponent>();
            base.OnCreatePlayerPrefab();
        }

        /// <summary>
        /// Modify NetworkManager instances for settings specific to tests
        /// </summary>
        protected override void OnServerAndClientsCreated()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.EnableSceneManagement = false;
                client.NetworkConfig.AutoSpawnPlayerPrefabClientSide = true;
            }
            SessionOwner.LogLevel = LogLevel.Developer;

            // Validate we are in distributed authority mode with client side spawning and using CMB Service
            Assert.True(SessionOwner.NetworkConfig.NetworkTopology == NetworkTopologyTypes.DistributedAuthority, "Distributed authority topology is not set!");
            Assert.True(SessionOwner.AutoSpawnPlayerPrefabClientSide, "Client side spawning is not set!");
            Assert.True(SessionOwner.CMBServiceConnection, "CMBServiceConnection is not set!");

            // Create a prefab for creating and destroying tests (auto-registers with NetworkManagers)
            m_SpawnObject = CreateNetworkObjectPrefab("TestObject");
        }

        [UnityTest]
        public IEnumerator CreateObjectNew()
        {
            SpawnObject(m_SpawnObject, SessionOwner);

            yield return WaitForConditionOrTimeOut(CheckObjectExists);
            AssertOnTimeout("failed to spawn object!");
        }


        private bool CheckObjectExists()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                if (!s_GlobalNetworkObjects.ContainsKey(client.LocalClientId))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
