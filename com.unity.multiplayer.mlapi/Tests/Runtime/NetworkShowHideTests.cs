using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;

public class NetworkShowHideTest : NetworkBehaviour
{

}

public class ShowHideObject : NetworkBehaviour
{

}

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkShowHideTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        private ulong m_ClientId0;

        private NetworkObject m_NetSpawnedObject1;
        private NetworkObject m_NetSpawnedObject2;
        private NetworkObject m_NetSpawnedObject3;
        private NetworkObject m_Object1OnClient0;
        private NetworkObject m_Object2OnClient0;
        private NetworkObject m_Object3OnClient0;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    var networkTransform = playerPrefab.AddComponent<NetworkShowHideTest>();
                });
        }

        public GameObject PreparePrefab(Type type)
        {
            var prefabToSpawn = new GameObject();
            prefabToSpawn.AddComponent(type);
            var networkObjectPrefab = prefabToSpawn.AddComponent<NetworkObject>();
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObjectPrefab);
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            }
            return prefabToSpawn;
        }

        // Check that the first client see them, or not, as expected
        private void checkVisible(bool target)
        {
            Debug.Assert(m_NetSpawnedObject1.IsNetworkVisibleTo(m_ClientId0) == target);
            Debug.Assert(m_NetSpawnedObject2.IsNetworkVisibleTo(m_ClientId0) == target);
            Debug.Assert(m_NetSpawnedObject3.IsNetworkVisibleTo(m_ClientId0) == target);

            Debug.Assert(m_Object1OnClient0.IsSpawned == target);
            Debug.Assert(m_Object2OnClient0.IsSpawned == target);
            Debug.Assert(m_Object3OnClient0.IsSpawned == target);
        }

        // Set the 3 objects visibility
        private void show(bool visibility)
        {
            var list = new List<NetworkObject>();
            list.Add(m_NetSpawnedObject1);
            list.Add(m_NetSpawnedObject2);
            list.Add(m_NetSpawnedObject3);

            if (!visibility)
            {
                NetworkObject.NetworkHide(list, m_ClientId0);
            }
            else
            {
                NetworkObject.NetworkShow(list, m_ClientId0);
            }
        }

        private IEnumerator refreshNetworkObjects()
        {
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                    x => x.NetworkObjectId == m_NetSpawnedObject1.NetworkObjectId,
                    m_ClientNetworkManagers[0],
                    serverClientPlayerResult));
            m_Object1OnClient0 = serverClientPlayerResult.Result;
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                    x => x.NetworkObjectId == m_NetSpawnedObject2.NetworkObjectId,
                    m_ClientNetworkManagers[0],
                    serverClientPlayerResult));
            m_Object2OnClient0 = serverClientPlayerResult.Result;
            serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                    x => x.NetworkObjectId == m_NetSpawnedObject3.NetworkObjectId,
                    m_ClientNetworkManagers[0],
                    serverClientPlayerResult));
            m_Object3OnClient0 = serverClientPlayerResult.Result;

            // make sure the objects are set with the right network manager
            m_Object1OnClient0.NetworkManagerOwner = m_ClientNetworkManagers[0];
            m_Object2OnClient0.NetworkManagerOwner = m_ClientNetworkManagers[0];
            m_Object3OnClient0.NetworkManagerOwner = m_ClientNetworkManagers[0];
        }


        [UnityTest]
        public IEnumerator NetworkShowHideTest()
        {
            m_ClientId0 = m_ClientNetworkManagers[0].LocalClientId;

            // create 3 objects

            var prefabToSpawn = PreparePrefab(typeof(ShowHideObject));
            var spawnedObject1 = UnityEngine.Object.Instantiate(prefabToSpawn);
            var spawnedObject2 = UnityEngine.Object.Instantiate(prefabToSpawn);
            var spawnedObject3 = UnityEngine.Object.Instantiate(prefabToSpawn);
            m_NetSpawnedObject1 = spawnedObject1.GetComponent<NetworkObject>();
            m_NetSpawnedObject2 = spawnedObject2.GetComponent<NetworkObject>();
            m_NetSpawnedObject3 = spawnedObject3.GetComponent<NetworkObject>();
            m_NetSpawnedObject1.NetworkManagerOwner = m_ServerNetworkManager;
            m_NetSpawnedObject2.NetworkManagerOwner = m_ServerNetworkManager;
            m_NetSpawnedObject3.NetworkManagerOwner = m_ServerNetworkManager;
            m_NetSpawnedObject1.Spawn();
            m_NetSpawnedObject2.Spawn();
            m_NetSpawnedObject3.Spawn();

            yield return new WaitForSeconds(0.1f);

            // get the NetworkObject on a client instance
            yield return refreshNetworkObjects();

            // check object start visible
            checkVisible(true);

            // hide them on one client
            show(false);
            yield return new WaitForSeconds(0.1f);

            // verify they got hidden
            checkVisible(false);

            // show them to that client
            show(true);
            yield return new WaitForSeconds(0.1f);
            yield return refreshNetworkObjects();

            // verify they become visible
            checkVisible(true);
        }
    }
}
