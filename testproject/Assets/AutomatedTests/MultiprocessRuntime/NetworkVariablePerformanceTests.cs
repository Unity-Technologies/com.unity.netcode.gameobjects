using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Spawning;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static MLAPI.MultiprocessRuntimeTests.NetworkVariablePerformanceTests.ExecuteInContext;
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
        private static ObjectPool<GameObject> s_ObjectPool;

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

            s_ObjectPool = new ObjectPool<GameObject>(
                createFunc: () => GameObject.Instantiate(prefabToSpawn),
                actionOnGet: objectToGet => objectToGet.SetActive(true),
                actionOnRelease: objectToRelease => objectToRelease.SetActive(false),
                defaultCapacity:k_MaxObjectstoSpawn
            );
        }


        public class ExecuteInContext : CustomYieldInstruction
        {
            public enum ExecutionContext
            {
                Server,
                Clients
            }

            private ExecutionContext m_ActionContextContext;
            private Action<byte[]> m_Todo;
            public static Dictionary<string, ExecuteInContext> allActions = new Dictionary<string, ExecuteInContext>();
            // private static int s_ActionID;
            private static Dictionary<string, int> s_MethodIDCounter = new Dictionary<string, int>();
            // private int m_CurrentActionID;
            private NetworkManager m_NetworkManager;
            private bool m_IsRegistering;
            private List<Func<bool>> m_WaitForClientCheck = new List<Func<bool>>();
            private bool m_FinishOnInvoke;

            // assumes this is called from same callsite as ExecuteInContext
            public static void InitTest(MethodBase callerMethod)
            {
                // var callerMethod = new StackFrame(1).GetMethod();
                var methodIdentifier = GetMethodIdentifier(callerMethod);
                s_MethodIDCounter[methodIdentifier] = 0;
            }

            public static string GetMethodIdentifier(MethodBase method)
            {
                string allParameters = "";
                foreach (var param in method.GetParameters())
                {
                    allParameters += param.Name;
                }

                var info = method.DeclaringType.FullName + method.Name + allParameters;
                Debug.Log(info);
                return info;
            }

            private bool ShouldExecuteLocally => (m_ActionContextContext == ExecutionContext.Server && m_NetworkManager.IsServer) || (m_ActionContextContext == ExecutionContext.Clients && !m_NetworkManager.IsServer);

            /// <summary>
            /// Executes an action with the specified context. This allows writing tests all in the same sequential flow,
            /// making it more readable. This allows not having to jump between static client methods and test method
            /// </summary>
            /// <param name="actionContext">context to use. for example, should execute client side? server side?</param>
            /// <param name="todo">action to execute</param>
            /// <param name="isRegistering">is it currently registering this action. This should be passed by the test method on process startup</param>
            /// <param name="paramToPass">parameters to pass to action</param>
            /// <param name="networkManager"></param>
            /// <param name="finishOnInvoke"> waits multiple frames before allowing the execution to continue. This means ClientFinishedServerRpc must be called manually</param>
            public ExecuteInContext(ExecutionContext actionContext, Action<byte[]> todo, byte[] paramToPass = default, NetworkManager networkManager = null, bool finishOnInvoke = true)
            {
                m_IsRegistering = TestCoordinator.Instance.isRegistering;
                m_ActionContextContext = actionContext;
                m_Todo = todo;
                m_FinishOnInvoke = finishOnInvoke;
                if (networkManager == null)
                {
                    networkManager = NetworkManager.Singleton;
                }

                m_NetworkManager = networkManager; // todo test using this for in-process tests too?

                var callerMethod = new StackFrame(1).GetMethod();
                var methodId = GetMethodIdentifier(callerMethod);
                if (!s_MethodIDCounter.ContainsKey(methodId))
                {
                    s_MethodIDCounter[methodId] = 0;
                }

                string currentActionID = methodId + s_MethodIDCounter[methodId]++;

                if (m_IsRegistering)
                {
                    Debug.Log($"registering action with id {currentActionID}");
                    allActions[currentActionID] = this;
                }
                else
                {
                    if (ShouldExecuteLocally)
                    {
                        m_Todo.Invoke(paramToPass);
                    }
                    else
                    {
                        if (networkManager.IsServer)
                        {
                            TestCoordinator.Instance.TriggerActionIDClientRpc(currentActionID, paramToPass,
                                clientRpcParams: new ClientRpcParams()
                                {
                                    Send = new ClientRpcSendParams() {TargetClientIds = TestCoordinator.AllClientIdExceptMine.ToArray()}
                                });
                            foreach (var clientId in TestCoordinator.AllClientIdExceptMine)
                            {
                                m_WaitForClientCheck.Add(TestCoordinator.ConsumeClientIsFinished(clientId));
                            }
                        }
                        else
                        {
                            TestCoordinator.Instance.TriggerActionIDServerRpc(currentActionID, paramToPass);
                        }
                    }
                }
            }

            public void Invoke(byte[] args)
            {
                m_Todo.Invoke(args);
                if (m_FinishOnInvoke)
                {
                    if (!m_NetworkManager.IsServer)
                    {
                        TestCoordinator.Instance.ClientFinishedServerRpc();
                    }
                    else
                    {
                        throw new NotImplementedException("todo implement");
                    }
                }
            }

            public override bool keepWaiting
            {
                get
                {
                    if (m_IsRegistering || ShouldExecuteLocally || m_WaitForClientCheck == null)
                    {
                        return false;
                    }

                    for (int i = m_WaitForClientCheck.Count-1; i >= 0; i--)
                    {
                        var waiter = m_WaitForClientCheck[i];
                        var receivedResponse = waiter.Invoke();
                        if (receivedResponse)
                        {
                            m_WaitForClientCheck.RemoveAt(i);
                        }
                        else
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }


        // public class MyOuterActionAttribute : NUnitAttribute, IOuterUnityTestAction
        // {
        //     public static int a = 0;
        //     public IEnumerator BeforeTest(ITest test)
        //     {
        //         a = 1;
        //         yield break;
        //     }
        //
        //     public IEnumerator AfterTest(ITest test)
        //     {
        //         a = 0;
        //         yield break;
        //     }
        // }
        // [UnityTest, MyOuterActionAttribute]
        // public IEnumerator MyTestInsidePlaymode()
        // {
        //     Assert.AreEqual(1, MyOuterActionAttribute.a);
        //     yield return null;
        // }

        [AttributeUsage(AttributeTargets.Method)]
        public class MultiprocessContextBasedTestAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                yield return new WaitUntil(() => TestCoordinator.Instance != null && TestCoordinator.Instance.hasRegistered);
                var method = test.Method.ReturnType.GetMethods(BindingFlags.Default).Where(m => m.Name == "MoveNext").First();
                ExecuteInContext.InitTest(method);
                // ExecuteInContext.InitTest(test.Method.MethodInfo);
            }

            public IEnumerator AfterTest(ITest test)
            {
                yield break;
            }
        }

        [UnityTest, MultiprocessContextBasedTest]
        public IEnumerator TestExecuteInContext()
        {
            // TODO convert other tests to this format
            // todo move ExecuteInContext out of here (in test coordinator?)

            yield return new ExecuteInContext(ExecutionContext.Server, (byte[] args) =>
            {
                int count = BitConverter.ToInt32(args, 0);
                Debug.Log($"something server side, count is {count}");
            }, paramToPass: BitConverter.GetBytes(1));

            yield return new ExecuteInContext(ExecutionContext.Clients, (byte[] args) =>
            {
                int count = BitConverter.ToInt32(args, 0);
                Debug.Log($"something client side, count is {count}");
                TestCoordinator.Instance.WriteTestResultsServerRpc(12345);
#if UNITY_EDITOR
                Assert.Fail("Should not be here!! This should only execute on client!!");
#endif
            }, paramToPass: BitConverter.GetBytes(1));

            yield return new ExecuteInContext(ExecutionContext.Server, _ =>
            {
                int count = 0;
                foreach (var res in TestCoordinator.ConsumeCurrentResult())
                {
                    count++;
                    Assert.AreEqual(12345, res.result);
                }
                Assert.Greater(count, 0);
            });

            int timeToWait = 4;
            yield return new ExecuteInContext(ExecutionContext.Clients, _ =>
            {
                void update(float _)
                {
                    if (Time.time > timeToWait)
                    {
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= update;
                        TestCoordinator.Instance.WriteTestResultsServerRpc(Time.time);

                        TestCoordinator.Instance.ClientFinishedServerRpc(); // since finishOnInvoke is false, we need to do this manually
                    }
                    else
                    {
                        Debug.Log($"current time on client : {Time.time}");
                    }
                };
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += update;
            }, finishOnInvoke: false); // waits multiple frames before allowing the next action to continue.

            yield return new ExecuteInContext(ExecutionContext.Server, (byte[] args) =>
            {
                int count = 0;
                foreach (var res in TestCoordinator.ConsumeCurrentResult())
                {
                    count++;
                    Assert.GreaterOrEqual(res.result, timeToWait);
                }
                Assert.Greater(count, 0);
            });
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
        [TestCase(1000, ExpectedResult = null)]
        [TestCase(800, ExpectedResult = null)]
        [TestCase(400, ExpectedResult = null)]
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
