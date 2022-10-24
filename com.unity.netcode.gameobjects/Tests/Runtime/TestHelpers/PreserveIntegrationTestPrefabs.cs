using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class PreserveIntegrationTestPrefabs : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private List<GameObject> m_Prefabs = new List<GameObject>();

        public PreserveIntegrationTestPrefabs(HostOrServer hostOrServer)
        {
            m_UseHost = hostOrServer == HostOrServer.Host;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_Prefabs.Add(CreateNetworkObjectPrefab("TestPrefab1"));
            m_Prefabs.Add(CreateNetworkObjectPrefab("TestPrefab2"));
        }

        private void CheckPrefabs(bool beforeShutdown = true)
        {
            var beforOrAfterLabel = beforeShutdown ? "Before Shutdown" : "After Shutdown";
            Assert.True(m_ServerNetworkManager.NetworkConfig.PlayerPrefab != null, $"[{beforeShutdown}][Server] Player prefab is null!");
            for (int i = 0; i < NumberOfClients; i++)
            {
                Assert.True(m_ClientNetworkManagers[i].NetworkConfig.PlayerPrefab != null, $"[{beforeShutdown}][Client NetworkManager-{i}] Player prefab is null!");
            }
        }

        private bool ServerAndClientsShutdown()
        {
            if (m_ServerNetworkManager.ShutdownInProgress)
            {
                return false;
            }

            for (int i = 0; i < NumberOfClients; i++)
            {
                if (m_ClientNetworkManagers[i].ShutdownInProgress)
                {
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator PreservePrefabsDuringShutdown()
        {
            CheckPrefabs();
            m_ServerNetworkManager.Shutdown();
            m_ClientNetworkManagers[0].Shutdown();
            m_ClientNetworkManagers[1].Shutdown();

            yield return WaitForConditionOrTimeOut(ServerAndClientsShutdown);
            AssertOnTimeout("Timed out waiting for the server and all clients to shutdown!");

            m_ServerNetworkManager.StartHost();
            m_ClientNetworkManagers[0].StartClient();
            m_ClientNetworkManagers[1].StartClient();

            yield return WaitForClientsConnectedOrTimeOut();

            CheckPrefabs();
        }
    }
}
