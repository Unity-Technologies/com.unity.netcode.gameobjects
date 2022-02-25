using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class RpcManyClientsObject : NetworkBehaviour
    {
        public int Count = 0;
        public List<ulong> ReceivedFrom = new List<ulong>();
        [ServerRpc(RequireOwnership = false)]
        public void ResponseServerRpc(ServerRpcParams rpcParams = default)
        {
            ReceivedFrom.Add(rpcParams.Receive.SenderClientId);
            Count++;
        }

        [ClientRpc]
        public void NoParamsClientRpc()
        {
            ResponseServerRpc();
        }

        [ClientRpc]
        public void OneParamClientRpc(int value)
        {
            ResponseServerRpc();
        }

        [ClientRpc]
        public void TwoParamsClientRpc(int value1, int value2)
        {
            ResponseServerRpc();
        }

        [ClientRpc]
        public void WithParamsClientRpc(ClientRpcParams param)
        {
            ResponseServerRpc();
        }
    }

    public class RpcManyClientsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 10;

        private GameObject m_PrefabToSpawn;

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = PreparePrefab(typeof(RpcManyClientsObject));
        }

        public GameObject PreparePrefab(Type type)
        {
            var prefabToSpawn = new GameObject();
            prefabToSpawn.AddComponent(type);
            var networkObjectPrefab = prefabToSpawn.AddComponent<NetworkObject>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObjectPrefab);
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            }
            return prefabToSpawn;
        }

        [UnityTest]
        public IEnumerator RpcManyClientsTest()
        {
            var spawnedObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            var netSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
            netSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;

            netSpawnedObject.Spawn();


            var rpcManyClientsObject = netSpawnedObject.GetComponent<RpcManyClientsObject>();

            rpcManyClientsObject.Count = 0;
            rpcManyClientsObject.NoParamsClientRpc(); // RPC with no params
            var waiters = new List<IEnumerator>();

            foreach (var client in m_ClientNetworkManagers)
            {
                waiters.Add(MultiInstanceHelpers.WaitForMessageOfType<ServerRpcMessage>(m_ServerNetworkManager));
            }

            yield return MultiInstanceHelpers.RunMultiple(waiters);

            Assert.AreEqual(NbClients + 1, rpcManyClientsObject.Count);

            rpcManyClientsObject.Count = 0;
            rpcManyClientsObject.OneParamClientRpc(0); // RPC with one param
            waiters.Clear();
            for (int i = 0; i < m_ClientNetworkManagers.Length; i++)
            {
                waiters.Add(MultiInstanceHelpers.WaitForMessageOfType<ServerRpcMessage>(m_ServerNetworkManager));
            }

            yield return MultiInstanceHelpers.RunMultiple(waiters);

            Debug.Assert(rpcManyClientsObject.Count == (NumberOfClients + 1));

            var param = new ClientRpcParams();

            rpcManyClientsObject.Count = 0;
            rpcManyClientsObject.TwoParamsClientRpc(0, 0); // RPC with two params
            waiters.Clear();
            for (int i = 0; i < m_ClientNetworkManagers.Length; i++)
            {
                waiters.Add(MultiInstanceHelpers.WaitForMessageOfType<ServerRpcMessage>(m_ServerNetworkManager));
            }

            yield return MultiInstanceHelpers.RunMultiple(waiters);

            Assert.AreEqual(NbClients + 1, rpcManyClientsObject.Count);

            rpcManyClientsObject.ReceivedFrom.Clear();
            rpcManyClientsObject.Count = 0;
            var target = new List<ulong> { m_ClientNetworkManagers[1].LocalClientId, m_ClientNetworkManagers[2].LocalClientId };
            param.Send.TargetClientIds = target;
            rpcManyClientsObject.WithParamsClientRpc(param);
            waiters.Clear();
            waiters.Add(MultiInstanceHelpers.WaitForMessageOfType<ServerRpcMessage>(m_ServerNetworkManager));
            waiters.Add(MultiInstanceHelpers.WaitForMessageOfType<ServerRpcMessage>(m_ServerNetworkManager));

            yield return MultiInstanceHelpers.RunMultiple(waiters);

            // either of the 2 selected clients can reply to the server first, due to network timing
            var possibility1 = new List<ulong> { m_ClientNetworkManagers[1].LocalClientId, m_ClientNetworkManagers[2].LocalClientId };
            var possibility2 = new List<ulong> { m_ClientNetworkManagers[2].LocalClientId, m_ClientNetworkManagers[1].LocalClientId };

            Assert.AreEqual(2, rpcManyClientsObject.Count);
            Debug.Assert(Enumerable.SequenceEqual(rpcManyClientsObject.ReceivedFrom, possibility1) || Enumerable.SequenceEqual(rpcManyClientsObject.ReceivedFrom, possibility2));
        }
    }
}
