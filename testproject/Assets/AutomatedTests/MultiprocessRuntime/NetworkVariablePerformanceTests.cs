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
using static TestCoordinator.ExecuteStepInContext;


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
        protected override bool m_IsPerformanceTest => false; // for debug, todo remove me

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

        [UnityTest, Performance, MultiprocessContextBasedTest]
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
            InitSteps();

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                Assert.LessOrEqual(nbObjects, k_MaxObjectstoSpawn); // sanity check
            });

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                // setup clients
                void Update(float deltaTime)
                {
                    var count = OneNetVar.nbInstances;
                    if (count > 0)
                    {
                        TestCoordinator.Instance.WriteTestResultsServerRpc(count);

                        if (count >= s_TargetCount)
                        {
                            // we got what we want, don't update results any longer
                            NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= Update;
                        }
                    }
                }

                s_TargetCount = nbObjects;
                var prefabToSpawn = PrefabReference.Instance.referencedPrefab;
                var addedHandler = NetworkManager.Singleton.PrefabHandler.AddHandler(prefabToSpawn, new CustomPrefabSpawnForTest1(prefabToSpawn));
                if (!addedHandler)
                {
                    throw new Exception("Couldn't add Handler!");
                }

                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;
            });

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {

                // start test
                using (Measure.Scope($"Time Taken For Spawning {nbObjects} objects and getting report"))
                {
                    // spawn prefabs for test

                    // todo test with multiple workers
                    for (int i = 0; i < nbObjects; i++)
                    {
                        var spawnedObject = s_ObjectPool.Get();
                        var oneNetVar = SetupSpawnedObject(spawnedObject);
                        oneNetVar.NetworkObject.Spawn(destroyWithScene: true);
                        m_SpawnedObjects.Add(oneNetVar.NetworkObject);
                    }
                }
            }, additionalIsFinishedWaiter: () =>
            {
                // wait for spawn results coming from clients
                int finishedCount = 0;
                if (TestCoordinator.AllClientIdsWithResults.Length != NbWorkers)
                {
                    return false;
                }

                foreach (var clientIdWithResult in TestCoordinator.AllClientIdsWithResults)
                {
                    var latestResult = TestCoordinator.PeekLatestResult(clientIdWithResult);
                    if (latestResult == nbObjects)
                    {
                        finishedCount++;
                    }
                }

                return finishedCount == NbWorkers;
            });
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                // add measurements
                var allocated = new SampleGroup($"NbSpawnedPerFrame", SampleUnit.Undefined);

                List<(ulong, float)> wrongFirstResults = new List<(ulong, float)>();
                foreach (var current in TestCoordinator.ConsumeCurrentResult())
                {
                    Debug.Log($"got results, asserting, result is {current.result} from key {current.clientId}");
                    Measure.Custom(allocated, current.result);
                    if (current.result != nbObjects)
                    {
                        wrongFirstResults.Add(current);
                    }
                }

                if (wrongFirstResults.Count > 0)
                {
                    Assert.Fail($"Expected first spawn to be {nbObjects}, but instead got {wrongFirstResults[0].Item2} items for client {wrongFirstResults[0].Item1}");
                }

                Debug.Log($"finished with test for {nbObjects} objects");
            });

        }

        [UnityTearDown, MultiprocessContextBasedTest]
        public IEnumerator UnityTeardown()
        {
            InitSteps();

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                foreach (var spawnedObject in m_SpawnedObjects)
                {
                    spawnedObject.Despawn();
                    s_ObjectPool.Release(spawnedObject.gameObject);
                }
                m_SpawnedObjects.Clear();
            });

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                var prefabToSpawn = PrefabReference.Instance.referencedPrefab;
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefabToSpawn);
                s_TargetCount = 0;
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate = null;

                void WaitForAllOneNetVarToDespawn(float deltaTime)
                {
                    var count = OneNetVar.nbInstances;
                    Debug.Log($"waiting for despawn, count is {count} before client setting its done");
                    if (count == 0)
                    {
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= WaitForAllOneNetVarToDespawn;
                        TestCoordinator.Instance.ClientFinishedServerRpc();
                    }
                }
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += WaitForAllOneNetVarToDespawn;
            }, spansMultipleUpdates: true);
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
