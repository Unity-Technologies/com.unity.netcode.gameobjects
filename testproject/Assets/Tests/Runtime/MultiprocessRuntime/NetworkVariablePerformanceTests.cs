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
using static TestCoordinator.ExecuteStepInContext;
using Random = System.Random;

namespace MLAPI.MultiprocessRuntimeTests
{
    // todo profile all this
    public class NetworkVariablePerformanceTests : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 1;
        private const int k_MaxObjectsToSpawn = 100000;
        private List<NetworkObject> m_SpawnedObjects = new List<NetworkObject>();
        private static GameObjectPool s_ObjectPool;
        protected override bool m_IsPerformanceTest => true;

        private class OneNetVar : NetworkBehaviour
        {
            public static int nbInstances;
            public NetworkVariableInt oneInt = new NetworkVariableInt();

            public void Init()
            {
                nbInstances++;
                if (IsServer)
                {
                    oneInt.Value = 1;
                }
            }

            private void OnDisable()
            {
                nbInstances--;
            }
        }

        private class CustomPrefabSpawnForTest1 : INetworkPrefabInstanceHandler
        {
            // private GameObject m_PrefabToSpawn;
            private GameObjectPool m_ObjectPool;

            public CustomPrefabSpawnForTest1(GameObject prefabToSpawn)
            {
                // m_PrefabToSpawn = prefabToSpawn;
                m_ObjectPool = new GameObjectPool();
                m_ObjectPool.Init(k_MaxObjectsToSpawn, prefabToSpawn);
            }

            public NetworkObject HandleNetworkPrefabSpawn(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                var networkObject = m_ObjectPool.Get().GetComponent<NetworkObject>(); // todo this is expensive
                var r = new Random();
                Transform netTransform = networkObject.transform;
                netTransform.position = new Vector3(r.Next(-10, 10), r.Next(-10, 10), r.Next(-10, 10));
                netTransform.rotation = rotation;
                SetupSpawnedObject(networkObject.gameObject); // adds custom component on spawn
                return networkObject;
            }

            public void HandleNetworkPrefabDestroy(NetworkObject networkObject)
            {
                Transform netTransform = networkObject.transform;
                netTransform.position = Vector3.zero;
                netTransform.rotation = Quaternion.identity;
                // TeardownSpawnedObject(networkObject.gameObject);
                m_ObjectPool.Release(networkObject.gameObject);

                // UnityEngine.Object.Destroy(networkObject.gameObject);
            }
        }

        private static void OnSceneLoadedInitSetupSuite(Scene scene, LoadSceneMode loadSceneMode)
        {
            var prefabToSpawn = PrefabReference.Instance.referencedPrefab;

            s_ObjectPool = new GameObjectPool();
            s_ObjectPool.Init(k_MaxObjectsToSpawn, prefabToSpawn);
        }

        [OneTimeSetUp]
        public override void SetupTestFixture()
        {
            base.SetupTestFixture();
            SceneManager.sceneLoaded += OnSceneLoadedInitSetupSuite;
        }

        [UnityTest, Performance, MultiprocessContextBasedTest]
        public IEnumerator TestSpawningManyObjects([Values(1, 1000, 10000)] int nbObjects)
        {
            InitContextSteps();

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                Assert.LessOrEqual(nbObjects, k_MaxObjectsToSpawn); // sanity check
            });

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, stepToExecute: nbObjectsBytes =>
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
            }, paramToPass: BitConverter.GetBytes(nbObjects));

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                // start test
                using (Measure.Scope($"Time Taken For Spawning {nbObjects} objects and getting report"))
                {
                    // spawn prefabs for test
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
                var allocated = new SampleGroup("NbSpawnedPerFrame", SampleUnit.Undefined);

                foreach (var clientId in TestCoordinator.AllClientIdsWithResults)
                {
                    var lastResult = TestCoordinator.PeekLatestResult(clientId);
                    Assert.That(lastResult, Is.EqualTo(nbObjects));
                }

                Assert.That(TestCoordinator.AllClientIdsWithResults.Length, Is.EqualTo(NbWorkers));
                foreach (var (clientId, result) in TestCoordinator.ConsumeCurrentResult())
                {
                    Debug.Log($"got result {result} from key {clientId}");
                    Measure.Custom(allocated, result);
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

                void UpdateWaitForAllOneNetVarToDespawn(float deltaTime)
                {
                    var count = OneNetVar.nbInstances;
                    if (count == 0)
                    {
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= UpdateWaitForAllOneNetVarToDespawn;
                        TestCoordinator.Instance.ClientFinishedServerRpc();
                    }
                }
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += UpdateWaitForAllOneNetVarToDespawn;
            }, waitMultipleUpdates: true);
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
            var oneNetVar = spawnedObject.AddComponent<OneNetVar>(); // todo this is expensive
            oneNetVar.Init();
            return oneNetVar;
        }
    }
}
