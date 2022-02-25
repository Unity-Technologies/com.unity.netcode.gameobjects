// This integration test has too many issues with it and shouldn't have been merged into develop
#if DISABLE_UNTIL_FIXED
using System.Collections;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class TransformInterpolationObject : NetworkBehaviour
    {
        public bool CheckPosition;
        public bool IsMoving;
        public bool IsFixed;

        private void Update()
        {
            // Check the position of the nested object on the client
            if (CheckPosition)
            {
                if (transform.position.y < 0.0f || transform.position.y > 100.0f)
                {
                    Debug.LogError($"Interpolation failure. transform.position.y is {transform.position.y}. Should be between 0.0 and 100.0");
                }
            }

            // Move the nested object on the server
            if (IsMoving)
            {
                var y = Time.realtimeSinceStartup;
                while (y > 10.0f)
                {
                    y -= 10.0f;
                }

                // change the space between local and global every second
                GetComponent<NetworkTransform>().InLocalSpace = ((int)y % 2 == 0);

                transform.position = new Vector3(0.0f, y * 10, 0.0f);
            }

            // On the server, make sure to keep the parent object at a fixed position
            if (IsFixed)
            {
                transform.position = new Vector3(1000.0f, 1000.0f, 1000.0f);
            }
        }
    }

    public class TransformInterpolationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private ulong m_ClientId0;
        private GameObject m_PrefabToSpawn;

        private NetworkObject m_AsNetworkObject;
        private NetworkObject m_SpawnedObjectOnClient;

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("TransformTestObject");
            m_PrefabToSpawn.AddComponent<NetworkTransform>();
            m_PrefabToSpawn.AddComponent<TransformInterpolationObject>();
            base.OnServerAndClientsCreated();
        }

        // TODO: This is a temporary comment of these methods to get this test working in this branch.
        // TBD how/if this gets updated.

        //private IEnumerator RefreshNetworkObjects()
        //{
        //    var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
        //    yield return MultiInstanceHelpers.Run(
        //        MultiInstanceHelpers.GetNetworkObjectByRepresentation(
        //            x => x.NetworkObjectId == m_AsNetworkObject.NetworkObjectId,
        //            m_ClientNetworkManagers[0],
        //            serverClientPlayerResult));
        //    m_SpawnedObjectOnClient = serverClientPlayerResult.Result;

        //    // make sure the objects are set with the right network manager
        //    m_SpawnedObjectOnClient.NetworkManagerOwner = m_ClientNetworkManagers[0];
        //}

        //public GameObject PreparePrefab(Type type)
        //{
        //    var prefabToSpawn = new GameObject();
        //    prefabToSpawn.AddComponent(type);
        //    prefabToSpawn.AddComponent<NetworkTransform>();
        //    var networkObjectPrefab = prefabToSpawn.AddComponent<NetworkObject>();
        //    MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObjectPrefab);
        //    m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
        //    foreach (var clientNetworkManager in m_ClientNetworkManagers)
        //    {
        //        clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
        //    }
        //    return prefabToSpawn;
        //}

        [UnityTest]

        public IEnumerator TransformInterpolationTest()
        {
            m_ClientId0 = m_ClientNetworkManagers[0].LocalClientId;

            // create an object
            var baseObject = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            var spawnedObject = SpawnObject(m_PrefabToSpawn, m_ClientNetworkManagers[0]);
            //var spawnedObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            //var baseObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            //baseObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            //baseObject.GetComponent<NetworkObject>().Spawn();

            //m_AsNetworkObject = spawnedObject.GetComponent<NetworkObject>();
            //m_AsNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            m_AsNetworkObject = spawnedObject.GetComponent<NetworkObject>();
            m_AsNetworkObject.TrySetParent(baseObject);
            //m_AsNetworkObject.Spawn();
            yield return s_DefaultWaitForTick;

            //yield return RefreshNetworkObjects();

            m_AsNetworkObject.TrySetParent(baseObject);

            baseObject.GetComponent<TransformInterpolationObject>().IsFixed = true;
            spawnedObject.GetComponent<TransformInterpolationObject>().IsMoving = true;

            // Give two seconds for the object to settle
            // yield return new WaitForSeconds(2.0f);
            yield return s_DefaultWaitForTick;

            m_SpawnedObjectOnClient = s_GlobalNetworkObjects[m_ClientNetworkManagers[0].LocalClientId][m_AsNetworkObject.NetworkObjectId];

            m_SpawnedObjectOnClient.GetComponent<TransformInterpolationObject>().CheckPosition = true;

            // Test that interpolation works correctly for 10 seconds
            // Increasing this duration gives you the opportunity to go check in the Editor how the objects are setup
            // and how they move

            // [NSS]: Disabling this as this just delays the total time to complete all tests and the objects spawned have no
            // way to visually determine if it is working properly or not.  As well, integration tests should not require someone
            // to visually detect if it is working or not.
            //yield return new WaitForSeconds(10.0f);
        }
    }
}
#endif
