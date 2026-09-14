using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkVariableNameTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private NetworkVariableNameComponent m_NetworkVariableNameComponent;

        private GameObject m_PrefabToTest;

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToTest = CreateNetworkObjectPrefab("NetVarNameTest");
            m_PrefabToTest.AddComponent<NetworkVariableNameComponent>();
            base.OnServerAndClientsCreated();
        }

        [UnityTest]
        public IEnumerator VerifyNetworkVariableNameInitialization()
        {
            var authority = GetAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_PrefabToTest, authority);
            var authorityNetworkObject = authorityInstance.GetComponent<NetworkVariableNameComponent>();

            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout($"Not all clients spawned {authorityInstance.name}!");

            foreach (var networkManager in m_NetworkManagers)
            {
                var componentInstance = networkManager.SpawnManager.SpawnedObjects[authorityNetworkObject.NetworkObjectId].GetComponent<NetworkVariableNameComponent>();
                // Verify fields have regular naming
                Assert.AreEqual(nameof(NetworkVariableNameComponent.NetworkVarList), componentInstance.NetworkVarList.Name);
            }
        }

        private class NetworkVariableNameComponent : NetworkBehaviour
        {
            public NetworkList<ulong> NetworkVarList = new NetworkList<ulong>();
        }
    }
}
