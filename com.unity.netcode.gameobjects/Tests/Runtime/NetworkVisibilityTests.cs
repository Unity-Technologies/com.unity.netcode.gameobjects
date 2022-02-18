using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkVisibilityTests : NetcodeIntegrationTest
    {
        protected override int NbClients => 1;
        private GameObject m_TestNetworkPrefab;
        private bool m_CanStartServerAndClients;


        protected override IEnumerator OnSetup()
        {
            m_CanStartServerAndClients = false;
            m_TestNetworkPrefab = new GameObject("Object");
            var networkObject = m_TestNetworkPrefab.AddComponent<NetworkObject>();
            m_TestNetworkPrefab.AddComponent<NetworkVisibilityComponent>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            return base.OnSetup();
        }

        protected override bool CanStartServerAndClients()
        {
            return m_CanStartServerAndClients;
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

        [UnityTest]
        public IEnumerator HiddenObjectsTest([Values] bool enableSceneManagement)
        {
            var validNetworkPrefab = new NetworkPrefab();
            validNetworkPrefab.Prefab = m_TestNetworkPrefab;
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = enableSceneManagement;
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
                client.NetworkConfig.EnableSceneManagement = enableSceneManagement;
            }

            m_CanStartServerAndClients = true;

            yield return StartServerAndClients();

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
