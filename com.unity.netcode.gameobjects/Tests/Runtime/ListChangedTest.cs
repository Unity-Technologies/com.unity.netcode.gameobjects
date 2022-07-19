using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkListChangedTestComponent : NetworkBehaviour
    {

    }

    public class ListChangedObject : NetworkBehaviour
    {
        public static List<ListChangedObject> ClientTargetedNetworkObjects = new List<ListChangedObject>();
        public static ulong ClientIdToTarget;

        public static NetworkObject GetNetworkObjectById(ulong networkObjectId)
        {
            foreach (var entry in ClientTargetedNetworkObjects)
            {
                if (entry.NetworkObjectId == networkObjectId)
                {
                    return entry.NetworkObject;
                }
            }
            return null;
        }

        public NetworkList<int> MyNetworkList = new NetworkList<int>();

        public override void OnNetworkSpawn()
        {
            MyNetworkList.OnListChanged += Changed;

            if (NetworkManager.LocalClientId == ClientIdToTarget)
            {
                ClientTargetedNetworkObjects.Add(this);
            }
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (ClientTargetedNetworkObjects.Contains(this))
            {
                ClientTargetedNetworkObjects.Remove(this);
            }
            base.OnNetworkDespawn();
        }

        public void Changed(NetworkListEvent<int> listEvent)
        {
            Debug.Log($"listEvent.Type is {listEvent.Type}");
        }
    }

    public class NetworkListChangedTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private ulong m_ClientId0;
        private GameObject m_PrefabToSpawn;

        private NetworkObject m_NetSpawnedObject1;
        private NetworkObject m_NetSpawnedObject2;
        private NetworkObject m_NetSpawnedObject3;
        private NetworkObject m_Object1OnClient0;
        private NetworkObject m_Object2OnClient0;
        private NetworkObject m_Object3OnClient0;

        protected override void OnCreatePlayerPrefab()
        {
            var networkTransform = m_PlayerPrefab.AddComponent<NetworkListChangedTestComponent>();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("ListChangedObject");
            m_PrefabToSpawn.AddComponent<ListChangedObject>();
        }

        private bool RefreshNetworkObjects()
        {
            m_Object1OnClient0 = ListChangedObject.GetNetworkObjectById(m_NetSpawnedObject1.NetworkObjectId);
            m_Object2OnClient0 = ListChangedObject.GetNetworkObjectById(m_NetSpawnedObject2.NetworkObjectId);
            m_Object3OnClient0 = ListChangedObject.GetNetworkObjectById(m_NetSpawnedObject3.NetworkObjectId);
            if (m_Object1OnClient0 == null || m_Object2OnClient0 == null || m_Object3OnClient0 == null)
            {
                return false;
            }
            Assert.True(m_Object1OnClient0.NetworkManagerOwner == m_ClientNetworkManagers[0]);
            Assert.True(m_Object2OnClient0.NetworkManagerOwner == m_ClientNetworkManagers[0]);
            Assert.True(m_Object3OnClient0.NetworkManagerOwner == m_ClientNetworkManagers[0]);
            return true;
        }


        [UnityTest]
        public IEnumerator NetworkListChangedTest()
        {
            m_ClientId0 = m_ClientNetworkManagers[0].LocalClientId;
            ListChangedObject.ClientTargetedNetworkObjects.Clear();
            ListChangedObject.ClientIdToTarget = m_ClientId0;

            // create 3 objects
            var spawnedObject1 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            var spawnedObject2 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            var spawnedObject3 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            m_NetSpawnedObject1 = spawnedObject1.GetComponent<NetworkObject>();
            m_NetSpawnedObject2 = spawnedObject2.GetComponent<NetworkObject>();
            m_NetSpawnedObject3 = spawnedObject3.GetComponent<NetworkObject>();

            // get the NetworkObject on a client instance
            yield return WaitForConditionOrTimeOut(RefreshNetworkObjects);
            AssertOnTimeout($"Could not refresh all NetworkObjects!");

            m_NetSpawnedObject1.GetComponent<ListChangedObject>().MyNetworkList.Add(42);
            m_NetSpawnedObject1.GetComponent<ListChangedObject>().MyNetworkList[0] = 44;

            // todo
        }
    }
}
