using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkShowHideTest : NetworkBehaviour
    {

    }

    public class ShowHideObject : NetworkBehaviour
    {
        public NetworkVariable<int> MyNetworkVariable;

        private void Start()
        {
            MyNetworkVariable = new NetworkVariable<int>();
            MyNetworkVariable.OnValueChanged += Changed;
        }

        public void Changed(int before, int after)
        {
            Debug.Log($"Value changed from {before} to {after}");
        }

    }

    public class NetworkShowHideTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        private ulong m_ClientId0;
        private GameObject m_PrefabToSpawn;

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
                    m_PrefabToSpawn = PreparePrefab(typeof(ShowHideObject));
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

        // Check that the first client see them, or not, as expected
        private IEnumerator CheckVisible(bool target)
        {
            int count = 0;
            do
            {
                yield return new WaitForSeconds(0.1f);
                count++;

                if (count > 20)
                {
                    // timeout waiting for object to reach the expect visibility
                    Assert.Fail("timeout waiting for object to reach the expect visibility");
                    break;
                }
            } while (m_NetSpawnedObject1.IsNetworkVisibleTo(m_ClientId0) != target ||
                     m_NetSpawnedObject2.IsNetworkVisibleTo(m_ClientId0) != target ||
                     m_NetSpawnedObject3.IsNetworkVisibleTo(m_ClientId0) != target ||
                     m_Object1OnClient0.IsSpawned != target ||
                     m_Object2OnClient0.IsSpawned != target ||
                     m_Object3OnClient0.IsSpawned != target
                     );

            Debug.Assert(m_NetSpawnedObject1.IsNetworkVisibleTo(m_ClientId0) == target);
            Debug.Assert(m_NetSpawnedObject2.IsNetworkVisibleTo(m_ClientId0) == target);
            Debug.Assert(m_NetSpawnedObject3.IsNetworkVisibleTo(m_ClientId0) == target);

            Debug.Assert(m_Object1OnClient0.IsSpawned == target);
            Debug.Assert(m_Object2OnClient0.IsSpawned == target);
            Debug.Assert(m_Object3OnClient0.IsSpawned == target);
        }

        // Set the 3 objects visibility
        private void Show(bool individually, bool visibility)
        {
            if (individually)
            {
                if (!visibility)
                {
                    m_NetSpawnedObject1.NetworkHide(m_ClientId0);
                    m_NetSpawnedObject2.NetworkHide(m_ClientId0);
                    m_NetSpawnedObject3.NetworkHide(m_ClientId0);
                }
                else
                {
                    m_NetSpawnedObject1.NetworkShow(m_ClientId0);
                    m_NetSpawnedObject2.NetworkShow(m_ClientId0);
                    m_NetSpawnedObject3.NetworkShow(m_ClientId0);
                }
            }
            else
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
        }

        private IEnumerator RefreshNetworkObjects()
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


            var spawnedObject1 = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            var spawnedObject2 = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            var spawnedObject3 = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            m_NetSpawnedObject1 = spawnedObject1.GetComponent<NetworkObject>();
            m_NetSpawnedObject2 = spawnedObject2.GetComponent<NetworkObject>();
            m_NetSpawnedObject3 = spawnedObject3.GetComponent<NetworkObject>();
            m_NetSpawnedObject1.NetworkManagerOwner = m_ServerNetworkManager;
            m_NetSpawnedObject2.NetworkManagerOwner = m_ServerNetworkManager;
            m_NetSpawnedObject3.NetworkManagerOwner = m_ServerNetworkManager;
            m_NetSpawnedObject1.Spawn();
            m_NetSpawnedObject2.Spawn();
            m_NetSpawnedObject3.Spawn();


            for (int mode = 0; mode < 2; mode++)
            {
                // get the NetworkObject on a client instance
                yield return RefreshNetworkObjects();

                // check object start visible
                yield return CheckVisible(true);

                // hide them on one client
                Show(mode == 0, false);

                yield return new WaitForSeconds(1.0f);

                m_NetSpawnedObject1.GetComponent<ShowHideObject>().MyNetworkVariable.Value = 3;

                yield return new WaitForSeconds(1.0f);

                // verify they got hidden
                yield return CheckVisible(false);

                // show them to that client
                Show(mode == 0, true);
                yield return RefreshNetworkObjects();

                // verify they become visible
                yield return CheckVisible(true);
            }
        }
    }
}
