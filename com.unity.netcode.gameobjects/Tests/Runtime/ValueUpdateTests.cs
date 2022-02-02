#if INCLUDE_NETCODE_RUNTIME_TESTS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class ValueUpdateObject : NetworkBehaviour
    {
        public NetworkVariable<int> MyNetworkVariable = new NetworkVariable<int>();

        public ValueUpdateObject()
        {
            MyNetworkVariable.OnValueChanged = OnValueChanged;
        }

        public void OnValueChanged(int prevValue, int newValue)
        {
            Debug.Log($"OnValueChanged {prevValue} -> {newValue} at {MyNetworkVariable.TickRead}");
        }
    }

    public class ValueUpdateTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;
        private GameObject m_TestNetworkPrefab;
        private NetworkObject m_NetSpawnedObject;
        private List<NetworkObject> m_NetSpawnedObjectOnClient = new List<NetworkObject>();

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    var networkTransform = playerPrefab.AddComponent<HiddenVariableTest>();
                    m_TestNetworkPrefab = PreparePrefab();
                });
        }

        public GameObject PreparePrefab()
        {
            var prefabToSpawn = new GameObject("MyTestObject");
            var networkObjectPrefab = prefabToSpawn.AddComponent<NetworkObject>();
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObjectPrefab);
            prefabToSpawn.AddComponent<ValueUpdateObject>();

            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            }
            return prefabToSpawn;
        }

        public IEnumerator WaitForConnectedCount(int targetCount)
        {
            var endTime = Time.realtimeSinceStartup + 1.0;
            while (m_ServerNetworkManager.ConnectedClientsList.Count < targetCount && Time.realtimeSinceStartup < endTime)
            {
                yield return new WaitForSeconds(0.01f);
            }
        }

        public IEnumerator RefreshGameObects()
        {
            m_NetSpawnedObjectOnClient.Clear();

            foreach (var netMan in m_ClientNetworkManagers)
            {
                var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
                yield return MultiInstanceHelpers.Run(
                    MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                        x => x.NetworkObjectId == m_NetSpawnedObject.NetworkObjectId,
                        netMan,
                        serverClientPlayerResult));
                m_NetSpawnedObjectOnClient.Add(serverClientPlayerResult.Result);
            }
        }

        [UnityTest]
        public IEnumerator ValueUpdateTest()
        {
            Debug.Log("Running test");

            var spawnedObject = Object.Instantiate(m_TestNetworkPrefab);
            m_NetSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
            m_NetSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;
            yield return WaitForConnectedCount(NbClients);
            Debug.Log("Clients connected");

            // ==== Spawn object with ownership on one client
            var client = m_ServerNetworkManager.ConnectedClientsList[1];
            m_NetSpawnedObject.SpawnWithOwnership(client.ClientId);
            yield return RefreshGameObects();

            // set value to 4
            m_NetSpawnedObject.GetComponent<ValueUpdateObject>().MyNetworkVariable.Value = 4;
            yield return new WaitForSeconds(1.0f);

            for (var i = 0; i < m_NetSpawnedObjectOnClient.Count; i++)
            {
                Debug.Assert(m_NetSpawnedObjectOnClient[i].GetComponent<ValueUpdateObject>().MyNetworkVariable.Value == 4);
            }

            // despawn
            m_NetSpawnedObject.Despawn(false);
            yield return new WaitForSeconds(1.0f);

            // respawn
            m_NetSpawnedObject.SpawnWithOwnership(client.ClientId);
            yield return RefreshGameObects();

            // set value to 6
            m_NetSpawnedObject.GetComponent<ValueUpdateObject>().MyNetworkVariable.Value = 6;
            yield return new WaitForSeconds(1.0f);

            for (var i = 0; i < m_NetSpawnedObjectOnClient.Count; i++)
            {
                Debug.Assert(m_NetSpawnedObjectOnClient[i].GetComponent<ValueUpdateObject>().MyNetworkVariable.Value == 6);
            }
        }
    }

}
#endif
