using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectNetworkClientOwnedObjectsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;
        private GameObject m_NetworkObject;

        protected override void OnServerAndClientsCreated()
        {
            m_NetworkObject = CreateNetworkObjectPrefab("ClientOwnedObject");
        }

        [UnityTest]
        public IEnumerator ChangeOwnershipOwnedObjectsAddTest()
        {
            NetworkObject serverObject = m_NetworkObject.GetComponent<NetworkObject>();
            serverObject.NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.Spawn();

            // Provide enough time for the client to receive and process the spawned message.
            yield return s_DefaultWaitForTick;

            // The object is owned by server
            Assert.False(m_ServerNetworkManager.SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));

            // Change the ownership
            serverObject.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            // Provide enough time for the client to receive and process the change in ownership message.
            yield return s_DefaultWaitForTick;

            // Ensure it's now added to the list
            yield return WaitForConditionOrTimeOut(() => m_ClientNetworkManagers[0].SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for client to gain ownership!");
            Assert.True(m_ClientNetworkManagers[0].SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));
            Assert.True(m_ServerNetworkManager.SpawnManager.GetClientOwnedObjects(m_ClientNetworkManagers[0].LocalClientId).Any(x => x.NetworkObjectId == serverObject.NetworkObjectId));
        }
    }
}
