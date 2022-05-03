using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Netcode;
using NUnit.Framework;
using UnityEngine;
using Unity.Netcode.Transports.UNET;
using Unity.Netcode.MultiprocessRuntimeTests;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// TestCoordinator
/// Used for coordinating multiprocess end to end tests. Used to call RPCs on other nodes and gather results
/// This is needed to coordinate server and client execution steps. The current remote player test runner hardcodes test
/// to run in a bootstrap scene before launching the player and doesn't call each tests individually. There's not opportunity
/// to coordinate test execution between client and server with that model.
/// The only per tests communication already existing is to get the results per test as they are running
/// With this test coordinator, it's not possible to start a main test node with the test runner and have that server start other worker nodes
/// on which to execute client tests. We use netcode as both a test framework and as the target of our performance tests.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class TestCoordinator : NetworkBehaviour
{
    public static TestCoordinator Instance;

    public const int PerTestTimeoutSec = 3 * 60; // seconds

    public const float MaxWaitTimeoutSec = 56;

    public static ConfigurationType ConfigurationType;

    private const char k_MethodFullNameSplitChar = '@';

    private bool m_ShouldShutdown;
    private float m_TimeSinceLastConnected;
    private float m_TimeSinceLastKeepAlive;
    private bool m_IsClient;

    private Dictionary<ulong, List<float>> m_TestResultsLocal = new Dictionary<ulong, List<float>>(); // this isn't super efficient, but since it's used for signaling around the tests, shouldn't be too bad
    private Dictionary<ulong, bool> m_ClientIsFinished = new Dictionary<ulong, bool>();

    public static List<ulong> AllClientIdsWithResults => Instance.m_TestResultsLocal.Keys.ToList();
    public static List<ulong> AllClientIdsExceptMine => NetworkManager.Singleton.ConnectedClients.Keys.ToList().FindAll(client => client != NetworkManager.Singleton.LocalClientId);
    public static List<ulong> NetworkManagerClientConnectedCallbackReceived = new List<ulong>();
    public static List<ulong> NetworkManagerClientDisconnectedCallbackReceived = new List<ulong>();

    private string m_ConnectAddress = "127.0.0.1";
    private string m_Port = "3076";

    private int m_NumberOfCallsToUpdate;
    private List<float> m_UpdateDeltaTime;
    private int m_NumberOfCallsToFixedUpdate;
    private List<float> m_FixedUpdateDeltaTime;
    private Stopwatch m_Stopwatch;
    private static int s_ProcessId;
    public static string Rawgithash;

    static TestCoordinator()
    {
        ConfigurationType = ConfigurationType.Unknown;
        Rawgithash = "x";
    }

    private void Awake()
    {
        MultiprocessLogger.Log("TestCoordinator - Awake");

        s_ProcessId = Process.GetCurrentProcess().Id;
        MultiprocessLogger.Log($"Awake - {s_ProcessId}");
        string[] cliargList = Environment.GetCommandLineArgs();
        string cliargs = "";
        for (int i = 0; i < cliargList.Length; i++)
        {
            cliargs += " ";
            cliargs += cliargList[i];
        }

        MultiprocessLogger.Log("Trying to read githash file");
        
        try
        {
            var githash_resource = Resources.Load<TextAsset>("Text/githash");
            if (githash_resource != null)
            {
                Rawgithash = githash_resource.ToString();
                if (!string.IsNullOrEmpty(Rawgithash))
                {
                    Rawgithash = Rawgithash.Trim();
                }
            }
        }
        catch (Exception e)
        {
            MultiprocessLogger.Log($"Exception getting githash resource file: {e.Message}");
        }
        MultiprocessLogger.Log($"Awake - {s_ProcessId} Trying to read githash file: {Rawgithash}");

        if (ConfigurationType == ConfigurationType.Unknown)
        {
            MultiprocessLogger.Log("Try to get config data from server");
            var jobQueue = MultiprocessLogHandler.GetRemoteConfig();
            MultiprocessLogger.Log("Try to get config data from server...done");
            foreach (var job in jobQueue.JobQueueItems)
            {
                MultiprocessLogger.Log($"{Rawgithash} compared to {job.GitHash} if matches use hostIp {job.HostIp} {job.PlatformId} {job.TransportName} {job.CreatedBy}");
                if (Rawgithash.Equals(job.GitHash))
                {
                    MultiprocessLogger.Log($"GitHas Match!");
                    MultiprocessLogHandler.ClaimJobQueueItem(job);
                    MultiprocessLogger.Log($"Claimed job, setting Host Ip to {job.HostIp}");
                    m_ConnectAddress = job.HostIp;
                    m_IsClient = true;
                    ConfigurationType = ConfigurationType.Remote;
                    break;
                }
                else
                {
                    MultiprocessLogger.Log($"No match between {Rawgithash} and {job.GitHash}");
                }
            }
        }

        if (ConfigurationType == ConfigurationType.Unknown)
        {
            try
            {
                MultiprocessLogger.Log($"Awake - {s_ProcessId} Trying to read remoteConfig resource");
                var remoteConfig = Resources.Load<TextAsset>("Text/remoteConfig").ToString();
                if (!string.IsNullOrEmpty(remoteConfig))
                {
                    MultiprocessLogger.Log($"Awake - {s_ProcessId} - remoteConfig resource is {remoteConfig}");
                    RemoteConfiguration rc = JsonUtility.FromJson<RemoteConfiguration>(remoteConfig);
                    MultiprocessLogger.Log("Checking remoteconfig object");
                    MultiprocessLogger.Log(rc.IpAddressOfHost);
                    m_ConnectAddress = rc.IpAddressOfHost;
                    ConfigurationType = ConfigurationType.ResourceFile;
                    if (rc.OperationMode.Equals("client"))
                    {
                        m_IsClient = true;
                    }
                }
                else
                {
                    MultiprocessLogger.Log($"Awake - {s_ProcessId} remoteConfigFile was nullOrEmpty");
                }
            }
            catch (Exception remoteConfigReadException)
            {
                MultiprocessLogger.Log($"Awake - {s_ProcessId} Exception reading remoteConfig {remoteConfigReadException.Message}");
            }
        }

        MultiprocessLogger.Log($"Awake - {s_ProcessId} with args: {cliargs} at git hash {Rawgithash}");
        if (Instance != null)
        {
            MultiprocessLogger.LogError("Multiple test coordinator, destroying this instance");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        /*
        JobQueueItemArray jobQueueItems = MultiprocessLogHandler.GetRemoteConfig();
        if (jobQueueItems != null && jobQueueItems.jobQueueItems != null)
        {
            MultiprocessLogger.Log($"Remote configuration data {jobQueueItems.jobQueueItems.Count}");
            foreach (var jobItem in jobQueueItems.jobQueueItems)
            {
                MultiprocessLogger.Log($"JobQueueItem is - {jobItem.platform} {jobItem.githash} {jobItem.jobid}");
            }
        }
        else
        {
            MultiprocessLogger.Log($"Remote configuration data returned null");
        }
        */

    }

    public void Start()
    {
        MultiprocessLogger.Log($"TestCoordinator {s_ProcessId} - Start {Application.platform} - m_IsClient {m_IsClient}");
        m_Stopwatch = Stopwatch.StartNew();
        m_NumberOfCallsToUpdate = 0;
        m_NumberOfCallsToFixedUpdate = 0;
        m_FixedUpdateDeltaTime = new List<float>();
        m_UpdateDeltaTime = new List<float>();
        if (ConfigurationType == ConfigurationType.CommandLine ||
            ConfigurationType == ConfigurationType.Unknown)
        {
            m_IsClient = Environment.GetCommandLineArgs().Any(value => value == MultiprocessOrchestration.IsWorkerArg);
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (arg.Equals("-ip"))
                {
                    m_ConnectAddress = args[i + 1];
                }

                if (arg.Equals("-p"))
                {
                    m_Port = args[i + 1];
                }

                if (arg.Equals("-jobid"))
                {
                    string sJobId = args[i + 1];
                    long jobId;
                    if (!long.TryParse(sJobId, out jobId))
                    {
                        jobId = -2;
                    }
                    MultiprocessLogHandler.JobId = jobId;
                }
                if (arg.Equals("-testname"))
                {
                    string testname = args[i + 1];
                    MultiprocessLogHandler.TestName = testname;
                }
            }
        }
        
        var ushortport = ushort.Parse(m_Port);
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        
        if (!m_IsClient)
        {
            m_ConnectAddress = "0.0.0.0";
        }

        MultiprocessLogger.Log($"Transport is {transport.ToString()} {m_ConnectAddress} {ushortport}");

        try
        {
            switch (transport)
            {
                case UNetTransport unetTransport:
                    unetTransport.ConnectPort = ushortport;
                    unetTransport.ServerListenPort = ushortport;
                    if (m_IsClient)
                    {
                        MultiprocessLogger.Log($"Setting ConnectAddress to {m_ConnectAddress} port {ushortport} isClient: {m_IsClient}");
                        unetTransport.ConnectAddress = m_ConnectAddress;
                    }
                    break;
                case UnityTransport unityTransport:
                    MultiprocessLogger.Log($"Setting unityTransport.ConnectionData.Port {ushortport}, isClient: {m_IsClient}, Address {m_ConnectAddress}");
                    unityTransport.ConnectionData.Port = ushortport;
                    unityTransport.ConnectionData.Address = m_ConnectAddress;
                    break;
                default:
                    MultiprocessLogger.LogError($"The transport {transport} has no case");
                    break;
            }
        }
        catch (Exception e)
        {
            MultiprocessLogger.Log($"Exception in switch/case for transport {transport} {e.Message}");
        }

        // Let's set the target framerate to see if we get more predictable results
        // Setting the targetFrameRate to 5 should make for a lower load on the display adapter
        // as well as the CPU so that the resources aren't being consumed in order to try and keep
        // up: TODO: Make this a parameter so we can tune it.
        QualitySettings.vSyncCount = 0;
        if (IsServer)
        {
            Application.targetFrameRate = 15;
        }
        else
        {
            Application.targetFrameRate = 5;
        }

        if (m_IsClient)
        {
            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
            LogInformation($"started netcode client, isConnected: {NetworkManager.Singleton.IsConnectedClient}, with pid {s_ProcessId}");
        }
        else
        {
            m_TimeSinceLastKeepAlive = Time.time;
        }
        ExecuteStepInContext.InitializeAllSteps();
    }

    private void Singleton_OnClientDisconnectCallback(ulong clientId)
    {
        LogInformation($"Singleton_OnClientDisconnectCallback in TestCoordinator triggered {clientId}");
        if (!IsServer)
        {
            // if disconnect callback is for me or for server, quit, we're done here
            QuitApplication($"received disconnect from {clientId}, quitting pid {s_ProcessId} since we are not the server");
        }
    }

    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        LogInformation($"Singleton_OnClientConnectedCallback in TestCoordinator triggered {obj}");
    }

    public void FixedUpdate()
    {
        float deltaTime = Time.deltaTime;
        m_NumberOfCallsToFixedUpdate++;
        m_FixedUpdateDeltaTime.Add(deltaTime);
        if (deltaTime > 0.4f)
        {
            LogInformation($"FixedUpdate - {s_ProcessId} Count: {m_NumberOfCallsToFixedUpdate}; Time.deltaTime: {deltaTime}; Average: {m_FixedUpdateDeltaTime.Average()}");
        }
    }

    public void Update()
    {
        float deltaTime = Time.deltaTime;
        m_NumberOfCallsToUpdate++;
        m_UpdateDeltaTime.Add(deltaTime);

        if (!IsServer)
        {
            if (m_Stopwatch.ElapsedMilliseconds > 3500)
            {
                m_Stopwatch.Restart();
                LogInformation($"Update - {s_ProcessId} Count: {m_NumberOfCallsToUpdate}; Time.deltaTime: {deltaTime}; Average {m_UpdateDeltaTime.Average()}");
            }
        }

        if (Time.time - m_TimeSinceLastKeepAlive > PerTestTimeoutSec)
        {
            LogInformation($"Update - {s_ProcessId} - Exceeded PerTestTimeoutSec");
            QuitApplication($"{s_ProcessId} Stayed idle too long, quitting: {Time.time} - {m_TimeSinceLastKeepAlive} > {PerTestTimeoutSec}");
            Assert.Fail("Stayed idle too long");
        }

        if ((IsServer && NetworkManager.Singleton.IsListening) || (IsClient && NetworkManager.Singleton.IsConnectedClient))
        {
            m_TimeSinceLastConnected = Time.time;
        }
        else if (Time.time - m_TimeSinceLastConnected > MaxWaitTimeoutSec || m_ShouldShutdown)
        {
            // Make sure we don't have zombie processes
            LogInformation($"Update - {s_ProcessId} - quitting application, shouldShutdown set to {m_ShouldShutdown}, is listening {NetworkManager.Singleton.IsListening}, is connected client {NetworkManager.Singleton.IsConnectedClient}");
            if (!m_ShouldShutdown)
            {
                QuitApplication($"something wrong happened, was not connected for {Time.time - m_TimeSinceLastConnected} seconds");
                // Assert.Fail($"something wrong happened, was not connected for {Time.time - m_TimeSinceLastConnected} seconds");
            }
        }
    }

    private void LogInformation(string extraMessage = "")
    {
        MultiprocessLogger.Log($"\n" +
                $"NetworkTransport.name {NetworkManager.NetworkConfig.NetworkTransport.name};" +
                $" isConnectedClient: {NetworkManager.Singleton.IsConnectedClient}; " +
                $" isClient: {m_IsClient}/{IsClient}; " +
                $" isServer: {IsServer};\n " +
                $" platform: {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture};" +
                $" pid: {s_ProcessId};" +
                $" framerate: {Application.targetFrameRate} , vsynccount: {QualitySettings.vSyncCount}\n" + 
                $" {extraMessage}");
    }

    private static void QuitApplication(string reason)
    {
#if UNITY_EDITOR
        MultiprocessLogger.Log($"Setting UnityEditor isPlaying to false for pid {s_ProcessId} because {reason}");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        MultiprocessLogger.Log($"Calling Application.Quit for pid {s_ProcessId} because: {reason}");
        Application.Quit();
#endif
    }

    public void TestRunTeardown()
    {
        MultiprocessLogger.Log("TestCoordinator - TestRunTearDown");
        m_TestResultsLocal.Clear();
        MultiprocessLogger.Log($"TestCoordinator - TestRunTearDown... Done clearing m_TestResultsLocal, count: {m_TestResultsLocal.Count}");
    }

    public void OnEnable()
    {
        NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        LogInformation($"NetworkManager_OnClientConnectedCallback triggered - {obj}");
        NetworkManagerClientConnectedCallbackReceived.Add(obj);
    }

    public void OnDisable()
    {
        if (IsSpawned && NetworkObject != null && NetworkObject.NetworkManager != null)
        {
            MultiprocessLogger.Log("OnDisable - Removing OnClientDisconnectCallback");
            NetworkManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
        }

        base.OnDestroy();
    }

    public string GetConnectionAddress()
    {
        return m_ConnectAddress;
    }

    private static void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        MultiprocessLogger.Log($"NetworkManager_OnClientDisconnectCallback triggered - {clientId}");
        NetworkManagerClientDisconnectedCallbackReceived.Add(clientId);
        if (clientId == NetworkManager.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
        {
            // if disconnect callback is for me or for server, quit, we're done here
            QuitApplication($"received disconnect from {clientId}, quitting pid {s_ProcessId}");
        }
        else
        {
            MultiprocessLogger.Log($"Not quitting application on client dissconnect received because " +
                $" {clientId} does not equal {NetworkManager.Singleton.LocalClientId} && " +
                $" {clientId} does not equal {NetworkManager.ServerClientId}");
        }
    }

    private static string GetMethodInfo(Action<byte[]> method)
    {
        return $"{method.Method.DeclaringType.FullName}{k_MethodFullNameSplitChar}{method.Method.Name}";
    }

    private static string GetMethodInfo(Action method)
    {
        return $"{method.Method.DeclaringType.FullName}{k_MethodFullNameSplitChar}{method.Method.Name}";
    }

    public static IEnumerable<(ulong clientId, float result)> ConsumeCurrentResult()
    {
        foreach (var kv in Instance.m_TestResultsLocal)
        {
            while (kv.Value.Count > 0)
            {
                var toReturn = (kv.Key, kv.Value[0]);
                kv.Value.RemoveAt(0);
                yield return toReturn;
            }
        }
    }

    public static IEnumerable<float> ConsumeCurrentResult(ulong clientId)
    {
        var allResults = Instance.m_TestResultsLocal[clientId];
        while (allResults.Count > 0)
        {
            var toReturn = allResults[0];
            allResults.RemoveAt(0);
            yield return toReturn;
        }
    }

    public static float PeekLatestResult(ulong clientId)
    {
        if (Instance.m_TestResultsLocal.ContainsKey(clientId) && Instance.m_TestResultsLocal[clientId].Count > 0)
        {
            return Instance.m_TestResultsLocal[clientId].Last();
        }

        return float.NaN;
    }

    /// <summary>
    /// Returns appropriate lambda according to parameters
    /// Includes time check to make sure this times out
    /// </summary>
    /// <param name="useTimeoutException"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Func<bool> ResultIsSet(bool useTimeoutException = true)
    {
        var startWaitTime = Time.time;
        return () =>
        {
            if (Time.time - startWaitTime > MaxWaitTimeoutSec)
            {
                if (useTimeoutException)
                {
                    throw new Exception($"timeout while waiting for results, didn't get results for {Time.time - startWaitTime} seconds");
                }

                return true;
            }

            foreach (var clientIdAndTestResultList in Instance.m_TestResultsLocal)
            {
                if (clientIdAndTestResultList.Value.Count > 0)
                {
                    return true;
                }
            }

            return false;
        };
    }

    public static Func<bool> ConsumeClientIsFinished(ulong clientId, bool useTimeoutException = true)
    {
        var startWaitTime = Time.time;
        return () =>
        {
            if (Time.time - startWaitTime > MaxWaitTimeoutSec)
            {
                if (useTimeoutException)
                {
                    throw new Exception($"timeout while waiting for client finished, didn't get results for {Time.time - startWaitTime} seconds");
                }
                else
                {
                    return true;
                }
            }

            if (Instance.m_ClientIsFinished.ContainsKey(clientId) && Instance.m_ClientIsFinished[clientId])
            {
                Instance.m_ClientIsFinished[clientId] = false; // consume
                return true;
            }

            return false;
        };
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClientFinishedServerRpc(ServerRpcParams p = default)
    {
        // signal from clients to the server to say the client is done with it's task
        m_ClientIsFinished[p.Receive.SenderClientId] = true;
    }

    public void InvokeFromMethodActionRpc(Action<byte[]> methodInfo, params byte[] args)
    {
        var methodInfoString = GetMethodInfo(methodInfo);
        InvokeFromMethodNameClientRpc(methodInfoString, args, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = AllClientIdsExceptMine.ToArray() } });
    }

    public void InvokeFromMethodActionRpc(Action methodInfo)
    {
        var methodInfoString = GetMethodInfo(methodInfo);
        InvokeFromMethodNameClientRpc(methodInfoString, null, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = AllClientIdsExceptMine.ToArray() } });
    }

    [ClientRpc]
    public void TriggerActionIdClientRpc(string actionId, byte[] args, bool ignoreException, ClientRpcParams clientRpcParams = default)
    {
        MultiprocessLogger.Log($"received RPC from server, client side triggering action ID {actionId}");
        try
        {
            ExecuteStepInContext.AllActions[actionId].Invoke(args);
        }
        catch (Exception e)
        {
            WriteErrorServerRpc(e.ToString());

            if (!ignoreException)
            {
                throw;
            }
            else
            {
                Instance.ClientFinishedServerRpc();
            }
        }
    }

    [ClientRpc]
    public void InvokeFromMethodNameClientRpc(string methodInfoString, byte[] args, ClientRpcParams clientRpcParams = default)
    {
        try
        {
            var split = methodInfoString.Split(k_MethodFullNameSplitChar);
            var (classToExecute, staticMethodToExecute) = (split[0], split[1]);

            var foundType = Type.GetType(classToExecute);
            if (foundType == null)
            {
                throw new Exception($"couldn't find {classToExecute}");
            }

            var foundMethod = foundType.GetMethod(staticMethodToExecute, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (foundMethod == null)
            {
                throw new MissingMethodException($"couldn't find method {staticMethodToExecute}");
            }

            foundMethod.Invoke(null, args != null ? new object[] { args } : null);
        }
        catch (Exception e)
        {
            WriteErrorServerRpc(e.ToString());
            throw;
        }
    }

    [ClientRpc]
    public void CloseRemoteClientRpc()
    {
        try
        {
            MultiprocessLogger.Log($"Shutdown server/host/client {NetworkManager.Singleton.IsServer}/{NetworkManager.Singleton.IsHost}/{NetworkManager.Singleton.IsClient}");
            NetworkManager.Singleton.Shutdown();
            m_ShouldShutdown = true; // wait until isConnectedClient is false to run Application Quit in next update
            MultiprocessLogger.Log("Quitting player cleanly");
            Application.Quit();
        }
        catch (Exception e)
        {
            WriteErrorServerRpc(e.ToString());
            throw;
        }
    }

    public void KeepAliveOnServer()
    {
        m_TimeSinceLastKeepAlive = Time.time;
        MultiprocessLogger.Log($"KeepAliveOnServer - m_TimeSinceLastKeepAlive is {m_TimeSinceLastKeepAlive}");
    }

    [ClientRpc]
    public void KeepAliveClientRpc()
    {
        m_TimeSinceLastKeepAlive = Time.time;
    }

    [ServerRpc(RequireOwnership = false)]
    public void WriteTestResultsServerRpc(float result, ServerRpcParams receiveParams = default)
    {
        var senderId = receiveParams.Receive.SenderClientId;
        MultiprocessLogger.Log($"Server received result [{result}] from sender [{senderId}]");
        if (!m_TestResultsLocal.ContainsKey(senderId))
        {
            m_TestResultsLocal[senderId] = new List<float>();
        }

        m_TestResultsLocal[senderId].Add(result);
    }

    /// <summary>
    /// Use this to communicate client-side errors for server-side logging using the MultiprocessLogger.
    /// </summary>
    /// <remarks>
    /// Use <see cref="NetworkLog.LogErrorServer"/> to log server-side without MultiprocessLogger formatting.
    /// </remarks>
    [ServerRpc(RequireOwnership = false)]
    public void WriteErrorServerRpc(string errorMessage, ServerRpcParams receiveParams = default)
    {
        MultiprocessLogger.LogError($"[Netcode-Server Sender={receiveParams.Receive.SenderClientId}] {errorMessage}");
    }
}

