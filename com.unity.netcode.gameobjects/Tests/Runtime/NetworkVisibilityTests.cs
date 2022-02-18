using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkVisibilityTests : NetcodeIntegrationTest
    {
        protected override int NbClients => 1;
        private GameObject m_TestNetworkPrefab;

        protected override IEnumerator OnSetup()
        {
            m_TestNetworkPrefab = new GameObject("Object");
            var networkObject = m_TestNetworkPrefab.AddComponent<NetworkObject>();
            m_TestNetworkPrefab.AddComponent<NetworkVisibilityComponent>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            var validNetworkPrefab = new NetworkPrefab();
            validNetworkPrefab.Prefab = m_TestNetworkPrefab;
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = false;
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
                client.NetworkConfig.EnableSceneManagement = false;
            }
        }

        protected override IEnumerator OnServerAndClientsStartedAndConnected()
        {
            var serverObject = Object.Instantiate(m_TestNetworkPrefab, Vector3.zero, Quaternion.identity);
            NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkVisibilityComponent>().Hide();
            serverNetworkObject.Spawn();

            yield return base.OnServerAndClientsStartedAndConnected();
        }

        [Test]
        public void HiddenObjectsTest()
        {
            Assert.AreEqual(2, Object.FindObjectsOfType<NetworkVisibilityComponent>().Length);
        }

        protected override IEnumerator OnTearDown()
        {
            Object.Destroy(m_TestNetworkPrefab);
            m_TestNetworkPrefab = null;

            return base.OnTearDown();
        }
    }
}
