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

        protected bool ShouldIgnoreTests => IsPerformanceTest && Application.isEditor || !BuildMultiprocessTestPlayer.IsMultiprocessTestPlayerAvailable(); // todo remove UTR check once we have proper automation

        /// <summary>
        /// Implement this to specify the amount of workers to spawn from your main test runner
        /// TODO there's a good chance this will be refactored with something fancier once we start integrating with bokken
        /// </summary>
        protected abstract int WorkerCount { get; }

        [OneTimeSetUp]
        public virtual void SetupTestSuite()
        {
            if (ShouldIgnoreTests)
            {
                if (!BuildMultiprocessTestPlayer.IsMultiprocessTestPlayerAvailable())
                {
                    Assert.Ignore($"Multiprocess test player is not available so cannot run tests: {Application.platform}");
                }
                Assert.Ignore("Ignoring tests that shouldn't run from unity editor. Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
            }

            SceneManager.LoadScene(BuildMultiprocessTestPlayer.MainSceneName, LoadSceneMode.Single);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            NetworkManager.Singleton.StartHost();
            for (int i = 0; i < WorkerCount; i++)
            {
                MultiprocessOrchestration.StartWorkerNode(); // will automatically start built player as clients
            }

            m_SceneHasLoaded = true;
        }

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && m_SceneHasLoaded);

            var startTime = Time.time;
            while (NetworkManager.Singleton.ConnectedClients.Count <= WorkerCount)
            {
                yield return new WaitForSeconds(0.2f);

                if (Time.time - startTime > TestCoordinator.MaxWaitTimeoutSec)
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
                TestCoordinator.Instance.CloseRemoteClientRpc();
                NetworkManager.Singleton.Shutdown();
                Object.Destroy(NetworkManager.Singleton.gameObject); // making sure we clear everything before reloading our scene
                SceneManager.LoadScene(k_GlobalEmptySceneName); // using empty scene to clear our state
            }
        }
    }
}

