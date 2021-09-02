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

        static private bool s_SceneHasLoaded;

        /// <summary>
        /// Implement this to specify the amount of workers to spawn from your main test runner
        /// TODO there's a good chance this will be refactored with something fancier once we start integrating with bokken
        /// </summary>
        protected abstract int WorkerCount { get; }

        private Scene m_OriginalActiveScene;


        private void SceneUnloaded(Scene scene)
        {
            if (scene.name == BuildMultiprocessTestPlayer.MainSceneName)
            {
                SceneManager.sceneUnloaded -= SceneUnloaded;
                LoadMainTestScene();
            }
        }

        private void LoadMainTestScene()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadSceneAsync(BuildMultiprocessTestPlayer.MainSceneName, LoadSceneMode.Additive);
        }

        [OneTimeSetUp]
        public virtual void SetupTestSuite()
        {
            if (IsPerformanceTest)
            {
                Assert.Ignore("Ignoring performance tests. Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
                // This assert should result in OneTimeSetUp not even running
            }
            Debug.Log("Running OneTimeSetUp ... SetupTestSuite");
            // TestFixtures will run for each TestFixture, w
            if (!s_SceneHasLoaded)
            {
                var shouldUnloadFirst = false;

                var currentlyActiveScene = SceneManager.GetActiveScene();

                if (currentlyActiveScene.name == BuildMultiprocessTestPlayer.MainSceneName)
                {
                    if (m_OriginalActiveScene.IsValid() && currentlyActiveScene.name.StartsWith("InitTestScene"))
                    {
                        SceneManager.SetActiveScene(m_OriginalActiveScene);
                        currentlyActiveScene = SceneManager.GetActiveScene();
                    }
                    shouldUnloadFirst = true;
                }
                else
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        var sceneName = scene.name;
                        if (sceneName == BuildMultiprocessTestPlayer.MainSceneName)
                        {
                            shouldUnloadFirst = true;
                        }
                    }
                }

                if (shouldUnloadFirst)
                {
                    if (currentlyActiveScene.IsValid() && currentlyActiveScene.name.StartsWith("InitTestScene"))
                    {
                        m_OriginalActiveScene = currentlyActiveScene;
                    }
                    SceneManager.sceneUnloaded += SceneUnloaded;
                    SceneManager.UnloadSceneAsync(BuildMultiprocessTestPlayer.MainSceneName);
                }
                else
                {
                    if (currentlyActiveScene.IsValid() && currentlyActiveScene.name.StartsWith("InitTestScene"))
                    {
                        m_OriginalActiveScene = currentlyActiveScene;
                    }
                    LoadMainTestScene();
                }
            }
            else if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
                // Use scene verification to make sure we don't try to synchronize the TestRunner scene
                NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifySceneBeforeLoading;
            }
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

            s_SceneHasLoaded = true;
        }

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening && s_SceneHasLoaded);

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
            TestCoordinator.Instance.TestRunTeardown();
        }

        [OneTimeTearDown]
        public virtual void TeardownSuite()
        {
            Debug.Log("Running OneTimeTearDown ... TeardownSuite");
            TestCoordinator.Instance.CloseRemoteClientRpc();

            NetworkManager.Singleton.StopHost();
            Object.Destroy(NetworkManager.Singleton.gameObject); // making sure we clear everything before reloading our scene
            if(m_OriginalActiveScene.IsValid())
            {
                SceneManager.SetActiveScene(m_OriginalActiveScene);
            }
            SceneManager.UnloadSceneAsync(BuildMultiprocessTestPlayer.MainSceneName);
            s_SceneHasLoaded = false;

            MultiprocessOrchestration.KillAllProcesses();
        }
    }
}

