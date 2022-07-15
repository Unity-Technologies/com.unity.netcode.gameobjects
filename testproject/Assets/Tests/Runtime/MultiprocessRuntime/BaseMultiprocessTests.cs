using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_UNET_PRESENT
using Unity.Netcode.Transports.UNET;
#endif
using Unity.Netcode.Transports.UTP;


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
        protected string[] platformList { get; set; }
        private List<Process> m_LaunchProcessList = new List<Process>();

        protected int GetWorkerCount()
        {
            platformList = MultiprocessOrchestration.GetRemotePlatformList();
            return platformList == null ? WorkerCount : platformList.Length;
        }
        protected bool m_LaunchRemotely;
        private bool m_HasSceneLoaded = false;
        // TODO: Remove UTR check once we have Multiprocess tests fully working
        protected bool IgnoreMultiprocessTests => MultiprocessOrchestration.ShouldIgnoreUTRTests();

        protected virtual bool IsPerformanceTest => false;

        /// <summary>
        /// Implement this to specify the amount of workers to spawn from your main test runner
        /// Note: If using remote workers, the woorker count will come from the environment variable
        /// </summary>
        protected abstract int WorkerCount { get; }

        private const string k_FirstPartOfTestRunnerSceneName = "InitTestScene";

        // Since we want to additively load our BuildMultiprocessTestPlayer.MainSceneName
        // We want to keep a reference to the
        private Scene m_OriginalActiveScene;

        [OneTimeSetUp]
        public virtual void SetupTestSuite()
        {
            MultiprocessLogger.Log("Running SetupTestSuite - OneTimeSetup");
            MultiprocessOrchestration.IsPerformanceTest = IsPerformanceTest;
            MultiprocessOrchestration.IsHost = true;
            if (IgnoreMultiprocessTests)
            {
                Assert.Ignore("Ignoring tests under UTR. For testing, include the \"-bypassIgnoreUTR\" command line parameter.");
            }

            if (IsPerformanceTest)
            {
                Assert.Ignore("Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
            }
            MultiprocessLogger.Log($"Currently active scene {SceneManager.GetActiveScene().name}");
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
            MultiprocessLogger.Log($"OnSceneLoaded {scene.name}");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (scene.name == BuildMultiprocessTestPlayer.MainSceneName)
            {
                SceneManager.SetActiveScene(scene);
            }

            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            MultiprocessLogger.Log($"transport is {transport}");
            switch (transport)
            {
#if UNITY_UNET_PRESENT
                case UNetTransport unetTransport:
                    unetTransport.ConnectPort = int.Parse(TestCoordinator.Port);
                    unetTransport.ServerListenPort = int.Parse(TestCoordinator.Port);
                    unetTransport.ConnectAddress = "127.0.0.1";
                    MultiprocessLogger.Log($"Setting ConnectAddress to {unetTransport.ConnectAddress} port {unetTransport.ConnectPort}, {unetTransport.ServerListenPort}");

                    break;
#endif
                case UnityTransport unityTransport:
                    unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";
                    unityTransport.ConnectionData.Port = ushort.Parse(TestCoordinator.Port);
                    MultiprocessLogger.Log($"Setting unityTransport.ConnectionData.Port {unityTransport.ConnectionData.ServerListenAddress}:{unityTransport.ConnectionData.Port}");
                    break;
                default:
                    MultiprocessLogger.LogError($"The transport {transport} has no case");
                    break;
            }

            MultiprocessLogger.Log("Starting Host");
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

            MultiprocessLogger.Log(MultiprocessLogHandler.ReportQueue());
            MultiprocessLogHandler.Flush();

            var startTime = Time.time;
            m_LaunchRemotely = MultiprocessOrchestration.IsRemoteOperationEnabled();

            MultiprocessLogger.Log($"Host: " +
                $" and connected client count is {NetworkManager.Singleton.ConnectedClients.Count} " +
                $" and WorkerCount is {GetWorkerCount()} " +
                $" and LaunchRemotely is {m_LaunchRemotely}");

            // Moved this out of OnSceneLoaded as OnSceneLoaded is a callback from the SceneManager and just wanted to avoid creating
            // processes from within the same callstack/context as the SceneManager.  This will instantiate up to the WorkerCount and
            // then any subsequent calls to Setup if there are already workers it will skip this step
            if (!m_LaunchRemotely)
            {
                MultiprocessLogger.Log("Host: UnitySetup on host - m_LaunchRemotely is false");
                if (NetworkManager.Singleton.ConnectedClients.Count - 1 < WorkerCount)
                {
                    var numProcessesToCreate = WorkerCount - (NetworkManager.Singleton.ConnectedClients.Count - 1);
                    for (int i = 1; i <= numProcessesToCreate; i++)
                    {
                        MultiprocessLogger.Log($"Spawning testplayer {i} since connected client count is {NetworkManager.Singleton.ConnectedClients.Count} is less than {WorkerCount} and Number of spawned external players is {MultiprocessOrchestration.ActiveLocalTestprojectCount()} ");
                        string logPath = MultiprocessOrchestration.StartWorkerNode(); // will automatically start built player as clients
                        MultiprocessLogger.Log($"logPath to new process is {logPath}");
                        MultiprocessLogger.Log($"Active Worker Count {MultiprocessOrchestration.ActiveLocalTestprojectCount()} and connected client count is {NetworkManager.Singleton.ConnectedClients.Count}");
                    }
                }
            }
            else
            {
                MultiprocessLogger.Log($"UnitySetup on host - m_LaunchRemotely is true GetWorkerCount:{GetWorkerCount()} NetworkManager.Singleton.ConnectedClients.Count: {NetworkManager.Singleton.ConnectedClients.Count}");

                if (NetworkManager.Singleton.ConnectedClients.Count - 1 < GetWorkerCount())
                {
                    MultiprocessLogger.Log("Connected Client Count is less than Worker Count so try starting");
                    var machines = MultiprocessOrchestration.GetRemoteMachineList();
                    foreach (var machine in machines)
                    {
                        MultiprocessLogger.Log($"Server is posting JobQueueItem {TestCoordinator.Rawgithash}");
                        ConfigurationTools.PostJobQueueItem(TestCoordinator.Rawgithash);
                        MultiprocessLogger.Log($"Server is posting JobQueueItem {TestCoordinator.Rawgithash} ... done");
                        MultiprocessLogger.Log($"Start Worker on Remote Node : {machine.Name} to get worker count to {GetWorkerCount()} from {NetworkManager.Singleton.ConnectedClients.Count - 1}");
                        m_LaunchProcessList.Add(MultiprocessOrchestration.StartWorkersOnRemoteNodes(machine));
                    }
                }
                else
                {
                    MultiprocessLogger.Log("Connected client Count matches Worker Count");
                }
            }

            MultiprocessLogger.Log($"DEBUG: ConnectedClient Count: {NetworkManager.Singleton.ConnectedClients.Count} WorkerCount: {GetWorkerCount()}");
            var lastProcess = m_LaunchProcessList[m_LaunchProcessList.Count - 1];
            MultiprocessLogger.Log($"DEBUG: Lastprocess HasExited: {lastProcess.HasExited}");
            var timeOutTime = Time.realtimeSinceStartup + TestCoordinator.MaxWaitTimeoutSec;
            while (NetworkManager.Singleton.ConnectedClients.Count - 1 < GetWorkerCount())
            {
                yield return new WaitForSeconds(0.2f);

                if (Time.realtimeSinceStartup > timeOutTime)
                {
                    MultiprocessLogger.Log($"DEBUG: {DateTime.Now:T} Waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {GetWorkerCount()}, failing");
                    MultiprocessLogger.Log($"DEBUG: Lastprocess HasExited: {lastProcess.HasExited}");
                    // Can we read the output?
                    MultiprocessLogger.Log($"DEBUG: Reading StandardOutput");
                    string stdout = lastProcess.StandardOutput.ReadToEnd();
                    MultiprocessLogger.Log($"DEBUG: Reading StandardOutput {stdout}");
                    MultiprocessLogger.Log($"DEBUG: Reading StandardError");
                    string stderr = lastProcess.StandardError.ReadToEnd();
                    MultiprocessLogger.Log($"DEBUG: Reading StandardError {stderr}");
                    //TODO: If we have failed to start one or more processes we should do a retry at this point before giving up
                    throw new Exception($" {DateTime.Now:T} Waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {GetWorkerCount()}, failing");
                }
            }
            TestCoordinator.Instance.KeepAliveClientRpc();
            MultiprocessLogger.Log($"Connected client count is {NetworkManager.Singleton.ConnectedClients.Count}");
        }

        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            yield return new WaitForSeconds(0.2f);
            MultiprocessLogger.Log($"m_LaunchProcessList: {m_LaunchProcessList.Count}");
            foreach (var launchProcess in m_LaunchProcessList)
            {
                MultiprocessLogger.Log($"Launched Proocess: HasExited: {launchProcess.HasExited}");
            }
        }

        [TearDown]
        public virtual void Teardown()
        {
            MultiprocessLogger.Log("1 of 5 Running teardown");
            TestContext t1 = TestContext.CurrentContext;
            var t2 = TestContext.CurrentTestExecutionContext;
            TimeSpan duration = DateTime.UtcNow.Subtract(t2.StartTime);
            MultiprocessLogger.Log($"2 of 5 Running tearDown t1.Result.Outcome {t1.Result.Outcome} {t2.CurrentResult.FullName}");
            if (!t1.Result.Outcome.ToString().Equals("Passed"))
            {
                MultiprocessLogger.Log($"3 of 5 {t1.Result.Message} {t1.Result.StackTrace}");
            }
            MultiprocessLogger.Log($"4 of 5 TearDown t2.CurrentResult.FullName {t2.CurrentResult.FullName} " +
                $" t2.CurrentResult.ResultState {t2.CurrentResult.ResultState} start: {t2.StartTime.ToLongTimeString()} end: {DateTime.UtcNow.ToLongTimeString()} duration: {duration.TotalSeconds}");

            if (!IgnoreMultiprocessTests)
            {
                TestCoordinator.Instance.TestRunTeardown();
            }
            MultiprocessLogger.Log("5 of 5");
        }

        [OneTimeTearDown]
        public virtual void TeardownSuite()
        {
            MultiprocessLogger.Log($"TeardownSuite");
            if (!IgnoreMultiprocessTests)
            {
                MultiprocessLogger.Log($"TeardownSuite - NetworkManager.Singleton.Shutdown");
                NetworkManager.Singleton.Shutdown();
                if (MultiprocessOrchestration.IsRemoteOperationEnabled())
                {
                    var machines = MultiprocessOrchestration.GetRemoteMachineList();
                    foreach (var machine in machines)
                    {
                        MultiprocessLogger.Log($"TeardownSuite - KillMPTPlayer {machine.Name}");
                        m_LaunchProcessList.Add(MultiprocessOrchestration.KillRemotePlayer(machine));
                        MultiprocessLogger.Log($"TeardownSuite - KillMPTPlayer {machine.Name} - Wait For Exit");
                        m_LaunchProcessList[m_LaunchProcessList.Count - 1].WaitForExit();
                        MultiprocessLogger.Log($"TeardownSuite - GetMPLogs {machine.Name}");
                        m_LaunchProcessList.Add(MultiprocessOrchestration.GetMPLogs(machine));
                        MultiprocessLogger.Log($"TeardownSuite - GetMPLogs {machine.Name} - Wait For Exit");
                        m_LaunchProcessList[m_LaunchProcessList.Count - 1].WaitForExit();
                    }
                }
                else
                {
                    MultiprocessLogger.Log($"TeardownSuite - IsRemoteOperationEnabled is false");
                    MultiprocessLogger.Log($"TeardownSuite - ShutdownAllProcesses");
                    MultiprocessOrchestration.ShutdownAllLocalTestprojectProcesses();
                }

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

