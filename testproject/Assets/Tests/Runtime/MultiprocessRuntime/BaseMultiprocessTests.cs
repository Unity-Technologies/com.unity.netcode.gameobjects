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
        // TODO: Remove UTR check once we have Multiprocess tests fully working
        protected bool IgnoreMultiprocessTests => MultiprocessOrchestration.ShouldIgnoreUTRTests();


        protected virtual bool IsPerformanceTest => true;

        /// <summary>
        /// Implement this to specify the amount of workers to spawn from your main test runner
        /// TODO there's a good chance this will be re-factored with something fancier once we start integrating with bokken
        /// </summary>
        protected abstract int WorkerCount { get; }

        private const string k_FirstPartOfTestRunnerSceneName = "InitTestScene";

        // Since we want to additively load our BuildMultiprocessTestPlayer.MainSceneName
        // We want to keep a reference to the
        private Scene m_OriginalActiveScene;

        [OneTimeSetUp]
        public virtual void SetupTestSuite()
        {
            MultiProcessLog("Running SetupTestSuite - OneTimeSetup");
            if (IgnoreMultiprocessTests)
            {
                Assert.Ignore("Ignoring tests under UTR. For testing, include the \"-bypassIgnoreUTR\" command line parameter.");
            }

            if (IsPerformanceTest)
            {
                Assert.Ignore("Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
            }
            MultiProcessLog($"Currently active scene {SceneManager.GetActiveScene().name}");
            var currentlyActiveScene = SceneManager.GetActiveScene();

            // Just adding a sanity check here to help with debugging in the event that SetupTestSuite is
            // being invoked and the TestRunner scene has not been set to the active scene yet.
            // This could mean that TeardownSuite wasn't called or SceneManager is not finished unloading
            // or could not unload the BuildMultiprocessTestPlayer.MainSceneName.
            if (!currentlyActiveScene.name.StartsWith(k_FirstPartOfTestRunnerSceneName))
            {
                Debug.LogError($"Expected the currently active scene to begin with ({k_FirstPartOfTestRunnerSceneName}) but currently active scene is {currentlyActiveScene.name}");
            }
            m_OriginalActiveScene = currentlyActiveScene;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(BuildMultiprocessTestPlayer.MainSceneName, LoadSceneMode.Additive);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (scene.name == BuildMultiprocessTestPlayer.MainSceneName)
            {
                SceneManager.SetActiveScene(scene);
            }

            NetworkManager.Singleton.StartHost();

            // Use scene verification to make sure we don't try to get clients to synchronize the TestRunner scene
            NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifySceneIsValidForClientsToLoad;
        }

        /// <summary>
        /// We want to exclude the TestRunner scene on the host-server side so it won't try to tell clients to
        /// synchronize to this scene when they connect (host-server side only for multiprocess)
        /// </summary>
        /// <returns>true - scene is fine to synchronize/inform clients to load and false - scene should not be loaded by clients</returns>
        private bool VerifySceneIsValidForClientsToLoad(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName.StartsWith(k_FirstPartOfTestRunnerSceneName))
            {
                return false;
            }
            return true;
        }

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening);
            var startTime = Time.time;

            // Moved this out of OnSceneLoaded as OnSceneLoaded is a callback from the SceneManager and just wanted to avoid creating
            // processes from within the same callstack/context as the SceneManager.  This will instantiate up to the WorkerCount and
            // then any subsequent calls to Setup if there are already workers it will skip this step
            if (MultiprocessOrchestration.ActiveWorkerCount() < WorkerCount)
            {
                var numProcessesToCreate = WorkerCount - MultiprocessOrchestration.ActiveWorkerCount();
                for (int i = 0; i < numProcessesToCreate; i++)
                {
                    MultiProcessLog($"Spawning testplayer {i} since {MultiprocessOrchestration.ActiveWorkerCount()} is less than {WorkerCount} and connected client count is {NetworkManager.Singleton.ConnectedClients.Count}");
                    MultiprocessOrchestration.StartWorkerNode(); // will automatically start built player as clients
                    yield return new WaitForSeconds(1.0f);
                    MultiProcessLog($"Active Worker Count {MultiprocessOrchestration.ActiveWorkerCount()} is less than {WorkerCount} and connected client count is {NetworkManager.Singleton.ConnectedClients.Count}");
                }
            }
            else
            {
                MultiProcessLog($"No need to spawn a new test player as there are already existing processes {MultiprocessOrchestration.ActiveWorkerCount()}");
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
            MultiProcessLog("Running teardown");
            if (!IgnoreMultiprocessTests)
            {
                TestCoordinator.Instance.TestRunTeardown();
            }
        }

        [OneTimeTearDown]
        public virtual void TeardownSuite()
        {
            MultiProcessLog($"TeardownSuite");
            if (!IgnoreMultiprocessTests)
            {
                MultiProcessLog($"TeardownSuite - ShutdownAllProcesses");
                MultiprocessOrchestration.ShutdownAllProcesses();
                MultiProcessLog($"TeardownSuite - NetworkManager.Singleton.Shutdown");
                NetworkManager.Singleton.Shutdown();
                Object.Destroy(NetworkManager.Singleton.gameObject); // making sure we clear everything before reloading our scene
                MultiProcessLog($"Currently active scene {SceneManager.GetActiveScene().name}");
                MultiProcessLog($"m_OriginalActiveScene.IsValid {m_OriginalActiveScene.IsValid()}");
                if (m_OriginalActiveScene.IsValid())
                {
                    SceneManager.SetActiveScene(m_OriginalActiveScene);
                }
                MultiProcessLog($"TeardownSuite - Unload {BuildMultiprocessTestPlayer.MainSceneName}");
                SceneManager.UnloadSceneAsync(BuildMultiprocessTestPlayer.MainSceneName);
                MultiProcessLog($"TeardownSuite - Unload {BuildMultiprocessTestPlayer.MainSceneName}");
            }
        }

        public static void MultiProcessLog(string msg)
        {
            string dString = DateTime.Now.ToString("G");
            Debug.Log($" - MPLOG - {dString} : {msg}");
        }
    }
}

