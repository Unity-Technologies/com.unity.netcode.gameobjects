using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Unity.Netcode.Transports.UNET;

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
        private string m_Port = "3076"; // TODO This port will need to be reconfigurable
        private const string k_GlobalEmptySceneName = "EmptyScene";

        private bool m_SceneHasLoaded;

        protected bool ShouldIgnoreTests => IsPerformanceTest && Application.isEditor || !MultiprocessOrchestration.IsMultiprocessTestPlayerAvailable();

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
                Assert.Ignore("Ignoring tests that shouldn't run from unity editor. Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
            }
            Debug.Log("Loading Scene");
            SceneManager.LoadScene(BuildMultiprocessTestPlayer.MainSceneName, LoadSceneMode.Single);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            var ushortport = ushort.Parse(m_Port);
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            switch (transport)
            {
                case UNetTransport unetTransport:
                    unetTransport.ConnectPort = ushortport;
                    unetTransport.ServerListenPort = ushortport;
                    break;
            }
            NetworkManager.Singleton.StartHost();
            for (int i = 0; i < WorkerCount; i++)
            {
                Debug.Log($"Starting worker {i} out of {WorkerCount}");
                MultiprocessOrchestration.StartWorkerOnNodes(); // will automatically start built player as clients remotely, if available, otherwise locally
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
                yield return new WaitForSeconds(1.0f);

                if (Time.time - startTime > TestCoordinator.MaxWaitTimeoutSec)
                {
                    throw new Exception($"waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {WorkerCount}, failing");
                }
                Debug.LogWarning($"Connected client count < WorkerCount, {NetworkManager.Singleton.ConnectedClients.Count } < {WorkerCount} ");
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
                Object.Destroy(NetworkManager.Singleton.gameObject); // making sure we clear everything before reloading our scene
                SceneManager.LoadScene(k_GlobalEmptySceneName); // using empty scene to clear our state
            }
        }
    }
}

