using System;
using System.Collections;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class TransformInterpolationTest : NetworkBehaviour
    {

    }

    public class TransformInterpolationObject : NetworkBehaviour
    {
        public bool CheckPosition;
        private void Start()
        {
            Debug.Log($"started {IsServer}");
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"spawned {IsServer}");
        }

        private void Update()
        {
            if (CheckPosition)
            {
                if (transform.position.y < 0.0f || transform.position.y > 100.0f)
                {
                    Debug.LogWarning($"Interpolation failure. transform.position.y is {transform.position.y}. Should be between 0.0 and 100.0");
                }

                // todo: Why is this printing ridiculous values for transform.localPosition ?
                Debug.Log($"transform.position.y is {transform.position.y}. transform.localPosition.y is {transform.localPosition.y}.");
            }
        }
    }

    public class TransformInterpolationTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 1;

        private ulong m_ClientId0;
        private GameObject m_PrefabToSpawn;

        private NetworkObject m_AsNetworkObject;
        private NetworkTransform m_AsNetworkTransform;

        private NetworkObject m_Object1OnClient;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    m_PrefabToSpawn = PreparePrefab(typeof(TransformInterpolationObject));
                });
        }

        private IEnumerator RefreshNetworkObjects()
        {
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                    x => x.NetworkObjectId == m_AsNetworkObject.NetworkObjectId,
                    m_ClientNetworkManagers[0],
                    serverClientPlayerResult));
            m_Object1OnClient = serverClientPlayerResult.Result;
            yield return MultiInstanceHelpers.Run(
                MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                    x => x.NetworkObjectId == m_AsNetworkObject.NetworkObjectId,
                    m_ClientNetworkManagers[0],
                    serverClientPlayerResult));

            // make sure the objects are set with the right network manager
            m_Object1OnClient.NetworkManagerOwner = m_ClientNetworkManagers[0];
        }

        public GameObject PreparePrefab(Type type)
        {
            var prefabToSpawn = new GameObject();
            prefabToSpawn.AddComponent(type);
            prefabToSpawn.AddComponent<NetworkTransform>();
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
        public IEnumerator TransformInterpolationTest()
        {
            m_ClientId0 = m_ClientNetworkManagers[0].LocalClientId;

            // create an object
            var spawnedObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            var baseObject = UnityEngine.Object.Instantiate(m_PrefabToSpawn);
            baseObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            baseObject.GetComponent<NetworkObject>().Spawn();

            m_AsNetworkObject = spawnedObject.GetComponent<NetworkObject>();
            m_AsNetworkTransform = spawnedObject.GetComponent<NetworkTransform>();
            m_AsNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;

            m_AsNetworkObject.Spawn();

            baseObject.transform.position = new Vector3(1000.0f, 1000.0f, 1000.0f);
            spawnedObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            yield return RefreshNetworkObjects();

            m_Object1OnClient.GetComponent<TransformInterpolationObject>().CheckPosition = true;

            spawnedObject.transform.parent = baseObject.transform;

            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForSeconds(0.01f);

                if ((i != 0) && (i % 10 == 0))
                {
                    m_AsNetworkTransform.InLocalSpace = !m_AsNetworkTransform.InLocalSpace;
                }

                spawnedObject.transform.position = new Vector3(0.0f, i, 0.0f);
            }
        }
    }
}
