using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Spawning;
using NUnit.Framework;
using Unity.PerformanceTesting;
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
                UnityEngine.Object.Destroy(networkObject.gameObject);
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
            TestCoordinator.Instance.ClientDoneServerRpc();
        }

        public static void TeardownClientForTest1()
        {
            var callbacks = NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>();
            callbacks.OnUpdate -= UpdateClientForTest1;
            var prefabToSpawn = NetworkManager.Singleton.gameObject.GetComponent<PrefabReference>().referencedPrefab;
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefabToSpawn);
            TestCoordinator.Instance.ClientDoneServerRpc();
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

        [UnityTest, Order(1), Performance]
        public IEnumerator TestSpawn1Object()
        {
            // todo be able to run server code in a player and have client code execute from the editor
            yield return TestSpawningManyObjects(1);
        }

        [UnityTest, Order(2), Performance]
        public IEnumerator TestSpawn10Object()
        {
            yield return TestSpawningManyObjects(10);
        }

        [UnityTest, Order(3), Performance]
        public IEnumerator TestSpawn100Object()
        {
            yield return TestSpawningManyObjects(100);
        }

        [UnityTest, Order(4), Performance]
        public IEnumerator TestSpawn800Object()
        {
            yield return TestSpawningManyObjects(800);
        }

        [UnityTest, Order(5), Performance]
        public IEnumerator TestSpawn850Object()
        {
            yield return TestSpawningManyObjects(850);
        }

        [UnityTest, Order(6), Performance]
        public IEnumerator TestSpawn900Object()
        {
            yield return TestSpawningManyObjects(900);
        }

        [UnityTest, Order(7), Performance]
        public IEnumerator TestSpawn1000Object()
        {
            yield return TestSpawningManyObjects(1000);
        }

        public IEnumerator TestSpawningManyObjects(int nbObjects)
        {
            TestCoordinator.Instance.TriggerRpc(TestCoordinator.GetMethodInfo(SetupClientForTest1));
            foreach (var clientId in TestCoordinator.AllClientIdExceptMine)
            {
                // wait for the clients to be ready
                yield return new WaitUntil(TestCoordinator.ClientIsDone(clientId));
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

                for (int i = 0; i < NbWorkers; i++) // wait and test for the two clients
                {
                    yield return new WaitUntil(TestCoordinator.ResultIsSet());
                    var resKey = TestCoordinator.Instance.CurrentClientIdWithResults;

                    Debug.Log($"got results, asserting, result is {TestCoordinator.GetCurrentResult()} from key {resKey}");
                    Assert.AreEqual(nbObjects, TestCoordinator.GetCurrentResult());

                    // // todo
                    // while (result != nbObjects && !timeout)
                    // {
                    //     yield return new WaitUntil(TestCoordinator.ResultIsSet());
                    //
                    //     var allocated = new SampleGroup($"NbSpawnedPerFrame-{resKey}", SampleUnit.Byte);
                    //     Measure.Custom(allocated, TestCoordinator.GetCurrentResult());
                    // }
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
            foreach (var clientId in TestCoordinator.AllClientIdExceptMine)
            {
                // wait for the clients to be ready
                yield return new WaitUntil(TestCoordinator.ClientIsDone(clientId));
            }
        }

        private static OneNetVar SetupSpawnedObject(GameObject spawnedObject)
        {
            spawnedObject.name = "ReplicatedObjectTest1";
            var oneNetVar = spawnedObject.AddComponent<OneNetVar>();
            return oneNetVar;
        }
    }
}
