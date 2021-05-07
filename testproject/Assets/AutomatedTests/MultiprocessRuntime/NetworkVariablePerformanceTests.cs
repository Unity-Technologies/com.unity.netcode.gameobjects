using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Spawning;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MLAPI.MultiprocessRuntimeTests
{
    public class NetworkVariablePerformanceTests : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 1;

        public class CustomPrefabSpawnForTest1 : INetworkPrefabInstanceHandler
        {
            private GameObject m_PrefabToSpawn;

            public CustomPrefabSpawnForTest1(GameObject prefabToSpawn)
            {
                m_PrefabToSpawn = prefabToSpawn;
            }
            public NetworkObject HandleNetworkPrefabSpawn(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                var networkObject = GameObject.Instantiate(m_PrefabToSpawn).GetComponent<NetworkObject>();
                networkObject.transform.position = position;
                networkObject.transform.rotation = rotation;
                SetupSpawnedObject(networkObject.gameObject); // adds custom component on spawn
                return networkObject;
            }

            public void HandleNetworkPrefabDestroy(NetworkObject networkObject)
            {
                throw new NotImplementedException();
            }
        }

        public static void SetupClientForTest1()
        {
            //todo
            //TestCoordinator.WriteResults(Time.time);
            var prefabToSpawn = NetworkManager.Singleton.gameObject.GetComponent<PrefabReference>().referencedPrefab;
            var addedHandler = NetworkManager.Singleton.PrefabHandler.AddHandler(prefabToSpawn, new CustomPrefabSpawnForTest1(prefabToSpawn));
            Assert.True(addedHandler);
            var callbacks = NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>();
            callbacks.OnUpdate += UpdateClientForTest1;
            TestCoordinator.WriteResults(float.PositiveInfinity);
        }

        public static void TeardownClientForTest1()
        {
            var callbacks = NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>();
            callbacks.OnUpdate -= UpdateClientForTest1;
            var prefabToSpawn = NetworkManager.Singleton.gameObject.GetComponent<PrefabReference>().referencedPrefab;
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefabToSpawn);
            TestCoordinator.WriteResults(float.PositiveInfinity);
        }

        private static void UpdateClientForTest1(float deltaTime)
        {
            // var count = NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count;
            var count = OneNetVar.nbInstances;
            // Debug.Log($"number of spawned objects {count}");
            if (count > 0)
            {
                TestCoordinator.WriteResults(count); // multiple writes one update after the other might lose some sends?
            }
        }


        public class OneNetVar : NetworkBehaviour
        {
            public static int nbInstances;
            public NetworkVariableInt oneInt;

            private void Start()
            {
                nbInstances++;
            }

            private void OnDestroy()
            {
                nbInstances--;
            }
        }

        private List<NetworkObject> m_SpawnedObjects = new List<NetworkObject>();

        // [UnitySetUp]
        // public override IEnumerator Setup()
        // {
        //     yield return base.Setup();
        // }
        //
        // [TearDown]
        // public override void Teardown()
        // {
        //     base.Teardown();
        // }

        [UnityTest, Order(1)]
        public IEnumerator TestSpawn1Object()
        {
            yield return TestSpawningManyObjects(1);
        }

        [UnityTest, Order(2)]
        public IEnumerator TestSpawn10Object()
        {
            yield return TestSpawningManyObjects(10);
        }

        [UnityTest, Order(3)]
        public IEnumerator TestSpawn100Object()
        {
            yield return TestSpawningManyObjects(100);
        }

        [UnityTest, Order(4)]
        public IEnumerator TestSpawn1000Object()
        {
            yield return TestSpawningManyObjects(1000);
        }

        [UnityTest, Order(5)]
        public IEnumerator TestSpawn4000Object()
        {
            yield return TestSpawningManyObjects(4000);
        }

        [UnityTest, Order(6)]
        public IEnumerator TestSpawn5000Object()
        {
            yield return TestSpawningManyObjects(5000);
        }

        [UnityTest, Order(7)]
        public IEnumerator TestSpawn6000Object()
        {
            yield return TestSpawningManyObjects(6000);
        }

        public IEnumerator TestSpawningManyObjects(int nbObjects)
        {
            TestCoordinator.Instance.TriggerRpc(TestCoordinator.GetMethodInfo(SetupClientForTest1));
            for (int i = 0; i < NbWorkers; i++)
            {
                // wait for the clients to be ready
                yield return new WaitUntil(TestCoordinator.ResultIsSet());
            }

            var prefabToSpawn = NetworkManager.Singleton.gameObject.GetComponent<PrefabReference>().referencedPrefab;

            for (int i = 0; i < nbObjects; i++)
            {
                var spawnedObject = GameObject.Instantiate(prefabToSpawn);
                var oneNetVar = SetupSpawnedObject(spawnedObject);
                oneNetVar.NetworkObject.Spawn(destroyWithScene: true);
                m_SpawnedObjects.Add(oneNetVar.NetworkObject);
            }

            try
            {
                // yield return new WaitForSeconds(1000);

                for (int i = 0; i < NbWorkers; i++) // wait and test for the two clients
                {
                    yield return new WaitUntil(TestCoordinator.ResultIsSet());
                    var resKey = TestCoordinator.Instance.CurrentClientIdWithResults;

                    Debug.Log($"got results, asserting, result is {TestCoordinator.GetCurrentResult()} from key {resKey}");
                    Assert.AreEqual(nbObjects, TestCoordinator.GetCurrentResult());
                }
            }
            finally
            {
                TestCoordinator.Instance.TriggerRpc(TestCoordinator.GetMethodInfo(TeardownClientForTest1));
                foreach (var spawnedObject in m_SpawnedObjects)
                {
                    spawnedObject.Despawn();
                    GameObject.Destroy(spawnedObject);
                }
                m_SpawnedObjects.Clear();

            }
            yield return new WaitUntil(TestCoordinator.ResultIsSet()); // wait for teardown done client side

        }

        private static OneNetVar SetupSpawnedObject(GameObject spawnedObject)
        {
            spawnedObject.name = "ReplicatedObjectTest1";
            var oneNetVar = spawnedObject.AddComponent<OneNetVar>();
            return oneNetVar;
        }
    }
}
