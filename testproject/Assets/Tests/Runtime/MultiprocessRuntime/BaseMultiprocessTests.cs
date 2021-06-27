using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MLAPI.MultiprocessRuntimeTests
{
    public class MultiprocessTestsAttribute : CategoryAttribute
    {
        public const string multiprocessCategoryName = "Multiprocess";
        public MultiprocessTestsAttribute() : base(multiprocessCategoryName){}
    }

    [MultiprocessTests]
    public abstract class BaseMultiprocessTests
    {

        protected virtual bool m_IsPerformanceTest => true;

        private bool ShouldIgnoreTests => m_IsPerformanceTest && Application.isEditor;

        protected abstract int NbWorkers { get; }

        [OneTimeSetUp]
        public virtual void SetupTestFixture()
        {
            if (ShouldIgnoreTests)
            {
                Assert.Ignore("Ignoring tests that shouldn't run from unity editor. Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
            }

            SceneManager.LoadScene(BuildMultiprocessTestPlayer.mainSceneName, LoadSceneMode.Single);
            SceneManager.sceneLoaded += OnSceneLoaded;

            for (int i = 0; i < NbWorkers; i++)
            {
                MultiprocessOrchestration.StartWorkerNode(); // will automatically start built player as clients
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
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
            TestCoordinator.Instance.KeepAliveClientRpc();
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
                TestCoordinator.Instance.CloseRemoteClientRpc();
                NetworkManager.Singleton.StopHost();
            }
        }
    }
}

