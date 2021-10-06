using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class HiddenVariableTest : NetworkBehaviour
    {
    }

    public class HiddenVariableObject : NetworkBehaviour
    {
        public NetworkVariable<int> MyNetworkVariable = new NetworkVariable<int>();
        public NetworkList<int> MyNetworkList = new NetworkList<int>();

        public static Dictionary<ulong, int> ValueOnClient = new Dictionary<ulong, int>();
        public static int ExpectedSize = 0;
        public static int SpawnCount = 0;

        public override void OnNetworkSpawn()
        {
            Debug.Log($"{nameof(HiddenVariableObject)}.{nameof(OnNetworkSpawn)}() with value {MyNetworkVariable.Value}");

            MyNetworkVariable.OnValueChanged += Changed;
            MyNetworkList.OnListChanged += ListChanged;
            SpawnCount++;

            base.OnNetworkSpawn();
        }

        public void Changed(int before, int after)
        {
            Debug.Log($"Value changed from {before} to {after} on {NetworkManager.LocalClientId}");
            ValueOnClient[NetworkManager.LocalClientId] = after;
        }
        public void ListChanged(NetworkListEvent<int> listEvent)
        {
            Debug.Log($"ListEvent received: type {listEvent.Type}, index {listEvent.Index}, value {listEvent.Value}");
            Debug.Assert(ExpectedSize == MyNetworkList.Count);
        }
    }

    public class HiddenVariableTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 4;

        private NetworkObject m_NetSpawnedObject;
        private List<NetworkObject> m_NetSpawnedObjectOnClient = new List<NetworkObject>();
        private GameObject m_TestNetworkPrefab;

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
            prefabToSpawn.AddComponent<HiddenVariableObject>();

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

        public IEnumerator WaitForSpawnCount(int targetCount)
        {
            var endTime = Time.realtimeSinceStartup + 1.0;
            while (HiddenVariableObject.SpawnCount != targetCount &&
                   Time.realtimeSinceStartup < endTime)
            {
                yield return new WaitForSeconds(0.01f);
            }
        }

        public void VerifyLists()
        {
            NetworkList<int> prev = null;
            int numComparison = 0;

            // for all the instances of NetworkList
            foreach (var gameObject in m_NetSpawnedObjectOnClient)
            {
                // this skips despawned/hidden objects
                if (gameObject != null)
                {
                    // if we've seen another one before
                    if (prev != null)
                    {
                        var curr = gameObject.GetComponent<HiddenVariableObject>().MyNetworkList;

                        // check that the two lists are identical
                        Debug.Assert(curr.Count == prev.Count);
                        for (int index = 0; index < curr.Count; index++)
                        {
                            Debug.Assert(curr[index] == prev[index]);
                        }
                        numComparison++;
                    }
                    // store the list
                    prev = gameObject.GetComponent<HiddenVariableObject>().MyNetworkList;
                }
            }
            Debug.Log($"{numComparison} comparisons done.");
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
        public IEnumerator HiddenVariableTest()
        {
            HiddenVariableObject.SpawnCount = 0;
            HiddenVariableObject.ValueOnClient.Clear();
            HiddenVariableObject.ExpectedSize = 0;
            HiddenVariableObject.SpawnCount = 0;

            Debug.Log("Running test");

            var spawnedObject = Object.Instantiate(m_TestNetworkPrefab);
            m_NetSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
            m_NetSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;
            yield return WaitForConnectedCount(NbClients);
            Debug.Log("Clients connected");

            // ==== Spawn object with ownership on one client
            var client = m_ServerNetworkManager.ConnectedClientsList[1];
            var otherClient = m_ServerNetworkManager.ConnectedClientsList[2];
            m_NetSpawnedObject.SpawnWithOwnership(client.ClientId);

            yield return RefreshGameObects();

            // === Check spawn occured
            yield return WaitForSpawnCount(NbClients + 1);
            Debug.Assert(HiddenVariableObject.SpawnCount == NbClients + 1);
            Debug.Log("Objects spawned");

            // ==== Set the NetworkVariable value to 2
            HiddenVariableObject.ExpectedSize = 1;
            HiddenVariableObject.SpawnCount = 0;

            m_NetSpawnedObject.GetComponent<HiddenVariableObject>().MyNetworkVariable.Value = 2;
            m_NetSpawnedObject.GetComponent<HiddenVariableObject>().MyNetworkList.Add(2);

            yield return new WaitForSeconds(1.0f);

            foreach (var id in m_ServerNetworkManager.ConnectedClientsIds)
            {
                Debug.Assert(HiddenVariableObject.ValueOnClient[id] == 2);
            }

            VerifyLists();

            Debug.Log("Value changed");

            // ==== Hide our object to a different client
            HiddenVariableObject.ExpectedSize = 2;
            m_NetSpawnedObject.NetworkHide(otherClient.ClientId);

            // ==== Change the NetworkVariable value
            // we should get one less notification of value changing and no errors or exception
            m_NetSpawnedObject.GetComponent<HiddenVariableObject>().MyNetworkVariable.Value = 3;
            m_NetSpawnedObject.GetComponent<HiddenVariableObject>().MyNetworkList.Add(3);

            yield return new WaitForSeconds(1.0f);
            foreach (var id in m_ServerNetworkManager.ConnectedClientsIds)
            {
                if (id != otherClient.ClientId)
                {
                    Debug.Assert(HiddenVariableObject.ValueOnClient[id] == 3);
                }
            }

            VerifyLists();
            Debug.Log("Values changed");

            // ==== Show our object again to this client
            HiddenVariableObject.ExpectedSize = 3;
            m_NetSpawnedObject.NetworkShow(otherClient.ClientId);

            // ==== Wait for object to be spawned
            yield return WaitForSpawnCount(1);
            Debug.Assert(HiddenVariableObject.SpawnCount == 1);
            Debug.Log("Object spawned");

            // ==== We need a refresh for the newly re-spawned object
            yield return RefreshGameObects();

            // ==== Change the NetworkVariable value
            // we should get all notifications of value changing and no errors or exception
            m_NetSpawnedObject.GetComponent<HiddenVariableObject>().MyNetworkVariable.Value = 4;
            m_NetSpawnedObject.GetComponent<HiddenVariableObject>().MyNetworkList.Add(4);

            yield return new WaitForSeconds(1.0f);

            foreach (var id in m_ServerNetworkManager.ConnectedClientsIds)
            {
                Debug.Assert(HiddenVariableObject.ValueOnClient[id] == 4);
            }

            VerifyLists();
            Debug.Log("Values changed");

            // ==== Hide our object to that different client again, and then destroy it
            m_NetSpawnedObject.NetworkHide(otherClient.ClientId);
            yield return new WaitForSeconds(0.2f);
            m_NetSpawnedObject.Despawn();
            yield return new WaitForSeconds(0.2f);
        }
    }
}
