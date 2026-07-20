using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    internal class AddNetworkPrefabTest : NetcodeIntegrationTest
    {
        internal class EmptyComponent : NetworkBehaviour
        {

        }
        protected override int NumberOfClients => 1;

        private GameObject m_Prefab;

        protected override IEnumerator OnSetup()
        {
            // Host is irrelevant, messages don't get sent to the host "client"
            m_UseHost = false;

            yield return null;
        }

        private GameObject GenerateAndRegisterPrefab()
        {
            var originalPrefabInstance = NetcodeIntegrationTestHelpers.CreateNetworkObject("PrefabTest");
            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(originalPrefabInstance.GetComponent<NetworkObject>());
            

            m_ServerNetworkManager.NetworkConfig.SpawnTimeout = 0;
            m_ServerNetworkManager.NetworkConfig.ForceSamePrefabs = false;
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.SpawnTimeout = 0;
                client.NetworkConfig.ForceSamePrefabs = false;
            }
            return originalPrefabInstance;
        }

        protected override void OnServerAndClientsCreated()
        {
            RegisterPrefab();
        }

        private EmptyComponent GetObjectForClient(ulong clientId)
        {
            var emptyComponents = FindObjects.ByType<EmptyComponent>();
            foreach (var component in emptyComponents)
            {
                if (component.IsSpawned && component.NetworkManager.LocalClientId == clientId)
                {
                    var prefabGlobalObjectIdHash = m_Prefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
                    var componentGlobalObjectIdHash = m_Prefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
                    if (prefabGlobalObjectIdHash == componentGlobalObjectIdHash)
                    {
                        return component;
                    }
                }
            }
            return null;
        }

        private void RegisterPrefab(bool includeClients = true)
        {
            m_Prefab = GenerateAndRegisterPrefab();
            m_ServerNetworkManager.AddNetworkPrefab(m_Prefab);
            if (!includeClients)
            {
                return;
            }
            foreach (var client in m_ClientNetworkManagers)
            {
                client.AddNetworkPrefab(m_Prefab);
            }
        }

        private void DeregisterPrefab()
        {
            m_ServerNetworkManager.RemoveNetworkPrefab(m_Prefab);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.RemoveNetworkPrefab(m_Prefab);
            }
        }

        private static CoroutineRunner s_CoroutineRunner;

        [UnityTest]
        public IEnumerator WhenSpawningBeforeAddingPrefab_SpawnFails()
        {
            var serverObject = Object.Instantiate(m_Prefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<CreateObjectMessage>(m_ClientNetworkManagers[0]);
            Assert.IsNull(GetObjectForClient(m_ClientNetworkManagers[0].LocalClientId));
        }

        [UnityTest]
        public IEnumerator WhenSpawningAfterAddingServerPrefabButBeforeAddingClientPrefab_SpawnFails()
        {
            RegisterPrefab(false);

            var serverObject = Object.Instantiate(m_Prefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<CreateObjectMessage>(m_ClientNetworkManagers[0]);
            Assert.IsNull(GetObjectForClient(m_ClientNetworkManagers[0].LocalClientId));
        }

        [UnityTest]
        public IEnumerator WhenSpawningAfterAddingPrefabOnServerAndClient_SpawnSucceeds()
        {
            RegisterPrefab();

            var serverObject = Object.Instantiate(m_Prefab);
            var serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            serverNetworkObject.Spawn();
            yield return WaitForSpawnedOnAllOrTimeOut(serverObject);
            AssertOnTimeout($"{serverObject.name} did not spawn on all clients!");
            Assert.IsTrue(m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(serverNetworkObject.NetworkObjectId), $"Client did not spawn object!");
        }

        [UnityTest]
        public IEnumerator WhenSpawningAfterRemovingPrefabOnClient_SpawnFails()
        {
            RegisterPrefab();

            var serverObject = Object.Instantiate(m_Prefab);
            var serverNetworkObject = serverObject.GetComponent<NetworkObject>();

            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return WaitForSpawnedOnAllOrTimeOut(serverObject);
            AssertOnTimeout($"{serverObject.name} did not spawn on all clients!");
            Assert.IsTrue(m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(serverNetworkObject.NetworkObjectId), $"Client did not spawn object!");

            serverObject.GetComponent<NetworkObject>().Despawn();
            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<DestroyObjectMessage>(m_ClientNetworkManagers[0]);
            Assert.IsNull(GetObjectForClient(m_ClientNetworkManagers[0].LocalClientId));

            DeregisterPrefab();

            serverObject = Object.Instantiate(m_Prefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<CreateObjectMessage>(m_ClientNetworkManagers[0]);
            Assert.IsNull(GetObjectForClient(m_ClientNetworkManagers[0].LocalClientId));
        }
    }
}
