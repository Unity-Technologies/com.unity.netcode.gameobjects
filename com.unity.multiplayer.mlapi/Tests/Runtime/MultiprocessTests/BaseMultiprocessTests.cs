using System;
using System.Collections;
using MLAPI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MLAPI.MultiprocessRuntimeTests
{
    public class MultiprocessTests : CategoryAttribute
    {
        public const string categoryName = "Multiprocess";
        public MultiprocessTests(params string[] nodesRequired) : base(categoryName){}
    }

    [MultiprocessTests]
    public abstract class BaseMultiprocessTests
    {
        protected abstract int NbWorkers { get; }

        [OneTimeSetUp]
        public void SetupSuite()
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);

            Debug.Log("starting processes");
            for (int i = 0; i < NbWorkers; i++)
            {
                TestCoordinator.StartWorkerNode();
            }

            Debug.Log("processes started");
        }

        [UnitySetUp]
        public IEnumerator Setup()
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
        public void TeardownSingleTest()
        {
            TestCoordinator.Instance.TestRunTeardown();
        }

        [OneTimeTearDown]
        public void TeardownSuite()
        {
            TestCoordinator.Instance.CloseRemoteClientRpc();
        }

        protected static void ExecuteSimpleCoordinatorTest()
        {
            TestCoordinator.WriteResults(Time.time);
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinator()
        {
            //make sure the test coordinator works
            // Call the method
            TestCoordinator.Instance.TriggerTestClientRpc(TestCoordinator.GetMethodInfo(ExecuteSimpleCoordinatorTest));

            for (int i = 0; i < NbWorkers; i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.SetResults());
                var resKey = TestCoordinator.Instance.CurrentResultClient;

                Debug.Log($"got results, asserting, result is {TestCoordinator.GetCurrentResult()} from key {resKey}");
                Assert.True(TestCoordinator.GetCurrentResult() > 0f);
            }
        }
    }
}

