using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class MultiprocessTestsAttribute : CategoryAttribute
    {
        public const string MultiprocessCategoryName = "Multiprocess";
        public MultiprocessTestsAttribute() : base(MultiprocessCategoryName) { }
    }

    [MultiprocessTests]
    public abstract class BaseMultiprocessTests
    {
        protected virtual bool IsPerformanceTest => true;

        private const string k_GlobalEmptySceneName = "EmptyScene";

        private bool m_SceneHasLoaded;

        protected bool ShouldIgnoreTests => IsPerformanceTest && Application.isEditor || MultiprocessOrchestration.IsUsingUTR(); // todo remove UTR check once we have proper automation

        /// <summary>
        /// Implement this to specify the amount of workers to spawn from your main test runner
        /// TODO there's a good chance this will be refactored with something fancier once we start integrating with bokken
        /// </summary>
        protected abstract int WorkerCount { get; }

        private Scene m_OriginalActiveScene;

        [OneTimeSetUp]
        public virtual void SetupTestSuite()
        {
            if (IsPerformanceTest)
            {
                Assert.Ignore("Ignoring performance tests. Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
            }
            m_OriginalActiveScene = SceneManager.GetActiveScene();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadSceneAsync(BuildMultiprocessTestPlayer.MainSceneName, LoadSceneMode.Additive);
        }

        private bool VerifySceneBeforeLoading(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName.StartsWith("InitTestScene"))
            {
                return false;
            }
            return true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if(scene.name == BuildMultiprocessTestPlayer.MainSceneName)
            {
                SceneManager.SetActiveScene(scene);
            }

            NetworkManager.Singleton.StartHost();
            // Use scene verification to make sure we don't try to synchronize the TestRunner scene
            NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifySceneBeforeLoading;

            m_SceneHasLoaded = true;
        }

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening && m_SceneHasLoaded);

            if (MultiprocessOrchestration.Processes.Count < WorkerCount)
            {
                var numProcessesToCreate = WorkerCount - MultiprocessOrchestration.Processes.Count;
                for (int i = 0; i < numProcessesToCreate; i++)
                {
                    MultiprocessOrchestration.StartWorkerNode(); // will automatically start built player as clients
                }
            }

            var timeOutTime = Time.realtimeSinceStartup + TestCoordinator.MaxWaitTimeoutSec;
            while (NetworkManager.Singleton.ConnectedClients.Count <= WorkerCount)
            {
                yield return new WaitForSeconds(0.2f);

                if (Time.realtimeSinceStartup > timeOutTime)
                {
                    throw new Exception($"waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {WorkerCount}, failing");
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

                MultiprocessOrchestration.KillAllProcesses();
                //TestCoordinator.Instance.CloseRemoteClientRpc();

                NetworkManager.Singleton.StopHost();
                Object.Destroy(NetworkManager.Singleton.gameObject); // making sure we clear everything before reloading our scene
                if(m_OriginalActiveScene.IsValid())
                {
                    SceneManager.SetActiveScene(m_OriginalActiveScene);
                }
                SceneManager.UnloadSceneAsync(BuildMultiprocessTestPlayer.MainSceneName);


            }
        }
    }
}

