using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Spawning;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Random = System.Random;

namespace MLAPI.MultiprocessRuntimeTests
{
    public class NetworkVariablePerformanceTests : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 1;
        private const int k_MaxObjectstoSpawn = 10000;
        // todo move all of this static stuff to a self contained object. Could have the concept of a "client side test executor"?
        private static int s_TargetCount = 0;
        private List<NetworkObject> m_SpawnedObjects = new List<NetworkObject>();
        private static ObjectPool<GameObject> s_ObjectPool;


        public class OneNetVar : NetworkBehaviour
        {
            public static int nbInstances;
            public NetworkVariableInt oneInt;

            public void Init()
            {
                nbInstances++;
                Debug.Log("spawning!!!!!!");
            }

            private void OnDisable()
            {
                nbInstances--;
            }

            private void Update()
            {
                Debug.Log($"nb one net var instance!!!!! {nbInstances}");
            }
        }

        public static class NetworkVariablePerformanceTestsClient
        {
            public class CustomPrefabSpawnForTest1 : INetworkPrefabInstanceHandler
            {
                private GameObject m_PrefabToSpawn;
                private IObjectPool<GameObject> m_ObjectPool;

                public CustomPrefabSpawnForTest1(GameObject prefabToSpawn)
                {
                    m_PrefabToSpawn = prefabToSpawn;
                    m_ObjectPool = new ObjectPool<GameObject>(
                        createFunc: () => GameObject.Instantiate(m_PrefabToSpawn),
                        actionOnGet: objectToGet => objectToGet.SetActive(true),
                        actionOnRelease: objectToRelease => objectToRelease.SetActive(false),
                        defaultCapacity:k_MaxObjectstoSpawn
                    );
                }

                public NetworkObject HandleNetworkPrefabSpawn(ulong ownerClientId, Vector3 position, Quaternion rotation)
                {
                    var networkObject = m_ObjectPool.Get().GetComponent<NetworkObject>();
                    var r = new Random();
                    networkObject.transform.position = new Vector3(r.Next(-10, 10), r.Next(-10, 10), r.Next(-10, 10));
                    networkObject.transform.rotation = rotation;
                    SetupSpawnedObject(networkObject.gameObject); // adds custom component on spawn
                    return networkObject;
                }

                public void HandleNetworkPrefabDestroy(NetworkObject networkObject)
                {
                    networkObject.transform.position = Vector3.zero;
                    networkObject.transform.rotation = Quaternion.identity;
                    // TeardownSpawnedObject(networkObject.gameObject);
                    m_ObjectPool.Release(networkObject.gameObject);

                    // UnityEngine.Object.Destroy(networkObject.gameObject);
                }
            }

            public static void SetupClientForTest1(byte[] args)
            {
                s_TargetCount = BitConverter.ToInt32(args, 0);
                var prefabToSpawn = PrefabReference.Instance.referencedPrefab;
                var addedHandler = NetworkManager.Singleton.PrefabHandler.AddHandler(prefabToSpawn, new CustomPrefabSpawnForTest1(prefabToSpawn));
                if (!addedHandler)
                {
                    throw new Exception("Couldn't add Handler!");
                }

                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += UpdateClientForTest1;
                TestCoordinator.Instance.ClientDoneServerRpc();
            }

            private static void UpdateClientForTest1(float deltaTime)
            {
                var count = OneNetVar.nbInstances;
                if (count > 0)
                {
                    TestCoordinator.Instance.WriteTestResultsServerRpc(count);

                    // TestCoordinator.WriteResults(count);

                    if (count >= s_TargetCount)
                    {
                        // we got what we want, don't update results any longer
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= UpdateClientForTest1;
                    }
                }
            }

            public static void TeardownClientForTest1()
            {
                var prefabToSpawn = PrefabReference.Instance.referencedPrefab;
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefabToSpawn);
                s_TargetCount = 0;
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= UpdateClientForTest1;
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += WaitForAllOneNetVarToDespawn;
            }

            private static void WaitForAllOneNetVarToDespawn(float deltaTime)
            {
                var count = OneNetVar.nbInstances;
                Debug.Log($"waiting for despawn, count is {count} before client setting its done");
                if (count == 0)
                {
                    NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= WaitForAllOneNetVarToDespawn;
                    TestCoordinator.Instance.ClientDoneServerRpc();
                }
            }
        }



        [UnityTest, Order(1), Performance]
        public IEnumerator TestSpawn1()
        {
            // todo be able to run server code in a player and have client code execute from the editor
            yield return TestSpawningManyObjects(1);
        }

        [UnityTest, Order(2), Performance]
        public IEnumerator TestSpawn10()
        {
            yield return TestSpawningManyObjects(10);
        }

        [UnityTest, Order(3), Performance]
        public IEnumerator TestSpawn100()
        {
            yield return TestSpawningManyObjects(100);
        }

        [UnityTest, Order(4), Performance]
        public IEnumerator TestSpawn800()
        {
            yield return TestSpawningManyObjects(800);
        }

        [UnityTest, Order(5), Performance]
        public IEnumerator TestSpawn850()
        {
            yield return TestSpawningManyObjects(850);
        }

        [UnityTest, Order(6), Performance]
        public IEnumerator TestSpawn900()
        {
            yield return TestSpawningManyObjects(900);
        }

        [UnityTest, Order(7), Performance]
        public IEnumerator TestSpawn1000()
        {
            yield return TestSpawningManyObjects(1000);
        }

        [UnityTest, Order(7), Performance]
        public IEnumerator TestSpawn2500()
        {
            yield return TestSpawningManyObjects(2500);
        }

        [UnityTest, Order(8), Performance]
        public IEnumerator TestSpawn5000()
        {
            yield return TestSpawningManyObjects(5000);
        }

        [UnityTest, Order(9), Performance]
        public IEnumerator TestSpawn10000()
        {
            yield return TestSpawningManyObjects(10000); // to run these higher scale tests, we need NetworkManager's max
                                                         // receive events per tick to be set at a high enough value
        }

        [UnityTest, Order(10), Performance]
        public IEnumerator TestSpawn1000_2()
        {
            yield return TestSpawningManyObjects(1000);
        }

        [UnityTest, Order(11), Performance]
        public IEnumerator TestSpawn800_2()
        {
            yield return TestSpawningManyObjects(800);
        }

        [UnityTest, Order(12), Performance]
        public IEnumerator TestSpawn400_2()
        {
            yield return TestSpawningManyObjects(400);
        }

        private static void OnSceneLoadedInitSetupSuite(Scene scene, LoadSceneMode loadSceneMode)
        {
            var prefabToSpawn = PrefabReference.Instance.referencedPrefab;

            s_ObjectPool = new ObjectPool<GameObject>(
                createFunc: () => GameObject.Instantiate(prefabToSpawn),
                actionOnGet: objectToGet => objectToGet.SetActive(true),
                actionOnRelease: objectToRelease => objectToRelease.SetActive(false),
                defaultCapacity:k_MaxObjectstoSpawn
            );
        }

        [OneTimeSetUp]
        public override void SetupSuite()
        {
            base.SetupSuite();
            SceneManager.sceneLoaded += OnSceneLoadedInitSetupSuite;
        }

        public IEnumerator TestSpawningManyObjects(int nbObjects)
        {
            Assert.LessOrEqual(nbObjects, k_MaxObjectstoSpawn); // sanity check
            using (Measure.Scope($"Time Taken For Spawning {nbObjects} objects and getting report"))
            {
                // setup and wait
                // todo move this in base class? this could be a common API to setup players
                TestCoordinator.Instance.TriggerRpc(NetworkVariablePerformanceTestsClient.SetupClientForTest1, BitConverter.GetBytes(nbObjects));
                foreach (var clientId in TestCoordinator.AllClientIdExceptMine)
                {
                    // wait for the clients to be ready
                    yield return new WaitUntil(TestCoordinator.ConsumeClientIsDone(clientId));
                }

                // spawn prefabs for test

                // todo test with multiple workers
                for (int i = 0; i < nbObjects; i++)
                {
                    // var spawnedObject = GameObject.Instantiate(prefabToSpawn);
                    var spawnedObject = s_ObjectPool.Get();
                    var oneNetVar = SetupSpawnedObject(spawnedObject);
                    oneNetVar.NetworkObject.Spawn(destroyWithScene: true);
                    m_SpawnedObjects.Add(oneNetVar.NetworkObject);
                }

                // wait for spawn results coming from clients
                // for (int i = 0; i < NbWorkers; i++)
                {
                    float initialCount = float.NaN;
                    var startTime = Time.time;
                    var latestResult = float.NaN;
                    var allocated = new SampleGroup($"NbSpawnedPerFrame", SampleUnit.Undefined);
                    while (latestResult != nbObjects && Time.time - startTime < TestCoordinator.maxWaitTimeout)
                    {
                        // gather the object count over time to save it in the performance framework
                        yield return new WaitUntil(TestCoordinator.ResultIsSet(useTimeoutException: false));

                        foreach (var current in TestCoordinator.ConsumeCurrentResult())
                        {
                            latestResult = current.result;
                            Debug.Log($"got results, asserting, result is {current.result} from key {current.clientId}");
                            if (float.IsNaN(initialCount))
                            {
                                initialCount = current.result;
                            }

                            Measure.Custom(allocated, current.result);
                        }
                    }

                    Assert.AreEqual(nbObjects, initialCount);
                }
            }

            Debug.Log($"finished with test for {nbObjects} objects");
        }

        [UnityTearDown]
        public IEnumerator UnityTeardown()
        {
            // Teardown();
            TestCoordinator.Instance.TriggerRpc(NetworkVariablePerformanceTestsClient.TeardownClientForTest1);
            foreach (var spawnedObject in m_SpawnedObjects)
            {
                spawnedObject.Despawn();
                s_ObjectPool.Release(spawnedObject.gameObject);
            }

            m_SpawnedObjects.Clear();

            // wait for the clients to be done with their teardown
            foreach (var clientId in TestCoordinator.AllClientIdExceptMine)
            {
                yield return new WaitUntil(TestCoordinator.ConsumeClientIsDone(clientId, useTimeoutException:false));
            }
        }

        [OneTimeTearDown]
        public override void TeardownSuite()
        {
            base.TeardownSuite();
            SceneManager.sceneLoaded -= OnSceneLoadedInitSetupSuite;
        }

        private static OneNetVar SetupSpawnedObject(GameObject spawnedObject)
        {
            spawnedObject.name = "ReplicatedObjectTest1";
            var oneNetVar = spawnedObject.AddComponent<OneNetVar>();
            oneNetVar.Init();
            return oneNetVar;
        }

        // private static void TeardownSpawnedObject(GameObject spawnedObject)
        // {
        //     GameObject.Destroy(spawnedObject.GetComponent<OneNetVar>());
        // }
    }
}
