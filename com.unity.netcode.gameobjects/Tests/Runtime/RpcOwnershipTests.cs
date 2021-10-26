using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class RpcOwnershipTest : NetworkBehaviour
    {

    }

    public class RpcOwnershipObject : NetworkBehaviour
    {
        public int RequireOwnershipCount = 0;
        public int DoesntRequireOwnershipCount = 0;

        [ServerRpc(RequireOwnership = true)]
        public void RequireOwnershipServerRpc()
        {
            RequireOwnershipCount++;
        }

        [ServerRpc(RequireOwnership = false)]
        public void DoesntRequireOwnershipServerRpc()
        {
            DoesntRequireOwnershipCount++;
        }
    }

    public class RpcOwnershipTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        private GameObject m_PrefabToSpawn;

        private int m_ExpectedRequireOwnershipCount = 0;
        private int m_ExpectedDoesntRequireOwnershipCount = 0;


        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    var networkTransform = playerPrefab.AddComponent<RpcOwnershipTest>();
                    m_PrefabToSpawn = PreparePrefab(typeof(RpcOwnershipObject));
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
        public IEnumerator RpcOwnershipTest()
        {
            yield return RunTests(false);
            yield return RunTests(true);
        }

        private IEnumerator RunTests(bool serverOwned)
        {
            m_ExpectedRequireOwnershipCount = 0;
            m_ExpectedDoesntRequireOwnershipCount = 0;

            var spawnedObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            var netSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
            netSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;

            if (serverOwned)
            {
                netSpawnedObject.Spawn();
            }
            else
            {
                netSpawnedObject.SpawnWithOwnership(m_ClientNetworkManagers[1].LocalClientId);
            }

            // send RPCs from server
            if (!serverOwned)
            {
                LogAssert.Expect(LogType.Error, "Only the owner can invoke a ServerRpc that requires ownership!");
            }
            else
            {
                m_ExpectedRequireOwnershipCount++;
            }

            m_ExpectedDoesntRequireOwnershipCount++;
            spawnedObject.GetComponent<RpcOwnershipObject>().RequireOwnershipServerRpc();
            spawnedObject.GetComponent<RpcOwnershipObject>().DoesntRequireOwnershipServerRpc();

            // get the matching object on the client side
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                    x => x.NetworkObjectId == netSpawnedObject.NetworkObjectId,
                    m_ClientNetworkManagers[1],
                    serverClientPlayerResult));
            var netSpawnedObjectOnClient = serverClientPlayerResult.Result;
            netSpawnedObjectOnClient.NetworkManagerOwner = m_ClientNetworkManagers[1];

            // send RPCs from the client
            if (serverOwned) // condition is reversed, compared to above
            {
                LogAssert.Expect(LogType.Error, "Only the owner can invoke a ServerRpc that requires ownership!");
            }
            else
            {
                m_ExpectedRequireOwnershipCount++;
            }

            m_ExpectedDoesntRequireOwnershipCount++;
            netSpawnedObjectOnClient.GetComponent<RpcOwnershipObject>().RequireOwnershipServerRpc();
            netSpawnedObjectOnClient.GetComponent<RpcOwnershipObject>().DoesntRequireOwnershipServerRpc();

            yield return new WaitForSeconds(1.0f);

            // verify counts
            Debug.Assert(spawnedObject.GetComponent<RpcOwnershipObject>().RequireOwnershipCount == m_ExpectedRequireOwnershipCount);
            Debug.Assert(spawnedObject.GetComponent<RpcOwnershipObject>().DoesntRequireOwnershipCount == m_ExpectedDoesntRequireOwnershipCount);
        }
    }
}
