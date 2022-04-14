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
using UnityEngine.Networking.PlayerConnection;
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
        protected virtual int GetWorkerCount()
        {
            platformList = MultiprocessOrchestration.GetRemotePlatformList();

            if (platformList == null)
            {
                m_LaunchRemotely = false;
            }
            else
            {
                m_LaunchRemotely = true;
            }
            return platformList == null ? 2 : platformList.Length;

        }
        protected bool m_HasSceneLoaded = false;
        protected bool m_LaunchRemotely;
        protected virtual bool RunUnityTearDown => true;

        protected virtual bool IsPerformanceTest => false;

        /// <summary>
        /// Implement this to specify the amount of workers to spawn from your main test runner
        /// TODO there's a good chance this will be re-factored with something fancier once we start integrating with bokken
        /// </summary>
        protected string[] platformList { get; set; }

        private const string k_FirstPartOfTestRunnerSceneName = "InitTestScene";

        // Since we want to additively load our BuildMultiprocessTestPlayer.MainSceneName
        // We want to keep a reference to the
        private Scene m_OriginalActiveScene;
        private string m_Port = "3076";

        // As an alternative to polling ConnectedClients Count we can store a
        // collection of connected clients as reported by the client connect
        // and client disconnect callbacks
        protected List<ulong> m_ConnectedClientsList;

        public bool IsSceneLoading = false;

        [OneTimeSetUp]
        public virtual void SetupTestSuite()
        {
            MultiprocessLogger.Log("Running SetupTestSuite - OneTimeSetup");
            m_ConnectedClientsList = new List<ulong>();
            MultiprocessOrchestration.IsPerformanceTest = IsPerformanceTest;
            
            if (IsPerformanceTest)
            {
                // We don't want the UI updating as part of the performance test measurement.
                ThreeDText.IsPerformanceTest = IsPerformanceTest;
                // Assert.Ignore("Performance tests unable to run at this time, see: https://unity-ci.cds.internal.unity3d.com/job/11651103/results");
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
            MultiprocessLogger.Log($"Loading scene {BuildMultiprocessTestPlayer.MainSceneName}");
            IsSceneLoading = true;
            SceneManager.LoadScene(BuildMultiprocessTestPlayer.MainSceneName, LoadSceneMode.Additive);
            MultiprocessLogHandler.Flush();
            MultiprocessLogger.Log("BaseMultiprocessTests - Running SetupTestSuite - OneTimeSetup --- complete");
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
            // MultiprocessLogger.Log($"OnSceneLoaded: Starting Host {((UnityTransport)transport).ConnectionData.Address}");
            bool didStart = NetworkManager.Singleton.StartHost();
            // This isn't accurate but is a hack for the moment
            // TODO: Clean up at next opportunity
            TestCoordinator.ConfigurationType = ConfigurationType.CommandLine;
            MultiprocessLogger.Log($"OnSceneLoaded: Host Start Complete with status {didStart}");
            // Use scene verification to make sure we don't try to get clients to synchronize the TestRunner scene
            NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifySceneIsValidForClientsToLoad;
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
            m_HasSceneLoaded = true;
            IsSceneLoading = false;
        }

        private void Singleton_OnClientDisconnectCallback(ulong obj)
        {
            MultiprocessLogger.Log($"Singleton_OnClientDisconnectCallback in BaseMultiprocessTests triggered {obj} current count is {m_ConnectedClientsList.Count}");
            m_ConnectedClientsList.Remove(obj);
        }

        private void Singleton_OnClientConnectedCallback(ulong obj)
        {
            m_ConnectedClientsList.Add(obj);
            MultiprocessLogger.Log($"Singleton_OnClientConnectedCallback in BaseMultiprocessTests triggered with id:{obj}, new count is {m_ConnectedClientsList.Count}");
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
            MultiprocessLogger.Log($"1/3 NUnit Level Setup in Base Class - Connected Clients: {m_ConnectedClientsList.Count}/{NetworkManager.Singleton.ConnectedClients.Count} GetWorkerCount: {GetWorkerCount()}");
            TestContext t1 = TestContext.CurrentContext;
            MultiprocessLogger.Log($"2/3 NUnit Level Setup - FullName: {t1.Test.FullName}");
            var t2 = TestContext.CurrentTestExecutionContext;
            MultiprocessLogger.Log($"3/3 {t2.CurrentTest.FullName}");
        }

        public List<ulong> DisconnectClients(int messageCounter = 0)
        {
            var clientsToDisconnect = new List<ulong>();
            int localMessageCounter = 1;
            MultiprocessLogger.Log($"{messageCounter}.{(localMessageCounter++) / 10.0f} DisconnectClients - Start");
            MultiprocessLogHandler.Flush();
            var connectedClients = NetworkManager.Singleton.ConnectedClients;
            MultiprocessLogger.Log($"{messageCounter}.{(localMessageCounter++) / 10.0f} DisconnectClients - ConnectedClients.Count {connectedClients.Count}");
            if (m_ConnectedClientsList.Count > 0 || connectedClients.Count > 1)
            {
                MultiprocessLogger.Log(
                    $" {messageCounter}.{localMessageCounter++ / 10.0f} Connected Clients - \n" +
                    $" ListenerCount: {m_ConnectedClientsList.Count}\n" +
                    $" NetworkManagerCount: {connectedClients.Count}\n");

                foreach (ulong id in connectedClients.Keys)
                {
                    if (id != 0)
                    {
                        MultiprocessLogger.Log($" {messageCounter}.{(localMessageCounter++) / 10.0f} Identified still connected client {id} {connectedClients[id]}");
                        clientsToDisconnect.Add(id);
                    }
                }

                int connectedClientsDotCount = NetworkManager.Singleton.ConnectedClients.Count;
                foreach (var id in clientsToDisconnect)
                {
                    if (id != 0)
                    {
                        if (NetworkManager.Singleton != null &&
                            NetworkManager.Singleton.IsServer &&
                            NetworkManager.Singleton.IsListening)
                        {
                            MultiprocessLogger.Log($" {messageCounter}.{(localMessageCounter++) / 10.0f} Disconnecting client with id {id}");
                            NetworkManager.Singleton.DisconnectClient(id);
                            connectedClientsDotCount = NetworkManager.Singleton.ConnectedClients.Count;
                        }
                    }
                }
            }
            return clientsToDisconnect;
        }

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            MultiprocessLogger.Log(MultiprocessLogHandler.ReportQueue());
            MultiprocessLogHandler.Flush();
            int platformCount = platformList == null ? 0 : platformList.Length;
            /*
            MultiprocessLogger.Log($"UnitySetup in Base Class - \n" +
                $" Connected Clients (expected 0): m_ConnectedClientsList:{m_ConnectedClientsList.Count},\n" +
                $" GetWorkerCount() {GetWorkerCount()},\n" +
                $" platformList is : {platformCount}");
            */
            if (RunUnityTearDown)
            {
                m_ConnectedClientsList.Clear();
            }
            else
            {
                TestCoordinator.Instance.KeepAliveClientRpc();
            }
            yield return new WaitUntil(() => NetworkManager.Singleton != null);
            yield return new WaitUntil(() => NetworkManager.Singleton.IsServer);
            yield return new WaitUntil(() => NetworkManager.Singleton.IsListening);
            yield return new WaitUntil(() => m_HasSceneLoaded == true);

            // Need to make sure the host doesn't shutdown while setting up the clients
            TestCoordinator.Instance.KeepAliveOnServer();

            var numProcessesToCreate = GetWorkerCount();

            // Moved this out of OnSceneLoaded as OnSceneLoaded is a callback from the SceneManager and just wanted to avoid creating
            // processes from within the same callstack/context as the SceneManager.  This will instantiate up to the WorkerCount and
            // then any subsequent calls to Setup if there are already workers it will skip this step
            if (m_ConnectedClientsList.Count < numProcessesToCreate && NetworkManager.Singleton.ConnectedClients.Count < numProcessesToCreate + 1)
            {
                if (!m_LaunchRemotely)
                {
                    for (int i = 1; i <= numProcessesToCreate; i++)
                    {
                        MultiprocessLogger.Log($"Locally spawning testplayer {i}/{numProcessesToCreate} since ( connected client count - 1 ) is {NetworkManager.Singleton.ConnectedClients.Count - 1} is less than {GetWorkerCount()} and platformList is null");
                        MultiprocessOrchestration.StartWorkerOnLocalNode(); // will automatically start built player as clients
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
                    }

                    foreach (var task in tasks)
                    {
                        MultiprocessLogger.Log($"Task id {task.Id}");
                        task.Wait();
                    }

                    MultiprocessLogger.Log($"We are trying to get to {numProcessesToCreate} from {m_ConnectedClientsList.Count}"
                        + $" by launching {machines.Count} new instances");

                    int initialCount = m_ConnectedClientsList.Count;
                    foreach (var machine in machines)
                    {
                        if (!machine.IsValid())
                        {
                            MultiprocessLogger.Log("Warning: machine is not valid");
                        }
                        MultiprocessLogger.Log($"ConnectedClient count: {NetworkManager.Singleton.ConnectedClients.Count} , BokkenMachine process count before launch {BokkenMachine.ProcessList.Count}");
                        MultiprocessLogger.Log($"Remotely spawning testplayer on {machine.Name} {machine.Image} {machine.Type} since connected client count is {NetworkManager.Singleton.ConnectedClients.Count} is less than {GetWorkerCount()} and platformList is not null");
                        initialCount = m_ConnectedClientsList.Count;
                        machine.Launch();
                        MultiprocessLogger.Log($"Launching process complete... waiting for {m_ConnectedClientsList.Count} to increase from {initialCount}");
                        yield return new WaitUntil(() => m_ConnectedClientsList.Count > initialCount);
                        MultiprocessLogger.Log($"Done waiting... ConnectedClient count: {m_ConnectedClientsList.Count} , BokkenMachine process count after launch {BokkenMachine.ProcessList.Count}");
                    }
                }
            }
            else
            {
                // Maybe this should be an exception, there doesn't seem to be a legit reason to not have a new client for each test
                MultiprocessLogger.Log($"No need to spawn a new test player as there are already connected clients {NetworkManager.Singleton.ConnectedClients.Count}");
            }
            var timeOutTime = Time.realtimeSinceStartup + TestCoordinator.MaxWaitTimeoutSec;
            int counter = 0;
            while (m_ConnectedClientsList.Count < GetWorkerCount())
            {
                if (NetworkManager.Singleton.ConnectedClients.Count > GetWorkerCount())
                {
                    MultiprocessLogger.Log($"Clients connected based on listeners {m_ConnectedClientsList.Count}, vs NetworkManager {NetworkManager.Singleton.ConnectedClients.Count} WorkerCount: {GetWorkerCount()}");
                    break;
                }
                counter++;
                yield return new WaitForSecondsRealtime(0.7f);
                if (counter % 7 == 0)
                {
                    float beforeYield = Time.realtimeSinceStartup;
                    MultiprocessLogger.Log($"About to call yield return new WaitForSeconds(0.7f); with Time.realtimeSinceStartup {beforeYield} and scale time {Time.timeScale}");
                    yield return new WaitForSecondsRealtime(0.7f);
                    float afterYield = Time.realtimeSinceStartup;
                    if (afterYield - beforeYield < 0.7f)
                    {
                        MultiprocessLogger.Log("yield didn't actually wait 7/10s of a second in realtime so forcing a thread sleep...start");
                        Thread.Sleep(700);
                        MultiprocessLogger.Log("yield didn't actually wait 7/10s of a second in realtime so forcing a thread sleep...done");
                    }
                    afterYield = Time.realtimeSinceStartup;
                    MultiprocessLogger.Log($"waiting... until {Time.realtimeSinceStartup} > {timeOutTime} while waiting for {m_ConnectedClientsList.Count} == {GetWorkerCount()} OR {NetworkManager.Singleton.ConnectedClients.Count - 1} == {GetWorkerCount()}, {afterYield} - {beforeYield} = {afterYield - beforeYield}");
                }
                if (Time.realtimeSinceStartup > timeOutTime)
                {
                    MultiprocessLogger.Log($"FAIL - Waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {GetWorkerCount()}, failing");
                    throw new Exception($"FAIL - Waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {GetWorkerCount()}, failing");
                }
            }
            // Need to make sure the host doesn't shutdown while setting up the clients
            TestCoordinator.Instance.KeepAliveOnServer();
            PlayerConnection.instance.Send(new Guid("8c0c307b-f7fd-4216-8623-35b4b3f55fb6"), new byte[0]);
            MultiprocessLogger.Log($"SUCCESS - Connected client count: NetworkManager {NetworkManager.Singleton.ConnectedClients.Count} Listener: {m_ConnectedClientsList.Count} while waiting for WorkerCount {GetWorkerCount()}");
            MultiprocessLogger.Log(MultiprocessLogHandler.Flush());
            if (NetworkManager.Singleton.ConnectedClients.Count > GetWorkerCount() && m_ConnectedClientsList.Count < GetWorkerCount())
            {
                MultiprocessLogger.Log("Warning: Client connection listener didn't fire... a KeyNotFoundException at discconnect time might follow " +
                    $"{m_ConnectedClientsList.Count} but connected clients has increased" +
                    $" {NetworkManager.Singleton.ConnectedClients.Count}");
                yield return new WaitForSecondsRealtime(0.8f);
            }
            MultiprocessLogger.Log($"UnitySetup in Base Class ... end");
        }

        [TearDown]
        public virtual void Teardown()
        {
            MultiprocessLogger.Log($" 1/20 TearDown - BaseMultiProcessTests - start teardown");
            TestContext t1 = TestContext.CurrentContext;
            
            var t2 = TestContext.CurrentTestExecutionContext;
            TimeSpan duration = DateTime.UtcNow.Subtract(t2.StartTime);
            MultiprocessLogger.Log($" 2/20 TearDown t1.Result.Outcome {t1.Result.Outcome} {t2.CurrentResult.FullName}");
            if (!t1.Result.Outcome.ToString().Equals("Passed"))
            {
                MultiprocessLogger.Log($"2.1/20 {t1.Result.Message} {t1.Result.StackTrace}");
            }
            MultiprocessLogger.Log($" 3/30 TearDown t2.CurrentResult.FullName {t2.CurrentResult.FullName} " +
                $" t2.CurrentResult.ResultState {t2.CurrentResult.ResultState} start: {t2.StartTime.ToLongTimeString()} end: {DateTime.UtcNow.ToLongTimeString()} duration: {duration.TotalSeconds}");

            /*
            DisconnectClients(1);
            MultiprocessOrchestration.ClearProcesslist();
            MultiprocessLogger.Log($" 4/TearDown Process List Cleared which should mean all spawned processes should stop");
            if (m_LaunchRemotely && MultiprocessOrchestration.ShouldRunMultiMachineTests())
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
            }
            TestCoordinator.Instance.TestRunTeardown();
            */
            MultiprocessLogger.Log(" 4/20 - TearDown - BaseMultiProcessTests - Running TearDown ... Complete");
        }


        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            if (RunUnityTearDown)
            {
                MultiprocessLogger.Log($" 5/20 - UnityTearDown - BaseMultiProcessTests - start, calling DisconnectClients");
                string disconnectedClients = "";
                foreach (var clientId in TestCoordinator.NetworkManagerClientDisconnectedCallbackReceived)
                {
                    disconnectedClients += $" {clientId} ";
                }
                MultiprocessLogger.Log($"Before UnityTearDown call to DisconnectClients {disconnectedClients}");
                List<ulong> clientsToDisconnect = DisconnectClients(5);

                // At this point we have already called disconnect from TearDown so we should yield and wait for that disconnect to happen and report on it
                for (int i = 0; i < 10; i++)
                {
                    MultiprocessLogger.Log($" 6.{i / 10}/20 Need to confirm that {GetWorkerCount()} worker was shutdown, listener says {m_ConnectedClientsList.Count} and NetworkManager says: {NetworkManager.Singleton.ConnectedClients.Count}");
                    yield return new WaitForSeconds(1.0f);
                    if (NetworkManager.Singleton.ConnectedClients.Count == 1)
                    {
                        break;
                    }
                }

                foreach (var clientId in clientsToDisconnect)
                {
                    MultiprocessLogger.Log($"In teardown we tried to disconnect: {clientId}");
                }

                disconnectedClients = " - ";
                foreach (var clientId in TestCoordinator.NetworkManagerClientDisconnectedCallbackReceived)
                {
                    disconnectedClients += $" - {clientId} ";
                }
                MultiprocessLogger.Log($"After UnityTearDown call to DisconnectClients {disconnectedClients}");
                // Discovered that even after disconnect the player process doesn't auto-quit so need to force it
                MultiprocessOrchestration.ShutdownAllProcesses(m_LaunchRemotely, 6);
                MultiprocessLogger.Log($" 7/20 - UnityTearDown - Reporting on processes calling bokkenapi");
                if (MultiprocessOrchestration.ShouldRunMultiMachineTests())
                {
                    BokkenMachine.LogProcessListStatus();
                    // MultiprocessOrchestration.KillAllTestPlayersOnRemoteMachines();
                    MultiprocessOrchestration.ShutdownAllProcesses(true, 7);
                }
                MultiprocessLogger.Log(MultiprocessLogHandler.Flush());
                MultiprocessLogger.Log($" 8/20 - UnityTearDown - BaseMultiProcessTests - ... end - m_ConnectedClientsList: {m_ConnectedClientsList.Count} | ConnectedClients.Count: {NetworkManager.Singleton.ConnectedClients.Count}");
            }
            else
            {
                MultiprocessLogger.Log("Not running UnityTearDown");
                foreach (var p in BokkenMachine.ProcessList)
                {
                    // There must be someway to safely figure out if there are errors in the spawned processes
                }
            }
        }

        [OneTimeTearDown]
        public virtual void TeardownSuite()
        {
            MultiprocessLogger.Log(MultiprocessLogHandler.ReportQueue());
            MultiprocessLogHandler.Flush();
            try
            {
                MultiprocessLogger.Log($"3/20 - BaseMultiProcessTests - TeardownSuite : One time teardown - Should run MultiMachine Tests {MultiprocessOrchestration.ShouldRunMultiMachineTests()}");
                if (MultiprocessOrchestration.ShouldRunMultiMachineTests())
                {
                    BokkenMachine.FetchAllLogFiles();
                }

                MultiprocessLogger.Log($"5/20 - TeardownSuite - ShutdownAllProcesses - launchRemotely {m_LaunchRemotely}");
                MultiprocessOrchestration.ShutdownAllProcesses(m_LaunchRemotely, 5);
                MultiprocessLogger.Log($"6/20 - NetworkManager.Singleton.Shutdown");
                if (NetworkManager.Singleton != null)
                {
                    MultiprocessLogger.Log($"7/20 - Shutdown server/host/client " +
                        $"{NetworkManager.Singleton.IsServer}/{NetworkManager.Singleton.IsHost}/{NetworkManager.Singleton.IsClient}");
                    NetworkManager.Singleton.Shutdown();
                    Object.Destroy(NetworkManager.Singleton.gameObject); // making sure we clear everything before reloading our scene
                }
                else if (IsSceneLoading)
                {
                    MultiprocessLogger.Log($"7/20 - Warning: Scene is still loading while we are in teardown, this is bad");
                }
                else
                {
                    MultiprocessLogger.Log($"7/20 - NetworkManager.Singleton was null");
                }
                MultiprocessLogger.Log($"8/20 - Currently active scene name: {SceneManager.GetActiveScene().name}");
                MultiprocessLogger.Log($"9/20 - Original active scene name: {m_OriginalActiveScene.name}");
                MultiprocessLogger.Log($"10/20 - m_OriginalActiveScene.IsValid {m_OriginalActiveScene.IsValid()}");
                if (m_OriginalActiveScene.IsValid())
                {
                    MultiprocessLogger.Log($"11/20 - Setting the ActiveScene back to Original");
                    SceneManager.SetActiveScene(m_OriginalActiveScene);
                    MultiprocessLogger.Log($"12/20 - TeardownSuite - Unload {BuildMultiprocessTestPlayer.MainSceneName} ... start ");
                    AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(BuildMultiprocessTestPlayer.MainSceneName);
                    if (asyncOperation == null)
                    {
                        MultiprocessLogger.Log("13/20 - WARNING - SceneManager.UnloadSceneAsync returned null");
                    }
                    else
                    {
                        asyncOperation.completed += AsyncOperation_completed;
                        MultiprocessLogger.Log($"13/20 - Unload scene operation status {asyncOperation.isDone} {asyncOperation.progress} ");
                    }
                }
                else
                {
                    MultiprocessLogger.Log($"11/20 - m_OriginalActiveScene is not valid so not setting the ActiveScene back to Original, this will probably lead to failures");
                }
            }
            catch (Exception e)
            {
                MultiprocessLogger.Log($"WARNING: Suiteteardown threw exception which means all subsequent tests will fail: \n {e.Message} \n {e.StackTrace}");
            }
            finally
            {
                PlayerConnection.instance.Send(new Guid("8c0c307b-f7fd-4216-8623-35b4b3f55fb6"), new byte[0]);
            }
            MultiprocessLogHandler.Flush();
        }

        private void AsyncOperation_completed(AsyncOperation obj)
        {
            MultiprocessLogger.Log($"16/20 - Unload - UnloadScene {obj.progress} ");
        }
    }
}

