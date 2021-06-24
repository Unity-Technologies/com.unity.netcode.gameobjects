using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using MLAPI.NetworkVariable;
using MLAPI.Spawning;
using NUnit.Framework;
using NUnit.Framework.Internal;
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
                // Debug.Log($"nb one net var instance!!!!! {nbInstances}");
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
        public IEnumerator TestSpawningManyObjects([Values(1, 1000, 10000)] int nbObjects)
        {
            InitContextSteps();

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                Assert.LessOrEqual(nbObjects, k_MaxObjectstoSpawn); // sanity check
            });

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, paramToPass: BitConverter.GetBytes(nbObjects), todo: nbObjectsBytes =>
            {
                // setup clients

                var targetCount = BitConverter.ToInt32(nbObjectsBytes, 0);
                var prefabToSpawn = PrefabReference.Instance.referencedPrefab;
                var addedHandler = NetworkManager.Singleton.PrefabHandler.AddHandler(prefabToSpawn, new CustomPrefabSpawnForTest1(prefabToSpawn));
                if (!addedHandler)
                {
                    throw new Exception("Couldn't add Handler!");
                }

                void Update(float deltaTime)
                {
                    var count = OneNetVar.nbInstances;
                    Debug.Log($"count is {count}");
                    if (count > 0)
                    {
                        TestCoordinator.Instance.WriteTestResultsServerRpc(count);

                        if (count >= targetCount)
                        {
                            // we got what we want, don't update results any longer
                            NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= Update;
                        }
                    }
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

                foreach (var clientId in TestCoordinator.AllClientIdsWithResults)
                {
                    var lastResult = TestCoordinator.PeekLatestResult(clientId);
                    Assert.That(lastResult, Is.EqualTo(nbObjects));
                }

                Assert.That(TestCoordinator.AllClientIdsWithResults.Length, Is.EqualTo(NbWorkers));
                foreach (var current in TestCoordinator.ConsumeCurrentResult())
                {
                    Debug.Log($"got result {current.result} from key {current.clientId}");
                    Measure.Custom(allocated, current.result);
                }

                Debug.Log($"finished with test for {nbObjects} objects");
            });
        }

        [UnityTearDown, MultiprocessContextBasedTest]
        public IEnumerator UnityTeardown()
        {
            InitContextSteps();

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
