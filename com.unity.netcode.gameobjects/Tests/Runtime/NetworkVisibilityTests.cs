using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkVisibilityTests
    {
        private NetworkObject m_NetSpawnedObject;
        private GameObject m_TestNetworkPrefab;

        [TearDown]
        public void TearDown()
        {
            MultiInstanceHelpers.Destroy();
            if (m_TestNetworkPrefab)
            {
                Object.Destroy(m_TestNetworkPrefab);
                m_TestNetworkPrefab = null;
            }
        }

        [UnityTest]
        public IEnumerator HiddenObjectsTest([Values] bool enableSeneManagement)
        {

            const int numClients = 1;
            Assert.True(MultiInstanceHelpers.Create(numClients, out NetworkManager server, out NetworkManager[] clients));
            m_TestNetworkPrefab = new GameObject("Object");
            var networkObject = m_TestNetworkPrefab.AddComponent<NetworkObject>();
            m_TestNetworkPrefab.AddComponent<NetworkVisibilityComponent>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

            var validNetworkPrefab = new NetworkPrefab();
            validNetworkPrefab.Prefab = m_TestNetworkPrefab;
            server.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
            server.NetworkConfig.EnableSceneManagement = enableSeneManagement;
            foreach (var client in clients)
            {
                client.NetworkConfig.NetworkPrefabs.Add(validNetworkPrefab);
                client.NetworkConfig.EnableSceneManagement = enableSeneManagement;
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients, () =>
            {
                var serverObject = Object.Instantiate(m_TestNetworkPrefab, Vector3.zero, Quaternion.identity);
                NetworkObject serverNetworkObject = serverObject.GetComponent<NetworkObject>();
                serverNetworkObject.NetworkManagerOwner = server;
                serverNetworkObject.Spawn();
                serverObject.GetComponent<NetworkVisibilityComponent>().Hide();
            }))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // [Client-Side] Wait for a connection to the server
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients, null, 512));

            // [Host-Side] Check to make sure all clients are connected
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(server, clients.Length + 1, null, 512));

            Assert.AreEqual(2, Object.FindObjectsOfType<NetworkVisibilityComponent>().Length);
        }
    }
}
