using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI.NetworkVariable;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static TestCoordinator.ExecuteStepInContext;

namespace MLAPI.MultiprocessRuntimeTests
{
    // todo profile all this
    public class NetworkVariablePerformanceTests : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 1;
        private const int k_MaxObjectsToSpawn = 10000;
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

            public static void Stop()
            {
                nbInstances--;
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
        public IEnumerator TestSpawningManyObjects([Values(1, 1000, 2000, 3000, 5000, 10000)] int nbObjects)
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
                var hasAddedHandler = NetworkManager.Singleton.PrefabHandler.AddHandler(prefabToSpawn,
                    new CustomPrefabSpawnerForPerformanceTests(prefabToSpawn, k_MaxObjectsToSpawn, SetupSpawnedObject, StopSpawnedObject));
                Assert.That(hasAddedHandler);

                // add client side reporter for later spawn steps
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
                using (Measure.Scope($"Time Taken For Spawning {nbObjects} objects server side and getting report"))
                {
                    // spawn prefabs for test
                    for (int i = 0; i < nbObjects; i++)
                    {
                        var spawnedObject = s_ObjectPool.Get();
                        var netObj = SetupSpawnedObject(spawnedObject);
                        netObj.Spawn(destroyWithScene: true);
                        m_SpawnedObjects.Add(netObj);
                    }
                }
            }, additionalIsFinishedWaiter: () =>
            {
                // wait for spawn results coming from clients
                int finishedCount = 0;
                if (TestCoordinator.AllClientIdsWithResults.Count != NbWorkers)
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
                // todo add more metrics like memory usage, time taken to execute, etc
                var allocated = new SampleGroup("NbSpawnedPerFrame", SampleUnit.Undefined);

                foreach (var clientId in TestCoordinator.AllClientIdsWithResults)
                {
                    var lastResult = TestCoordinator.PeekLatestResult(clientId);
                    Assert.That(lastResult, Is.EqualTo(nbObjects));
                }

                Assert.That(TestCoordinator.AllClientIdsWithResults.Count, Is.EqualTo(NbWorkers));
                foreach (var (clientId, result) in TestCoordinator.ConsumeCurrentResult())
                {
                    Debug.Log($"got result {result} from key {clientId}");
                    Measure.Custom(allocated, result);
                }

            });
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, nbObjectsBytes =>
            {
                var nbObjectsParam = BitConverter.ToInt32(nbObjectsBytes, 0);
                Assert.That(GameObject.FindObjectsOfType(typeof(OneNetVar)).Length, Is.EqualTo(nbObjectsParam), "Wrong number of spawned objects client side");
            }, paramToPass:BitConverter.GetBytes(nbObjects));
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
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
                    StopSpawnedObject(spawnedObject);
                }
                m_SpawnedObjects.Clear();
                s_ObjectPool.Finish();
            });

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate = null; // todo move access to callbackcomponent to singleton

                void UpdateWaitForAllOneNetVarToDespawn(float deltaTime)
                {
                    if (OneNetVar.nbInstances == 0)
                    {
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= UpdateWaitForAllOneNetVarToDespawn;
                        TestCoordinator.Instance.ClientFinishedServerRpc();
                    }
                }
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += UpdateWaitForAllOneNetVarToDespawn;
            }, waitMultipleUpdates: true, ignoreTimeout:true); // ignoring timeout since you don't want to hide any issues in the main tests

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(PrefabReference.Instance.referencedPrefab);
            });
        }

        [OneTimeTearDown]
        public override void TeardownSuite()
        {
            base.TeardownSuite();
            SceneManager.sceneLoaded -= OnSceneLoadedInitSetupSuite;
        }

        private static NetworkObject SetupSpawnedObject(GameObject spawnedObject)
        {
            spawnedObject.name = "ReplicatedObjectTest1";
            var oneNetVar = spawnedObject.AddComponent<OneNetVar>(); // todo this is expensive
            oneNetVar.Init();
            return oneNetVar.NetworkObject;
        }

        private static void StopSpawnedObject(NetworkObject destroyedObject)
        {
            OneNetVar.Stop();
        }
    }
}
