using System;
using System.Collections;
using System.Collections.Generic;
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
        private string m_LogPath;
        private bool m_HasSceneLoaded = false;
        // TODO: Remove UTR check once we have Multiprocess tests fully working
        protected bool IgnoreMultiprocessTests => MultiprocessOrchestration.ShouldIgnoreUTRTests();

        protected virtual bool IsPerformanceTest => true;
        private string m_Port = "3076"; // TODO This port will need to be reconfigurable
        private const string k_GlobalEmptySceneName = "EmptyScene";

        private bool m_SceneHasLoaded;

        protected bool ShouldIgnoreTests => IsPerformanceTest && Application.isEditor || !BuildMultiprocessTestPlayer.IsMultiprocessTestPlayerAvailable();


        /// <summary>
        /// Implement this to specify the amount of workers to spawn from your main test runner
        /// TODO there's a good chance this will be re-factored with something fancier once we start integrating with bokken
        /// </summary>
        protected abstract int WorkerCount { get; }
        protected abstract string[] platformList { get; }

        private const string k_FirstPartOfTestRunnerSceneName = "InitTestScene";

        // Since we want to additively load our BuildMultiprocessTestPlayer.MainSceneName
        // We want to keep a reference to the
        private Scene m_OriginalActiveScene;

        [OneTimeSetUp]
        public virtual void SetupTestSuite()
        {
            MultiprocessLogger.Log("Running SetupTestSuite - OneTimeSetup");
            if (IgnoreMultiprocessTests)
            {
                Assert.Ignore("Ignoring tests under UTR. For testing, include the \"-bypassIgnoreUTR\" command line parameter.");
            }

            if (IsPerformanceTest)
            {
                Assert.Ignore("Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
            }
            
            var currentlyActiveScene = SceneManager.GetActiveScene();

            // Just adding a sanity check here to help with debugging in the event that SetupTestSuite is
            // being invoked and the TestRunner scene has not been set to the active scene yet.
            // This could mean that TeardownSuite wasn't called or SceneManager is not finished unloading
            // or could not unload the BuildMultiprocessTestPlayer.MainSceneName.
            if (!currentlyActiveScene.name.StartsWith(k_FirstPartOfTestRunnerSceneName))
            {
                MultiprocessLogger.LogError(
                    $"Expected the currently active scene to begin with ({k_FirstPartOfTestRunnerSceneName}) but" +
                    $" currently active scene is {currentlyActiveScene.name}");
            }
            m_OriginalActiveScene = currentlyActiveScene;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(BuildMultiprocessTestPlayer.MainSceneName, LoadSceneMode.Additive);
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
                default:
                    MultiprocessLogger.LogError($"OnSceneLoaded: Transport is {transport} which is an unaccounted for transport case");
                    break;
            }

            if (scene.name == BuildMultiprocessTestPlayer.MainSceneName)
            {
                SceneManager.SetActiveScene(scene);
            }

            
            NetworkManager.Singleton.StartHost();

            // Use scene verification to make sure we don't try to get clients to synchronize the TestRunner scene
            NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifySceneIsValidForClientsToLoad;

            m_HasSceneLoaded = true;
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
            yield return new WaitUntil(() => NetworkManager.Singleton != null);
            
            yield return new WaitUntil(() => NetworkManager.Singleton.IsServer);
            
            yield return new WaitUntil(() => NetworkManager.Singleton.IsListening);
            
            yield return new WaitUntil(() => m_HasSceneLoaded == true);
            
            var startTime = Time.time;

            MultiprocessLogger.Log($"Active Worker Count is {MultiprocessOrchestration.ActiveWorkerCount()} and connected client count is {NetworkManager.Singleton.ConnectedClients.Count}");
            if (MultiprocessOrchestration.ActiveWorkerCount() + 1 < NetworkManager.Singleton.ConnectedClients.Count)
            {
                MultiprocessLogger.Log("Is this a bad state?");
            }

            // Moved this out of OnSceneLoaded as OnSceneLoaded is a callback from the SceneManager and just wanted to avoid creating
            // processes from within the same callstack/context as the SceneManager.  This will instantiate up to the WorkerCount and
            // then any subsequent calls to Setup if there are already workers it will skip this step
            if (NetworkManager.Singleton.ConnectedClients.Count - 1 < WorkerCount)
            {
                var numProcessesToCreate = WorkerCount - (NetworkManager.Singleton.ConnectedClients.Count - 1);
                if (platformList == null)
                {
                    for (int i = 1; i <= numProcessesToCreate; i++)
                    {                        
                        MultiprocessLogger.Log($"Spawning testplayer {i} since connected client count is {NetworkManager.Singleton.ConnectedClients.Count} is less than {WorkerCount} and Number of spawned external players is {MultiprocessOrchestration.ActiveWorkerCount()} and platformList is null");
                        m_LogPath = MultiprocessOrchestration.StartWorkerNode(); // will automatically start built player as clients
                        MultiprocessLogger.Log($"logPath to new process is {m_LogPath}");
                        MultiprocessLogger.Log($"Active Worker Count {MultiprocessOrchestration.ActiveWorkerCount()} and connected client count is {NetworkManager.Singleton.ConnectedClients.Count}");
                    }
                }
                else
                {
                    var machines = new List<BokkenMachine>();
                    foreach (var platform in platformList)
                    {
                        MultiprocessLogger.Log($"Provisioning platform {platform}");
                        machines.Add(MultiprocessOrchestration.ProvisionWorkerNode(platform));
                        MultiprocessLogger.Log($"Machines list is now : {machines.Count}");
                        foreach (var machine in machines)
                        {
                            MultiprocessLogger.Log($"Machines list is now : {machine.Name}");
                        }
                    }
                    foreach (var machine in machines)
                    {
                        BokkenMachine.KillMultiprocessTestPlayer(machine.PathToJson);
                        MultiprocessLogger.Log($"Lauching process on remot machine {machine.Name} {machine.Image} {machine.Type}");
                        machine.Launch();                        
                        machine.PrintTaskListForMultiprocessTestPlayer();
                    }
                }
            }
            else
            {
                MultiprocessLogger.Log($"No need to spawn a new test player as there are already existing processes {MultiprocessOrchestration.ActiveWorkerCount()} and connected clients {NetworkManager.Singleton.ConnectedClients.Count}");
            }

            var timeOutTime = Time.realtimeSinceStartup + TestCoordinator.MaxWaitTimeoutSec;
            while (NetworkManager.Singleton.ConnectedClients.Count <= WorkerCount)
            {
                yield return new WaitForSeconds(0.2f);

                if (Time.realtimeSinceStartup > timeOutTime)
                {                    
                    throw new Exception($"Waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {WorkerCount}, failing");
                }
            }
            TestCoordinator.Instance.KeepAliveClientRpc();
            MultiprocessLogger.Log($"Active Worker Count {MultiprocessOrchestration.ActiveWorkerCount()} and connected client count is {NetworkManager.Singleton.ConnectedClients.Count}");
        }


        [TearDown]
        public virtual void Teardown()
        {
            MultiprocessLogger.Log("Running teardown");
            if (!IgnoreMultiprocessTests)
            {
                
                TestCoordinator.Instance.TestRunTeardown();
                MultiprocessOrchestration.KillAllTestPlayersOnRemoteMachines();
            }
        }

        [OneTimeTearDown]
        public virtual void TeardownSuite()
        {
            MultiprocessLogger.Log($"TeardownSuite");
            BokkenMachine.DisposeResources();
            MultiprocessLogger.Log($"TeardownSuite should have disposed resources");
            if (!IgnoreMultiprocessTests)
            {
                MultiprocessLogger.Log($"TeardownSuite - ShutdownAllProcesses");
                MultiprocessOrchestration.ShutdownAllProcesses();
                MultiprocessLogger.Log($"TeardownSuite - NetworkManager.Singleton.Shutdown");
                NetworkManager.Singleton.Shutdown();
                Object.Destroy(NetworkManager.Singleton.gameObject); // making sure we clear everything before reloading our scene
                MultiprocessLogger.Log($"Currently active scene {SceneManager.GetActiveScene().name}");
                MultiprocessLogger.Log($"m_OriginalActiveScene.IsValid {m_OriginalActiveScene.IsValid()}");
                if (m_OriginalActiveScene.IsValid())
                {
                    SceneManager.SetActiveScene(m_OriginalActiveScene);
                }
                MultiprocessLogger.Log($"TeardownSuite - Unload {BuildMultiprocessTestPlayer.MainSceneName}");
                SceneManager.UnloadSceneAsync(BuildMultiprocessTestPlayer.MainSceneName);
                MultiprocessLogger.Log($"TeardownSuite - Unload {BuildMultiprocessTestPlayer.MainSceneName}");
            }
        }
    }
}

