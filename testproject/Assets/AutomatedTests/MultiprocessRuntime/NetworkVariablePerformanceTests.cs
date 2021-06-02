using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.NetworkVariable;
using MLAPI.Spawning;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace MLAPI.MultiprocessRuntimeTests
{
    // todo profile all this
    public class NetworkVariablePerformanceTests : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 1;
        private const int k_MaxObjectstoSpawn = 100000;
        // todo move all of this static stuff to a self contained object. Could have the concept of a "client side test executor"?
        private static int s_TargetCount = 0;
        private List<NetworkObject> m_SpawnedObjects = new List<NetworkObject>();
        private static GameObjectPool s_ObjectPool;

        public NetworkVariablePerformanceTests()
        {

        }


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
                private GameObjectPool m_ObjectPool;

                public CustomPrefabSpawnForTest1(GameObject prefabToSpawn)
                {
                    m_PrefabToSpawn = prefabToSpawn;
                    m_ObjectPool = new GameObjectPool();
                    m_ObjectPool.Init(k_MaxObjectstoSpawn, m_PrefabToSpawn);
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
                TestCoordinator.Instance.ClientFinishedServerRpc();
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
                    TestCoordinator.Instance.ClientFinishedServerRpc();
                }
            }
        }

        private static void OnSceneLoadedInitSetupSuite(Scene scene, LoadSceneMode loadSceneMode)
        {
            var prefabToSpawn = PrefabReference.Instance.referencedPrefab;

            s_ObjectPool = new GameObjectPool();
            s_ObjectPool.Init(k_MaxObjectstoSpawn, prefabToSpawn);
        }







        [OneTimeSetUp]
        public override void SetupSuite()
        {
            base.SetupSuite();
            SceneManager.sceneLoaded += OnSceneLoadedInitSetupSuite;
        }

        [UnityTest, Performance]
        [TestCase(1, ExpectedResult = null)]
        [TestCase(10, ExpectedResult = null)]
        [TestCase(100, ExpectedResult = null)]
        [TestCase(800, ExpectedResult = null)]
        [TestCase(850, ExpectedResult = null)]
        [TestCase(900, ExpectedResult = null)]
        [TestCase(1000, ExpectedResult = null)]
        [TestCase(2500, ExpectedResult = null)]
        [TestCase(5000, ExpectedResult = null)]
        [TestCase(10000, ExpectedResult = null)]
        [TestCase(15000, ExpectedResult = null)]
        public IEnumerator TestSpawningManyObjects(int nbObjects)
        {
            Assert.LessOrEqual(nbObjects, k_MaxObjectstoSpawn); // sanity check

            // setup and wait for client to be ready
            TestCoordinator.Instance.TriggerRpc(NetworkVariablePerformanceTestsClient.SetupClientForTest1, BitConverter.GetBytes(nbObjects));
            foreach (var clientId in TestCoordinator.AllClientIdExceptMine)
            {
                // wait for the clients to be ready
                yield return new WaitUntil(TestCoordinator.ConsumeClientIsFinished(clientId));
            }

            // start test
            using (Measure.Scope($"Time Taken For Spawning {nbObjects} objects and getting report"))
            {
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
                for (int i = 0; i < NbWorkers; i++)
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
                yield return new WaitUntil(TestCoordinator.ConsumeClientIsFinished(clientId, useTimeoutException:false));
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
