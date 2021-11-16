using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

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

    public class RpcManyClientsTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 10;

        private GameObject m_PrefabToSpawn;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    m_PrefabToSpawn = PreparePrefab(typeof(RpcManyClientsObject));
                });
        }

        public GameObject PreparePrefab(Type type)
        {
            var prefabToSpawn = new GameObject();
            prefabToSpawn.AddComponent(type);
            var networkObjectPrefab = prefabToSpawn.AddComponent<NetworkObject>();
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObjectPrefab);
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
            int maxFrameNumber = Time.frameCount + 5;
            yield return new WaitUntil(() => rpcManyClientsObject.Count == (NbClients + 1) || Time.frameCount > maxFrameNumber);

            Debug.Assert(rpcManyClientsObject.Count == (NbClients + 1));

            rpcManyClientsObject.Count = 0;
            rpcManyClientsObject.OneParamClientRpc(0); // RPC with one param
            maxFrameNumber = Time.frameCount + 5;
            yield return new WaitUntil(() => rpcManyClientsObject.Count == (NbClients + 1) || Time.frameCount > maxFrameNumber);

            Debug.Assert(rpcManyClientsObject.Count == (NbClients + 1));

            var param = new ClientRpcParams();

            rpcManyClientsObject.Count = 0;
            rpcManyClientsObject.TwoParamsClientRpc(0, 0); // RPC with two params
            maxFrameNumber = Time.frameCount + 5;
            yield return new WaitUntil(() => rpcManyClientsObject.Count == (NbClients + 1) || Time.frameCount > maxFrameNumber);

            Debug.Assert(rpcManyClientsObject.Count == (NbClients + 1));

            rpcManyClientsObject.ReceivedFrom.Clear();
            rpcManyClientsObject.Count = 0;
            var target = new List<ulong> { m_ClientNetworkManagers[1].LocalClientId, m_ClientNetworkManagers[2].LocalClientId };
            param.Send.TargetClientIds = target;
            rpcManyClientsObject.WithParamsClientRpc(param);
            maxFrameNumber = Time.frameCount + 5;
            yield return new WaitUntil(() => Time.frameCount > maxFrameNumber);

            // either of the 2 selected clients can reply to the server first, due to network timing
            var possibility1 = new List<ulong>
                {m_ClientNetworkManagers[1].LocalClientId, m_ClientNetworkManagers[2].LocalClientId};
            var possibility2 = new List<ulong>
                {m_ClientNetworkManagers[2].LocalClientId, m_ClientNetworkManagers[1].LocalClientId};

            Debug.Assert(rpcManyClientsObject.Count == 2);
            Debug.Assert(Enumerable.SequenceEqual(rpcManyClientsObject.ReceivedFrom, possibility1) ||
                         Enumerable.SequenceEqual(rpcManyClientsObject.ReceivedFrom, possibility2));
        }
    }
}
