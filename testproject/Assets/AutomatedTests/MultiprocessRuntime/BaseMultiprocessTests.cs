using System;
using System.Collections;
using System.Linq;
using System.Threading;
using MLAPI;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static TestCoordinator.ExecuteStepInContext;


namespace MLAPI.MultiprocessRuntimeTests
{
    public class MultiprocessTests : CategoryAttribute
    {
        public const string multiprocessCategoryName = "Multiprocess";
        public MultiprocessTests(params string[] nodesRequired) : base(multiprocessCategoryName){}
    }

    [MultiprocessTests]
    public abstract class BaseMultiprocessTests
    {
        public const string mainSceneName = "MultiprocessTestingScene";

        protected virtual bool m_IsPerformanceTest => true;

        private bool ShouldIgnoreTests => m_IsPerformanceTest && !Environment.GetCommandLineArgs().Contains("-runTests");

        protected abstract int NbWorkers { get; }

        [OneTimeSetUp]
        public virtual void SetupSuite()
        {
            if (ShouldIgnoreTests)
            {
                Assert.Ignore("Ignoring tests that shouldn't run from unity editor. Performance tests should be run from remote test execution on device");
            }
            // todo cleanup comments
            // Build(TestCoordinator.buildPath);

            SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log("starting processes");
            for (int i = 0; i < NbWorkers; i++)
            {
                TestCoordinator.StartWorkerNode(); // will automatically start as clients
            }

            Debug.Log("processes started");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("starting MLAPI host");
            NetworkManager.Singleton.StartHost();
        }

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer);
            var startTime = Time.time;
            while (NetworkManager.Singleton.ConnectedClients.Count <= NbWorkers)
            {
                yield return new WaitForSeconds(0.2f);
                if (Time.time - startTime > TestCoordinator.maxWaitTimeout)
                {
                    throw new Exception($"waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {NbWorkers}, failing");
                }
            }
        }

        [TearDown]
        public virtual void Teardown()
        {
            if (!ShouldIgnoreTests)
            {
                TestCoordinator.Instance.TestRunTeardown();
            }
        }

        [OneTimeTearDown]
        public virtual void TeardownSuite()
        {
            if (!ShouldIgnoreTests)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Debug.Log("Teardown, closing remote clients and stopping host");
                TestCoordinator.Instance.CloseRemoteClientRpc();
                NetworkManager.Singleton.StopHost();
            }
        }
    }

    public class TestCoordinatorSmokeTests : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 1;

        protected override bool m_IsPerformanceTest => false;

        public static void ExecuteSimpleCoordinatorTest()
        {
            TestCoordinator.Instance.WriteTestResultsServerRpc(float.PositiveInfinity);
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinator()
        {
            // Sanity check for TestCoordinator
            // Call the method
            TestCoordinator.Instance.TriggerRpc(ExecuteSimpleCoordinatorTest);

            for (int i = 0; i < NbWorkers; i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                foreach (var current in TestCoordinator.ConsumeCurrentResult())
                {
                    Debug.Log($"got results, asserting, result is {current.result} from key {current.clientId}");
                    Assert.Greater(current.result, 0f);
                }
            }
        }

        [UnityTest, MultiprocessContextBasedTestAttribute()]
        public IEnumerator TestExecuteInContext()
        {
            // TODO convert other tests to this format
            // todo move ExecuteInContext out of here (in test coordinator?)

            int stepCountExecuted = 0;
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, (byte[] args) =>
            {
                stepCountExecuted++;
                int count = BitConverter.ToInt32(args, 0);
                Debug.Log($"something server side, count is {count}");
            }, paramToPass: BitConverter.GetBytes(1));

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, (byte[] args) =>
            {
                int count = BitConverter.ToInt32(args, 0);
                Debug.Log($"something client side, count is {count}");
                TestCoordinator.Instance.WriteTestResultsServerRpc(12345);
#if UNITY_EDITOR
                Assert.Fail("Should not be here!! This should only execute on client!!");
#endif
            }, paramToPass: BitConverter.GetBytes(1));

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                stepCountExecuted++;
                int count = 0;
                foreach (var res in TestCoordinator.ConsumeCurrentResult())
                {
                    count++;
                    Assert.AreEqual(12345, res.result);
                }
                Assert.Greater(count, 0);
            });

            int timeToWait = 4;
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                void Update(float _)
                {
                    if (Time.time > timeToWait)
                    {
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= Update;
                        TestCoordinator.Instance.WriteTestResultsServerRpc(Time.time);

                        TestCoordinator.Instance.ClientFinishedServerRpc(); // since finishOnInvoke is false, we need to do this manually
                    }
                    else
                    {
                        Debug.Log($"current time on client : {Time.time}");
                    }
                };
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;
            }, finishOnInvoke: false); // waits multiple frames before allowing the next action to continue.

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, (byte[] args) =>
            {
                stepCountExecuted++;
                int count = 0;
                foreach (var res in TestCoordinator.ConsumeCurrentResult())
                {
                    count++;
                    Assert.GreaterOrEqual(res.result, timeToWait);
                }
                Assert.Greater(count, 0);
            });
            if (!TestCoordinator.Instance.isRegistering)
            {
                Assert.AreEqual(3, stepCountExecuted);
            }
        }
    }
}

