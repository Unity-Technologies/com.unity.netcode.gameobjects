using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class ObjectNameIdentifierTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_Prefab;

        protected override void OnServerAndClientsCreated()
        {
            m_Prefab = CreateNetworkObjectPrefab("TrackedObject");
            base.OnServerAndClientsCreated();
        }

        private bool AllInstancesAreRegistered(StringBuilder errorLog, ulong networkObjectId)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!s_GlobalNetworkObjects.TryGetValue(networkManager.LocalClientId, out var networkObjects) || !networkObjects.ContainsKey(networkObjectId))
                {
                    errorLog.Append($"[Client-{networkManager.LocalClientId}] Object-{networkObjectId} is not registered.");
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator OwnershipChangesKeepAllInstancesRegistered()
        {
            var authority = GetAuthorityNetworkManager();
            var client = m_ClientNetworkManagers[0];
            var authorityInstance = SpawnObject(m_Prefab, authority).GetComponent<NetworkObject>();
            var networkObjectId = authorityInstance.NetworkObjectId;
            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance);
            AssertOnTimeout($"Timed out waiting for Object-{networkObjectId} to spawn on all instances!");

            authorityInstance.ChangeOwnership(client.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => client.SpawnManager.SpawnedObjects[networkObjectId].OwnerClientId == client.LocalClientId);
            AssertOnTimeout($"Timed out waiting for Client-{client.LocalClientId} to gain ownership of Object-{networkObjectId}!");
            var errorLog = new StringBuilder();
            Assert.IsTrue(AllInstancesAreRegistered(errorLog, networkObjectId), $"After gaining ownership: {errorLog}");

            authorityInstance.RemoveOwnership();
            yield return WaitForConditionOrTimeOut(() => client.SpawnManager.SpawnedObjects[networkObjectId].OwnerClientId == authority.LocalClientId);
            AssertOnTimeout($"Timed out waiting for the authority to regain ownership of Object-{networkObjectId}!");
            errorLog.Clear();
            Assert.IsTrue(AllInstancesAreRegistered(errorLog, networkObjectId), $"After removing ownership: {errorLog}");
        }
    }
}
