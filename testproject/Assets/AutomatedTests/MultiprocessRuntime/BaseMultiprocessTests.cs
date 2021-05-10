using System;
using System.Collections;
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

        protected abstract int NbWorkers { get; }

        [OneTimeSetUp]
        public virtual void SetupSuite()
        {
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
            TestCoordinator.Instance.TestRunTeardown();
        }

        [OneTimeTearDown]
        public virtual void TeardownSuite()
        {
            // if (NetworkManager.Singleton.IsHost)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Debug.Log("Teardown, closing remote clients and stopping host");
                TestCoordinator.Instance.CloseRemoteClientRpc();
                NetworkManager.Singleton.StopHost();
                // SceneManager.UnloadSceneAsync(mainSceneName);
            }
            // var startTime = Time.time;
            // // wait to run next tests until this test is completely torn down
            // while (Time.time - startTime < TestCoordinator.maxWaitTimeout && NetworkManager.Singleton.ConnectedClients.Count > 0)
            // {
            //     Thread.Sleep(10);
            // }
        }
    }

    public class TestCoordinatorSmokeTests : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 1;

        public static void ExecuteSimpleCoordinatorTest()
        {
            TestCoordinator.Instance.WriteTestResultsServerRpc(float.PositiveInfinity);
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinator()
        {
            //make sure the test coordinator works
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
    }
}

