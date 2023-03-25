using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using NUnit.Framework;
using UnityEngine;
using Unity.Netcode.MultiprocessRuntimeTests;
#if UNITY_UNET_PRESENT
using Unity.Netcode.Transports.UNET;
#endif
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
    public const int PerTestTimeoutSec = 5 * 60; // seconds

    public const float MaxWaitTimeoutSec = 60;
    private const char k_MethodFullNameSplitChar = '@';

    private bool m_ShouldShutdown;
    private float m_TimeSinceLastConnected;
    private float m_TimeSinceLastKeepAlive;

    public static TestCoordinator Instance;

    private Dictionary<ulong, List<float>> m_TestResultsLocal = new Dictionary<ulong, List<float>>(); // this isn't super efficient, but since it's used for signaling around the tests, shouldn't be too bad
    private Dictionary<ulong, bool> m_ClientIsFinished = new Dictionary<ulong, bool>();

    public static List<ulong> AllClientIdsWithResults => Instance.m_TestResultsLocal.Keys.ToList();
    public static List<ulong> AllClientIdsExceptMine => NetworkManager.Singleton.ConnectedClients.Keys.ToList().FindAll(client => client != NetworkManager.Singleton.LocalClientId);

    // Multimachine support
    private static int s_ProcessId;
    public static string Rawgithash;

    private ConfigurationType m_ConfigurationType;
    public ConfigurationType ConfigurationType
    {
        get { return m_ConfigurationType; }
        private set
        {
            if (m_ConfigurationType != value)
            {
                m_ConfigurationType = value;
            }
        }
    }
    private string m_ConnectAddress = "127.0.0.1";
    public static string Port = "7777";
    private bool m_IsClient;

    private void SetConfigurationTypeAndConnect(ConfigurationType type)
    {
        ConfigurationType = type;
        SetAddressAndPort();
        bool startClientResult = NetworkManager.Singleton.StartClient();
        MultiprocessLogger.Log($"Starting client");
    }

    public void Awake()
    {
        enabled = false;
        NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;

        MultiprocessLogger.Log("Awake - Initialize All Steps");
        ExecuteStepInContext.InitializeAllSteps();

        s_ProcessId = Process.GetCurrentProcess().Id;
        ReadGitHashFile();

        // Configuration via command line (supported for many but not all platforms)
        bool isClient = Environment.GetCommandLineArgs().Any(value => value == MultiprocessOrchestration.IsWorkerArg);
        if (isClient)
        {
            MultiprocessLogger.Log("Setting up via command line - client");
            m_IsClient = isClient;
            var cli = new CommandLineProcessor(Environment.GetCommandLineArgs());
            if (Environment.GetCommandLineArgs().Any(value => value == "-ip"))
            {
                m_ConnectAddress = cli.TransportAddress;
            }
            if (Environment.GetCommandLineArgs().Any(value => value == "-p"))
            {
                Port = cli.TransportPort;
            }
            SetConfigurationTypeAndConnect(ConfigurationType.CommandLine);
        }

        if (ConfigurationType == ConfigurationType.Unknown)
        {
            bool isHost = Environment.GetCommandLineArgs().Any(value => value == "host");
            if (isHost)
            {
                MultiprocessLogger.Log("Setting up via command line - host");
                var cli = new CommandLineProcessor(Environment.GetCommandLineArgs());
                ConfigurationType = ConfigurationType.CommandLine;
            }
        }


        // Configuration via configuration file - all platform support but set at build time
        if (ConfigurationType == ConfigurationType.Unknown)
        {
            //TODO: For next PR
        }

        // configuration via WebApi - works on all platforms and is set at run time
        if (ConfigurationType == ConfigurationType.Unknown)
        {
            MultiprocessLogger.Log($"Awake {s_ProcessId} - Calling ConfigureViewWebApi");
            ConfigureViaWebApi();
            MultiprocessLogger.Log($"Awake {s_ProcessId} - Calling ConfigureViewWebApi completed");
        }


        // if we've tried all the configuration types and none of them are correct then we should log it and just go with the default values
        if (ConfigurationType == ConfigurationType.Unknown)
        {
            MultiprocessLogger.Log("Unable to determine configuration for NetworkManager via commandline, webapi or config file");
        }

        if (Instance != null)
        {
            MultiprocessLogger.LogError("Multiple test coordinator, destroying this instance");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private async void ConfigureViaWebApi()
    {
        MultiprocessLogger.Log($"ConfigureViaWebApi - start");
        var jobQueue = await ConfigurationTools.GetRemoteConfig();
        foreach (var job in jobQueue.JobQueueItems)
        {
            if (Rawgithash.Equals(job.GitHash))
            {
                ConfigurationTools.ClaimJobQueueItem(job);
                m_ConnectAddress = job.HostIp;
                m_IsClient = true;
                MultiprocessLogHandler.JobId = job.JobId;
                SetConfigurationTypeAndConnect(ConfigurationType.Remote);
                break;
            }
            else
            {
                MultiprocessLogger.Log($"No match between {Rawgithash} and {job.GitHash}");
            }
        }
        MultiprocessLogger.Log($"ConfigureViaWebApi - end {ConfigurationType}");
    }

    private void ReadGitHashFile()
    {
        Rawgithash = "uninitialized";
        try
        {
            var githash_resource = Resources.Load<TextAsset>("Text/githash");
            if (githash_resource != null)
            {
                Rawgithash = githash_resource.ToString();
                if (!string.IsNullOrEmpty(Rawgithash))
                {
                    Rawgithash = Rawgithash.Trim();
                    MultiprocessLogger.Log($"Rawgithash is {Rawgithash}");
                }
            }
        }
        catch (Exception e)
        {
            MultiprocessLogger.Log($"Exception getting githash resource file: {e.Message}");
        }
    }

    private void SetAddressAndPort()
    {
        MultiprocessLogger.Log($"SetAddressAndPort - {Port} {m_ConnectAddress} {m_IsClient} ");
        var ushortport = ushort.Parse(Port);
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        MultiprocessLogger.Log($"transport is {transport}");
        switch (transport)
        {
#if UNITY_UNET_PRESENT
            case UNetTransport unetTransport:
                unetTransport.ConnectPort = ushortport;
                unetTransport.ServerListenPort = ushortport;
                if (m_IsClient)
                {
                    MultiprocessLogger.Log($"Setting ConnectAddress to {m_ConnectAddress} port {ushortport} isClient: {m_IsClient}");
                    unetTransport.ConnectAddress = m_ConnectAddress;
                }
                break;
#endif
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

    public void Start()
    {
        MultiprocessLogger.Log($"TestCoordinator - Start");
    }

    public void Update()
    {
        if (Time.time - m_TimeSinceLastKeepAlive > PerTestTimeoutSec)
        {
            QuitApplication();
            Assert.Fail("Stayed idle too long");
        }

        if ((IsServer && NetworkManager.Singleton.IsListening) || (IsClient && NetworkManager.Singleton.IsConnectedClient))
        {
            m_TimeSinceLastConnected = Time.time;
        }
        else if (Time.time - m_TimeSinceLastConnected > MaxWaitTimeoutSec || m_ShouldShutdown)
        {
            // Make sure we don't have zombie processes
            MultiprocessLogger.Log($"quitting application, shouldShutdown set to {m_ShouldShutdown}, is listening {NetworkManager.Singleton.IsListening}, is connected client {NetworkManager.Singleton.IsConnectedClient}");
            if (!m_ShouldShutdown)
            {
                QuitApplication();
                Assert.Fail($"something wrong happened, was not connected for {Time.time - m_TimeSinceLastConnected} seconds");
            }
        }
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void TestRunTeardown()
    {
        m_TestResultsLocal.Clear();
    }

    public void OnEnable()
    {
        MultiprocessLogger.Log("OnEnable - Setting OnClientDisconnectCallback");
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    public void OnDisable()
    {
        if (IsSpawned && NetworkObject != null && NetworkObject.NetworkManager != null)
        {
            MultiprocessLogger.Log("OnDisable - Removing OnClientDisconnectCallback");
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }

        base.OnDestroy();
    }

    // Once we are connected, we can run the update method
    public void OnClientConnectedCallback(ulong clientId)
    {
        if (enabled == false)
        {
            MultiprocessLogger.Log($"OnClientConnectedCallback enabling behavior clientId: {clientId} {NetworkManager.Singleton.IsHost}/{NetworkManager.Singleton.IsClient} IsRegistering:{ExecuteStepInContext.IsRegistering}");
            enabled = true;
        }
    }

    private static void OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
        {
            // if disconnect callback is for me or for server, quit, we're done here
            MultiprocessLogger.Log($"received disconnect from {clientId}, quitting");
            QuitApplication();
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
        WriteLogServerRpc($"received RPC from server, client side triggering action ID {actionId} {ExecuteStepInContext.AllActions.Count}");
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

            var foundType = Type.GetType(classToExecute) ?? throw new Exception($"couldn't find {classToExecute}");
            var foundMethod = foundType.GetMethod(staticMethodToExecute, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ?? throw new MissingMethodException($"couldn't find method {staticMethodToExecute}");
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

    [ServerRpc(RequireOwnership = false)]
    public void WriteLogServerRpc(string logMessage, ServerRpcParams receiveParams = default)
    {
        MultiprocessLogger.Log($"[Netcode-Server Sender={receiveParams.Receive.SenderClientId}] {logMessage}");
    }
}

