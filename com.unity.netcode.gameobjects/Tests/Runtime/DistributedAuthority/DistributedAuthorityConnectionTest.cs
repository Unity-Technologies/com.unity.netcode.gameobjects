using System;
using System.Collections;
using System.Linq;
using System.Net;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class DistributedAuthorityConnectionTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        // Set the network topology to distributed authority for all tests
        protected override NetworkTopologyTypes OnGetNetworkTopologyType() => NetworkTopologyTypes.DistributedAuthority;

        public DistributedAuthorityConnectionTest() : base(HostOrServer.DAHost) { }

        private GameObject m_SpawnObject;

        protected override bool UseCMBService()
        {
            return true;
        }


        /// <summary>
        /// Modify NetworkManager instances for settings specific to tests
        /// </summary>
        protected override void OnServerAndClientsCreated()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.EnableSceneManagement = false;

                // Validate we are in distributed authority mode with client side spawning and using CMB Service
                Assert.True(client.NetworkConfig.NetworkTopology == NetworkTopologyTypes.DistributedAuthority, "Distributed authority topology is not set!");
                Assert.True(client.CMBServiceConnection, "CMBServiceConnection is not set!");
            }

            // Create a prefab for creating and destroying tests (auto-registers with NetworkManagers)
            m_SpawnObject = CreateNetworkObjectPrefab("TestObject");
        }
        [UnityTest]
        public IEnumerator CreateObjectNew()
        {
            SpawnObject(m_SpawnObject, m_ClientNetworkManagers[0]);

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
