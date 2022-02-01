using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        protected virtual bool LaunchRemotely => false;

        protected virtual bool IsPerformanceTest => true;
        private string m_Port = "3076"; // TODO This port will need to be reconfigurable
        private const string k_GlobalEmptySceneName = "EmptyScene";

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

        // As an alternative to polling ConnectedClients Count we can store a
        // collection of connected clients as reported by the client connect
        // and client disconnect callbacks
        protected List<ulong> m_ConnectedClientsList;

        [OneTimeSetUp]
        public virtual void SetupTestSuite()
        {
            m_ConnectedClientsList = new List<ulong>();

            MultiprocessLogger.Log("BaseMultiprocessTests - Running SetupTestSuite - OneTimeSetup");
            MultiprocessLogger.Log($"BaseMultiprocessTests - Running SetupTestSuite - LaunchRemotely {LaunchRemotely} MultiprocessOrchestration.ShouldRunMultiMachineTests() {MultiprocessOrchestration.ShouldRunMultiMachineTests()}");
            if (LaunchRemotely && !MultiprocessOrchestration.ShouldRunMultiMachineTests())
            {
                Assert.Ignore($"Ignoring tests that require bokken for multimachine testing since as enableMultiMachineTesting Editor command line option not specified");
            }

            if (IsPerformanceTest)
            {
                Assert.Ignore("Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
            }

            // Build the multiprocess test player
            if (!BuildMultiprocessTestPlayer.DoesBuildInfoExist())
            {
                Assert.Ignore($"Ignoring tests that require a multiprocess testplayer build");
            }

            var currentlyActiveScene = SceneManager.GetActiveScene();
            MultiprocessLogger.Log($"Current Active Scene is {currentlyActiveScene.name}");
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
            MultiprocessLogger.Log("BaseMultiprocessTests - Running SetupTestSuite - OneTimeSetup --- complete");
            MultiprocessLogHandler.Flush();
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
                case UnityTransport unityTransport:
                    unityTransport.ConnectionData.Port = ushortport;
                    MultiprocessLogger.Log($"unityTransport ConnectionData: " +
                        $"{unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}");
                    break;
                default:
                    MultiprocessLogger.LogError($"OnSceneLoaded: Transport is {transport} which is an unaccounted for transport case");
                    break;
            }

            if (scene.name == BuildMultiprocessTestPlayer.MainSceneName)
            {
                MultiprocessLogger.Log($"OnSceneLoaded: setting active scene to {scene.name}");
                SceneManager.SetActiveScene(scene);
            }
            MultiprocessLogger.Log($"OnSceneLoaded: Starting Host {((UnityTransport)transport).ConnectionData.Address}");
            bool didStart = NetworkManager.Singleton.StartHost();
            MultiprocessLogger.Log($"OnSceneLoaded: Host Start Complete with status {didStart}");
            // Use scene verification to make sure we don't try to get clients to synchronize the TestRunner scene
            NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifySceneIsValidForClientsToLoad;
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
            m_HasSceneLoaded = true;
        }

        private void Singleton_OnClientDisconnectCallback(ulong obj)
        {
            MultiprocessLogger.Log($"OnClientDisconnectCallback triggered {obj}");
            m_ConnectedClientsList.Remove(obj);
            MultiprocessLogger.Log($"OnClientDisconnectCallback triggered {obj} current count is {m_ConnectedClientsList.Count}");
        }

        private void Singleton_OnClientConnectedCallback(ulong obj)
        {
            m_ConnectedClientsList.Add(obj);
            MultiprocessLogger.Log($"OnClientConnectedCallback triggered with id:{obj}, new count is {m_ConnectedClientsList.Count}");
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

        [SetUp]
        public void SetUp()
        {
            MultiprocessLogHandler.Flush();
            MultiprocessLogger.Log($"1/3 NUnit Level Setup in Base Class - Connected Clients: {m_ConnectedClientsList.Count}");
            TestContext t1 = TestContext.CurrentContext;
            MultiprocessLogger.Log($"2/3 NUnit Level Setup - FullName: {t1.Test.FullName}");
            var t2 = TestContext.CurrentTestExecutionContext;
            MultiprocessLogger.Log($"3/3 {t2.CurrentTest.FullName}");
            MultiprocessLogHandler.Flush();
        }

        public void DisconnectClients(int previousMessageCounter = 0)
        {
            int messageCounter = 1;
            if (m_ConnectedClientsList.Count > 0)
            {
                MultiprocessLogger.Log($"{previousMessageCounter}... {messageCounter++} There are {m_ConnectedClientsList.Count} connected clients");
                var connectedClients = NetworkManager.Singleton.ConnectedClients;
                MultiprocessLogger.Log($"{previousMessageCounter}... {messageCounter++} NetworkManager count is {connectedClients.Count}");
                var clientsToDisconnect = new List<ulong>();
                foreach (ulong id in connectedClients.Keys)
                {
                    MultiprocessLogger.Log($"{previousMessageCounter}... {messageCounter++} Identified still connected client {id} {connectedClients[id]}");
                    clientsToDisconnect.Add(id);
                }
                foreach (var id in clientsToDisconnect)
                {
                    if (id != 0)
                    {
                        MultiprocessLogger.Log($"{previousMessageCounter}... {messageCounter++} Disconnecting client with id {id}");
                        NetworkManager.Singleton.DisconnectClient(id);
                    }
                }
            }
        }

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            MultiprocessLogger.Log(MultiprocessLogHandler.ReportQueue());
            MultiprocessLogHandler.Flush();
            MultiprocessLogger.Log($"UnitySetup in Base Class - Connected Clients (expected 0): m_ConnectedClientsList:{m_ConnectedClientsList.Count}");
            m_ConnectedClientsList.Clear();
            if ((NetworkManager.Singleton != null) && (NetworkManager.Singleton.ConnectedClients != null))
            {
                MultiprocessLogger.Log($"NetworkManager.Singleton.ConnectedClients:{NetworkManager.Singleton.ConnectedClients.Count}");
            }
            yield return new WaitUntil(() => NetworkManager.Singleton != null);
            yield return new WaitUntil(() => NetworkManager.Singleton.IsServer);
            yield return new WaitUntil(() => NetworkManager.Singleton.IsListening);
            yield return new WaitUntil(() => m_HasSceneLoaded == true);
            // Moved this out of OnSceneLoaded as OnSceneLoaded is a callback from the SceneManager and just wanted to avoid creating
            // processes from within the same callstack/context as the SceneManager.  This will instantiate up to the WorkerCount and
            // then any subsequent calls to Setup if there are already workers it will skip this step
            if (m_ConnectedClientsList.Count < WorkerCount)
            {
                var numProcessesToCreate = WorkerCount - (NetworkManager.Singleton.ConnectedClients.Count - 1);
                if (!LaunchRemotely)
                {
                    for (int i = 1; i <= numProcessesToCreate; i++)
                    {
                        MultiprocessLogger.Log($"Spawning testplayer {i}/{numProcessesToCreate} since connected client count is {NetworkManager.Singleton.ConnectedClients.Count} is less than {WorkerCount} and platformList is null");
                        m_LogPath = MultiprocessOrchestration.StartWorkerNode(); // will automatically start built player as clients
                        MultiprocessLogger.Log($"logPath to new process is {m_LogPath}");
                        MultiprocessLogger.Log($"connected client count is NetworkManager:{NetworkManager.Singleton.ConnectedClients.Count}");
                        MultiprocessLogger.Log($"connected client count is m_ConnectedClientCount: {m_ConnectedClientsList.Count}");
                        MultiprocessLogger.Log(MultiprocessLogHandler.ReportQueue());
                    }
                }
                else
                {
                    var machines = new List<BokkenMachine>();
                    var tasks = new List<Task>();
                    foreach (var platform in platformList)
                    {
                        MultiprocessLogger.Log($"Provisioning platform {platform} if necessary");
                        var provisionTask = Task.Factory.StartNew(() =>
                        {
                            var machine = MultiprocessOrchestration.ProvisionWorkerNode(platform);
                            machines.Add(machine);
                        });
                        MultiprocessLogger.Log($"Task {provisionTask.Id} is for {platform}");
                        tasks.Add(provisionTask);
                        MultiprocessLogger.Log($"Machines list is now : {machines.Count}");
                    }

                    foreach (var task in tasks)
                    {
                        MultiprocessLogger.Log($"Task id {task.Id}");
                        task.Wait();
                    }

                    foreach (var machine in machines)
                    {
                        machine.CheckDirectoryStructure();
                    }

                    MultiprocessLogger.Log($"We are trying to get to {WorkerCount} from {m_ConnectedClientsList.Count}"
                        + $" by launching {machines.Count} new instances");

                    int initialCount = m_ConnectedClientsList.Count;
                    foreach (var machine in machines)
                    {
                        MultiprocessLogger.Log($"ConnectedClient count: {NetworkManager.Singleton.ConnectedClients.Count} , BokkenMachine process count before launch {BokkenMachine.ProcessList.Count}");
                        MultiprocessLogger.Log($"Launching process on remote machine {machine.Name} {machine.Image} {machine.Type}");
                        machine.Launch();
                        MultiprocessLogger.Log($"Launching process complete");
                        // yield return new WaitUntil(() => m_ConnectedClientsList.Count > initialCount);
                        initialCount = m_ConnectedClientsList.Count;
                        MultiprocessLogger.Log($"ConnectedClient count: {m_ConnectedClientsList.Count} , BokkenMachine process count after launch {BokkenMachine.ProcessList.Count}");
                    }
                }
            }
            else
            {
                // Maybe this should be an exception, there doesn't seem to be a legit reason to not have a new client for each test
                MultiprocessLogger.Log($"No need to spawn a new test player as there are already connected clients {NetworkManager.Singleton.ConnectedClients.Count}");
            }
            MultiprocessLogger.Log($"Checking timeout {Time.realtimeSinceStartup} + {TestCoordinator.MaxWaitTimeoutSec}");
            var timeOutTime = Time.realtimeSinceStartup + TestCoordinator.MaxWaitTimeoutSec;
            MultiprocessLogger.Log($"Timeout is now {timeOutTime}");
            MultiprocessLogger.Log($"According to connection listener we have {m_ConnectedClientsList.Count} clients currently connected");
            MultiprocessLogger.Log(MultiprocessLogHandler.ReportQueue());
            int counter = 0;
            while (m_ConnectedClientsList.Count < WorkerCount)
            {
                counter++;
                yield return new WaitForSecondsRealtime(0.7f);
                if (counter % 7 == 0)
                {
                    float beforeYield = Time.realtimeSinceStartup;
                    MultiprocessLogger.Log($"About to call yield return new WaitForSeconds(0.7f); with Time.realtimeSinceStartup {beforeYield} and scale time {Time.timeScale}");
                    yield return new WaitForSecondsRealtime(0.7f);
                    float afterYield = Time.realtimeSinceStartup;
                    MultiprocessLogger.Log($"waiting... until {Time.realtimeSinceStartup} > {timeOutTime} while waiting for {m_ConnectedClientsList.Count} == {WorkerCount}, {afterYield} - {beforeYield} = {afterYield - beforeYield}");
                    if (afterYield - beforeYield < 0.7f)
                    {
                        Thread.Sleep(700);
                    }
                }
                if (Time.realtimeSinceStartup > timeOutTime)
                {
                    MultiprocessLogger.Log($"FAIL - Waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {WorkerCount}, failing");
                    throw new Exception($"FAIL - Waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {WorkerCount}, failing");
                }
            }
            TestCoordinator.Instance.KeepAliveClientRpc();
            MultiprocessLogger.Log($"SUCCESS - Connected client count is {NetworkManager.Singleton.ConnectedClients.Count} and {m_ConnectedClientsList.Count} while waiting for WorkerCount {WorkerCount}");
            MultiprocessLogHandler.Flush();
        }

        [TearDown]
        public virtual void Teardown()
        {
            MultiprocessLogger.Log($" 1/Teardown BaseMultiProcessTests - Teardown : Running teardown");
            MultiprocessLogHandler.Flush();
            TestContext t1 = TestContext.CurrentContext;
            MultiprocessLogger.Log($" 2/Teardown t1.Result.Outcome {t1.Result.Outcome} {t1.Result.Message}");
            var t2 = TestContext.CurrentTestExecutionContext;
            MultiprocessLogger.Log($" 3/Teardown t2.CurrentResult.FullName {t2.CurrentResult.FullName} t2.CurrentResult.ResultState {t2.CurrentResult.ResultState} {t2.CurrentResult.Duration}");
            MultiprocessOrchestration.ClearProcesslist();
            MultiprocessLogger.Log($" 4/Teardown Process List Cleared which should mean all spawned processes should stop");
            if (LaunchRemotely && MultiprocessOrchestration.ShouldRunMultiMachineTests())
            {
                foreach (var process in BokkenMachine.ProcessList)
                {
                    MultiprocessLogger.Log("About to call HasExited on a process");
                    try
                    {
                        if (!process.HasExited)
                        {
                            MultiprocessLogger.Log($"Teardown found an active process {process.ProcessName} {process.Id}");
                        }
                    }
                    catch (InvalidOperationException ioe)
                    {
                        MultiprocessLogger.Log($"HasExited threw an exception {ioe.StackTrace}");
                    }
                }

                MultiprocessLogger.Log("Kill multi process test players");
                MultiprocessOrchestration.KillAllTestPlayersOnRemoteMachines();
            }

            TestCoordinator.Instance.TestRunTeardown();
            MultiprocessLogger.Log("BaseMultiProcessTests - Teardown : Running teardown ... Complete");
            MultiprocessLogger.Log(MultiprocessLogHandler.ReportQueue());
            MultiprocessLogHandler.Flush();
        }


        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            MultiprocessLogger.Log($"1/20 - UnityTearDown - {m_ConnectedClientsList.Count}");
            DisconnectClients();
            yield return new WaitForSeconds(1.0f);
            MultiprocessLogger.Log($"4/20 - UnityTearDown ... end - {m_ConnectedClientsList.Count}");
        }

        [OneTimeTearDown]
        public virtual void TeardownSuite()
        {
            MultiprocessLogger.Log(MultiprocessLogHandler.ReportQueue());
            MultiprocessLogHandler.Flush();
            try
            {
                MultiprocessLogger.Log($"BaseMultiProcessTests - TeardownSuite : One time teardown");
                MultiprocessLogger.Log($"TeardownSuite should have disposed resources");
                MultiprocessLogger.Log($"Should run MultiMachine Tests {MultiprocessOrchestration.ShouldRunMultiMachineTests()}");
                if (MultiprocessOrchestration.ShouldRunMultiMachineTests())
                {
                    BokkenMachine.FetchAllLogFiles();
                }

                MultiprocessLogger.Log($"5/20 - TeardownSuite - ShutdownAllProcesses - launchRemotely {LaunchRemotely}");
                MultiprocessOrchestration.ShutdownAllProcesses(LaunchRemotely);
                MultiprocessLogger.Log($"6/20 - NetworkManager.Singleton.Shutdown");
                if (NetworkManager.Singleton != null)
                {
                    MultiprocessLogger.Log($"7/20 - Shutdown server/host/client " +
                        $"{NetworkManager.Singleton.IsServer}/{NetworkManager.Singleton.IsHost}/{NetworkManager.Singleton.IsClient}");
                    NetworkManager.Singleton.Shutdown();
                    Object.Destroy(NetworkManager.Singleton.gameObject); // making sure we clear everything before reloading our scene
                    MultiprocessLogger.Log($"8/20 - Currently active scene {SceneManager.GetActiveScene().name}");
                    MultiprocessLogger.Log($"9/20 - Original active scene {m_OriginalActiveScene.name}");
                    MultiprocessLogger.Log($"10/20 - m_OriginalActiveScene.IsValid {m_OriginalActiveScene.IsValid()}");
                    if (m_OriginalActiveScene.IsValid())
                    {
                        MultiprocessLogger.Log($"11/20 - Setting the ActiveScene back to Original");
                        SceneManager.SetActiveScene(m_OriginalActiveScene);
                    }
                    else
                    {
                        MultiprocessLogger.Log($"12/20 - m_OriginalActiveScene is not valid so not setting the ActiveScene back to Original, this will probably lead to failures");
                    }
                    MultiprocessLogger.Log($"13/20 - TeardownSuite - Unload {BuildMultiprocessTestPlayer.MainSceneName} ... start ");
                    AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(BuildMultiprocessTestPlayer.MainSceneName);
                    MultiprocessLogger.Log($"14/20 - Unload scene operation status {asyncOperation.isDone} {asyncOperation.progress} ");
                    asyncOperation.completed += AsyncOperation_completed;
                    MultiprocessLogger.Log($"15/20 - Unload scene operation status {asyncOperation.isDone} {asyncOperation.progress} ");
                }
                else
                {
                    MultiprocessLogger.Log($"7/20 - NetworkManager.Singleton was null");
                }
            }
            catch (Exception e)
            {
                MultiprocessLogger.Log($"WARNING: Suiteteardown threw exception which means all subsequent tests will fail {e.Message} {e.StackTrace}");
            }
            MultiprocessLogHandler.Flush();
        }

        private void AsyncOperation_completed(AsyncOperation obj)
        {
            MultiprocessLogger.Log($"16/20 - Unload - UnloadScene {obj.progress} ");
        }
    }
}

