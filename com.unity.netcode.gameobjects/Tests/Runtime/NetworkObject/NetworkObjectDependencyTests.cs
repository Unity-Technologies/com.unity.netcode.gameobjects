using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Tests ensuring that dependent <see cref="NetworkObject"/>s are functioning properly. Expected behavior:
    /// - 
    /// </summary>
    public class NetworkObjectDependencyTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_PrefabToSpawn;

        protected override void OnCreatePlayerPrefab()
        {
            NetworkObject playerNetworkObject = m_PlayerPrefab.GetComponent<NetworkObject>();
            GameObject childObject = NetcodeIntegrationTestHelpers.AddNetworkObjectChildToPrefab(playerNetworkObject, "child");
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("PrefabWithChildNetworkObject");
            NetcodeIntegrationTestHelpers.AddNetworkObjectChildToPrefab(m_PrefabToSpawn.GetComponent<NetworkObject>(), "child");
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            var networkPrefab = new NetworkPrefab() { Prefab = m_PrefabToSpawn };
            networkManager.NetworkConfig.Prefabs.Add(networkPrefab);
        }


        /// <summary>
        /// Tests that depending <see cref="NetworkObject"/> on a player objects will be synchronized.
        /// </summary>
        [UnityTest]
        public IEnumerator TestPlayerDependingObjects()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ServerNetworkManager, serverClientPlayerResult);

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ClientNetworkManagers[0], clientClientPlayerResult);

            Assert.IsNotNull(serverClientPlayerResult.Result.gameObject);
            Assert.IsNotNull(clientClientPlayerResult.Result.gameObject);

            var serverClientPlayerChild = serverClientPlayerResult.Result.transform.GetChild(0)?.GetComponent<NetworkObject>();
            var clientClientPlayerChild = clientClientPlayerResult.Result.transform.GetChild(0)?.GetComponent<NetworkObject>();

            Assert.IsNotNull(serverClientPlayerChild);
            Assert.IsNotNull(clientClientPlayerChild);

            Assert.IsTrue(serverClientPlayerChild.NetworkObjectId == clientClientPlayerChild.NetworkObjectId); // They should have the same NetworkObjectId
            Assert.IsTrue(serverClientPlayerChild.NetworkObjectId > default(ulong)); // and that id should have been set 
        }

        /// <summary>
        /// Tests that depending <see cref="NetworkObject"/>s can be reparented
        /// and that the reparenting will be synchronized to late-joining clients.
        /// </summary>
        [UnityTest]
        public IEnumerator TestDependingObjectReparenting()
        {
            var serverDependingInstance = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager).transform.GetChild(0)?.GetComponent<NetworkObject>();
            Assert.IsNotNull(serverDependingInstance); // Sanity check

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<CreateObjectMessage>(m_ClientNetworkManagers[0]);

            serverDependingInstance.transform.parent = null;

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<ParentSyncMessage>(m_ClientNetworkManagers[0]);

            var clientDepending1Instance = s_GlobalNetworkObjects[m_ClientNetworkManagers[0].LocalClientId][serverDependingInstance.NetworkObjectId];
            Assert.IsNull(clientDepending1Instance.transform.parent); // Make sure the client instance was reparented

            yield return CreateAndStartNewClient();

            var clientDepending2Instance = s_GlobalNetworkObjects[m_ClientNetworkManagers[1].LocalClientId][serverDependingInstance.NetworkObjectId];
            Assert.IsNull(clientDepending2Instance.transform.parent); // Make sure the late-joining client instance was reparented
        }

        /// <summary>
        /// Tests that depending <see cref="NetworkObject"/>s can be deleted,
        /// and that those deletions will be synchronized across both connected 
        /// and late-joining clients.
        /// </summary>
        [UnityTest]
        public IEnumerator TestDependingObjectDeletion()
        {
            var serverDependentInstance = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager).GetComponent<NetworkObject>();
            var serverDependingInstance = serverDependentInstance.transform.GetChild(0)?.GetComponent<NetworkObject>();
            Assert.IsNotNull(serverDependingInstance); // Sanity check

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<CreateObjectMessage>(m_ClientNetworkManagers[0]);

            var clientDepending1Instance = s_GlobalNetworkObjects[m_ClientNetworkManagers[0].LocalClientId][serverDependingInstance.NetworkObjectId];
            Object.Destroy(serverDependingInstance.gameObject);

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<DestroyObjectMessage>(m_ClientNetworkManagers[0]);

            Assert.IsTrue(clientDepending1Instance == null, "Dependent NetworkObject was not destroyed on connected client.");

            yield return CreateAndStartNewClient();

            Assert.IsTrue(
                !s_GlobalNetworkObjects[m_ClientNetworkManagers[1].LocalClientId].ContainsKey(serverDependingInstance.NetworkObjectId) ||
                s_GlobalNetworkObjects[m_ClientNetworkManagers[1].LocalClientId][serverDependingInstance.NetworkObjectId] == null,
                "Dependent NetworkObject was not destroyed on late-joining client.");
        }

        /// <summary>
        /// Tests that deleting <see cref="NetworkObject"/>s also deletes any
        /// <see cref="NetworkObject"/>s that are dependent on the deleted one.
        /// </summary>
        [UnityTest]
        public IEnumerator TestDependentObjectDeletion()
        {
            var serverDependentInstance = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager).GetComponent<NetworkObject>();
            Assert.IsTrue(serverDependentInstance.DependingNetworkObjects.Count > 0); // Make sure the prefab has a dependent NetworkObject
            var serverDependingInstance = serverDependentInstance.DependingNetworkObjects[0];

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<CreateObjectMessage>(m_ClientNetworkManagers[0]);

            var clientDependentInstance = s_GlobalNetworkObjects[m_ClientNetworkManagers[0].LocalClientId][serverDependentInstance.NetworkObjectId];
            var clientDependingInstance = clientDependentInstance.DependingNetworkObjects[0];
            Object.Destroy(serverDependentInstance.gameObject);

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeHandled<DestroyObjectMessage>(m_ClientNetworkManagers[0]); // Wait for parent deleting

            Assert.IsTrue(serverDependentInstance == null, "Dependent NetworkObject was not destroyed on host.");
            Assert.IsTrue(serverDependingInstance == null, "Depending NetworkObject was not destroyed on host.");
            Assert.IsTrue(clientDependentInstance == null, "Dependent NetworkObject was not destroyed on connected client.");
            Assert.IsTrue(clientDependingInstance == null, "Depending NetworkObject was not destroyed on connected client.");
        }
    }
}
