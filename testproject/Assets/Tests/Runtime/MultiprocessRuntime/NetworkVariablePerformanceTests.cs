using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static ExecuteStepInContext;
using Object = UnityEngine.Object;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class NetworkVariablePerformanceTests : BaseMultiprocessTests
    {
        private const int k_MaxObjectsToSpawn = 10000;
        private List<OneNetVar> m_ServerSpawnedObjects = new List<OneNetVar>();
        private static GameObjectPool<OneNetVar> s_ServerObjectPool;
        private CustomPrefabSpawnerForPerformanceTests<OneNetVar> m_ClientPrefabHandler;
        private OneNetVar m_PrefabToSpawn;
        protected override bool RunUnityTearDown => false;
        protected override bool IsPerformanceTest => true;
        protected override int GetWorkerCount()
        {
            return platformList == null ? 1 : platformList.Length;
        }

        private class OneNetVar : NetworkBehaviour
        {
            public static int InstanceCount;
            public NetworkVariable<int> OneInt = new NetworkVariable<int>();

            public void Initialize()
            {
                InstanceCount++;
                if (IsServer)
                {
                    OneInt.Value = 1;
                }
            }

            public static void Stop()
            {
                InstanceCount--;
            }
        }

        [OneTimeSetUp]
        public override void SetupTestSuite()
        {
            base.SetupTestSuite();
            SceneManager.sceneLoaded += OnSceneLoadedInitSetupSuite;
        }

        private void OnSceneLoadedInitSetupSuite(Scene scene, LoadSceneMode loadSceneMode)
        {
            SceneManager.sceneLoaded -= OnSceneLoadedInitSetupSuite;
            InitializePrefab();
            s_ServerObjectPool = new GameObjectPool<OneNetVar>();
            s_ServerObjectPool.Initialize(k_MaxObjectsToSpawn, m_PrefabToSpawn);
        }

        private void InitializePrefab()
        {
            if (m_PrefabToSpawn == null)
            {
                var prefabCopy = Object.Instantiate(PrefabReference.Instance.ReferencedPrefab);
                m_PrefabToSpawn = prefabCopy.AddComponent<OneNetVar>();
            }
        }

        [UnityTest, Performance, MultiprocessContextBasedTest]
        public IEnumerator TestSpawningManyObjects([Values(7, 80, 300, 500)] int nbObjects)
        {
            InitializeContextSteps();

            if (!IsRegistering && TestCoordinator.Instance.NetworkManager.IsServer && BuildMultiprocessTestPlayer.ReadBuildInfo().IsDebug)
            {
                // build test player in debug mode to enable this
                var timeToWait = 20;
                Debug.Log($"Debug mode tests enabled, waiting {timeToWait} seconds to give some time to attach debugger");
                yield return new WaitForSeconds(timeToWait);
            }

            yield return new ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                MultiprocessLogger.Log("Step 1 - Start");
                Assert.LessOrEqual(nbObjects, k_MaxObjectsToSpawn); // sanity check
                MultiprocessLogger.Log("Step 1 - End");
            });

            yield return new ExecuteStepInContext(StepExecutionContext.Clients, stepToExecute: nbObjectsBytes =>
            {
                MultiprocessLogger.Log("Step 2 - Start");
                // setup clients
                InitializePrefab();
                var targetCount = BitConverter.ToInt32(nbObjectsBytes, 0);

                m_ClientPrefabHandler = new CustomPrefabSpawnerForPerformanceTests<OneNetVar>(m_PrefabToSpawn, k_MaxObjectsToSpawn, SetupSpawnedObject, StopSpawnedObject);
                var hasAddedHandler = NetworkManager.Singleton.PrefabHandler.AddHandler(m_PrefabToSpawn.NetworkObject, m_ClientPrefabHandler);
                Assert.That(hasAddedHandler);

                // add client side reporter for later spawn steps
                void UpdateFunc(float deltaTime)
                {
                    var count = OneNetVar.InstanceCount;
                    MultiprocessLogger.Log($"Step 2 - Update Func {deltaTime}");
                    if (count > 0)
                    {
                        TestCoordinator.Instance.WriteTestResultsServerRpc(count);

                        if (count >= targetCount)
                        {
                            // we got what we want, don't update results any longer
                            NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= UpdateFunc;
                        }
                    }
                }

                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += UpdateFunc;
            }, paramToPass: BitConverter.GetBytes(nbObjects));

            int resultsCount = 0;

            yield return new ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                MultiprocessLogger.Log($"Step 3 - Start {nbObjects}");
                // start test
                using (Measure.Scope($"Time Taken For Spawning {nbObjects} objects server side and getting report"))
                {
                    MultiprocessLogger.Log("Step 3 - Start using block");
                    // spawn prefabs for test
                    var totalAllocSampleGroup = new SampleGroup("GC Alloc", SampleUnit.Kilobyte);
                    var beforeAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
                    Measure.Custom(totalAllocSampleGroup, beforeAllocatedMemory / 1024f);
                    for (int i = 0; i < nbObjects; i++)
                    {
                        var spawnedObject = s_ServerObjectPool.Get();
                        SetupSpawnedObject(spawnedObject);
                        spawnedObject.NetworkObject.Spawn(destroyWithScene: true);
                        m_ServerSpawnedObjects.Add(spawnedObject);
                    }

                    var afterAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
                    Measure.Custom(totalAllocSampleGroup, afterAllocatedMemory / 1024f);
                    var diffAllocSampleGroup = new SampleGroup("GC Alloc diff for Spawn Server side", SampleUnit.Byte);
                    Measure.Custom(diffAllocSampleGroup, afterAllocatedMemory - beforeAllocatedMemory);
                    MultiprocessLogger.Log($"Step 3 - end using block {nbObjects}");
                }
            }, additionalIsFinishedWaiter: () =>
            {
                if (TestCoordinator.AllClientIdsWithResults.Count > resultsCount)
                {
                    resultsCount = TestCoordinator.AllClientIdsWithResults.Count;
                    MultiprocessLogger.Log($"Step 3 - additionalIsFinishedWaiter {TestCoordinator.AllClientIdsWithResults.Count} == {GetWorkerCount()}?");
                    // wait for spawn results coming from clients
                    int finishedCount = 0;
                    if (TestCoordinator.AllClientIdsWithResults.Count != (GetWorkerCount()))
                    {
                        MultiprocessLogger.Log($"Step 3 - Apparently TestCoordinator.AllClientIdsWithResults.Count != (WorkerCount)");
                        return false;
                    }

                    foreach (var clientIdWithResult in TestCoordinator.AllClientIdsWithResults)
                    {
                        var latestResult = TestCoordinator.PeekLatestResult(clientIdWithResult);
                        if (latestResult == nbObjects)
                        {
                            finishedCount++;
                        }
                        else
                        {
                            MultiprocessLogger.Log($"Step 3 - latestResult {latestResult} nbObjects {nbObjects}");
                        }
                    }
                    MultiprocessLogger.Log($"Step 3 - finishedCount == WorkerCount : {finishedCount} == {GetWorkerCount()}");
                    return finishedCount == GetWorkerCount();
                }
                else
                {
                    return false;
                }
            });

            var serverLastResult = 0f;
            yield return new ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                MultiprocessLogger.Log($"Step 4 - Start {nbObjects}");
                // add measurements
                // todo add more client-side metrics like memory usage, time taken to execute, etc
                var allocated = new SampleGroup("NbSpawnedPerFrame client side", SampleUnit.Undefined);

                foreach (var clientId in TestCoordinator.AllClientIdsWithResults)
                {
                    var lastResult = TestCoordinator.PeekLatestResult(clientId);
                    Assert.That(lastResult, Is.EqualTo(nbObjects));
                }

                Assert.That(TestCoordinator.AllClientIdsWithResults.Count, Is.EqualTo(GetWorkerCount()));
                foreach (var (clientId, result) in TestCoordinator.ConsumeCurrentResult())
                {
                    Measure.Custom(allocated, result);
                    serverLastResult = result;
                }
                MultiprocessLogger.Log($"Step 4 - End {nbObjects}");
            });
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, nbObjectsBytes =>
            {
                MultiprocessLogger.Log($"Step 5 - Start {nbObjects}");
                var nbObjectsParam = BitConverter.ToInt32(nbObjectsBytes, 0);
                Assert.That(Object.FindObjectsOfType(typeof(OneNetVar)).Length, Is.EqualTo(nbObjectsParam + 1), "Wrong number of spawned objects client side"); // +1 for the prefab to spawn
                MultiprocessLogger.Log($"Step 5 - End {nbObjects}");
            }, paramToPass: BitConverter.GetBytes(nbObjects));
            yield return new ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                MultiprocessLogger.Log($"Step 6 - {nbObjects}");
                Debug.Log($"finished with test for {nbObjects} expected objects and got {serverLastResult} objects");
            });
        }

        [UnityTearDown, MultiprocessContextBasedTest]
        public IEnumerator UnityTeardown()
        {
            InitializeContextSteps();

            yield return new ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                foreach (var spawnedObject in m_ServerSpawnedObjects)
                {
                    spawnedObject.NetworkObject.Despawn(false);
                    s_ServerObjectPool.Release(spawnedObject);
                    StopSpawnedObject(spawnedObject);
                }

                m_ServerSpawnedObjects.Clear();
            });

            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                //todo move access to callbackcomponent to singleton
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate = null;

                void UpdateWaitForAllOneNetVarToDespawnFunc(float deltaTime)
                {
                    if (OneNetVar.InstanceCount == 0)
                    {
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= UpdateWaitForAllOneNetVarToDespawnFunc;
                        TestCoordinator.Instance.ClientFinishedServerRpc();
                    }
                }

                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += UpdateWaitForAllOneNetVarToDespawnFunc;
            }, waitMultipleUpdates: true, ignoreTimeoutException: true); // ignoring timeout since you don't want to hide any issues in the main tests

            yield return new ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                m_ClientPrefabHandler.Dispose();
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(m_PrefabToSpawn.NetworkObject);
            });

            TestCoordinator.Instance.TestRunTeardown();

            yield return null;
        }

        [OneTimeTearDown]
        public override void TeardownSuite()
        {
            UnityTearDown();
            base.TeardownSuite();
            if (!IsPerformanceTest)
            {
                s_ServerObjectPool.Dispose();
            }
        }

        private static void SetupSpawnedObject(OneNetVar spawnedObject)
        {
            spawnedObject.Initialize();
        }

        private static void StopSpawnedObject(OneNetVar destroyedObject)
        {
            OneNetVar.Stop();
        }
    }
}
