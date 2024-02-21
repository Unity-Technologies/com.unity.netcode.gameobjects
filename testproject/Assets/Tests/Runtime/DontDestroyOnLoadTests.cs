using System.Collections;
using System.Text;
using NUnit.Framework;
using TestProject.ManualTests;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class DontDestroyOnLoadTests : NetcodeIntegrationTest
    {
        private const int k_ClientsToConnect = 4;
        protected override int NumberOfClients => 0;
        private GameObject m_DontDestroyOnLoadObject;

        protected override void OnServerAndClientsCreated()
        {
            m_DontDestroyOnLoadObject = CreateNetworkObjectPrefab("DDOLObject");
            m_DontDestroyOnLoadObject.AddComponent<ObjectToNotDestroyBehaviour>();
            base.OnServerAndClientsCreated();
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.Prefabs = m_ServerNetworkManager.NetworkConfig.Prefabs;
            base.OnNewClientCreated(networkManager);
        }

        private ulong m_SpawnedNetworkObjectId;
        private StringBuilder m_ErrorLog = new StringBuilder();

        private bool AllClientsSpawnedObjectIntoDDOL()
        {
            var passed = true;
            m_ErrorLog.Clear();
            foreach (var client in m_ClientNetworkManagers)
            {
                if (!client.SpawnManager.SpawnedObjects.ContainsKey(m_SpawnedNetworkObjectId))
                {
                    m_ErrorLog.AppendLine($"[Client-{client.LocalClientId}] Has not spanwed NetworkObjectId: {m_SpawnedNetworkObjectId}!");
                    passed = false;
                    continue;
                }

                var spawnedObject = client.SpawnManager.SpawnedObjects[m_SpawnedNetworkObjectId];
                if (spawnedObject.NetworkManager == client && spawnedObject.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    m_ErrorLog.AppendLine($"[Client-{client.LocalClientId}] {spawnedObject.name} is not in DDOL scene but is in scene {spawnedObject.gameObject.scene.name}");
                    passed = false;
                    continue;
                }
            }
            return passed;
        }

        [UnityTest]
        public IEnumerator ValidateNetworkObjectSynchronization()
        {
            var objectInstance = SpawnObject(m_DontDestroyOnLoadObject, m_ServerNetworkManager);
            m_SpawnedNetworkObjectId = objectInstance.GetComponent<NetworkObject>().NetworkObjectId;
            // Wait a tick for the object to be automatically migrated into the DDOL
            yield return s_DefaultWaitForTick;

            Assert.IsTrue(objectInstance.scene.name == "DontDestroyOnLoad");

            for (int i = 0; i < k_ClientsToConnect; i++)
            {
                yield return CreateAndStartNewClient();
            }

            yield return WaitForConditionOrTimeOut(AllClientsSpawnedObjectIntoDDOL);
            AssertOnTimeout($"[DDOL Test Failure] Reason for failure:\n {m_ErrorLog}");
        }
    }
}
