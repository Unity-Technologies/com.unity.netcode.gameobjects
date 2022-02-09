using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using NUnit.Framework;
using UnityEngine;
using Unity.Netcode.Transports.UNET;
using Unity.Netcode.MultiprocessRuntimeTests;

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
    public const int PerTestTimeoutSec = 4 * 60; // seconds

    public const float MaxWaitTimeoutSec = 56;
    private const char k_MethodFullNameSplitChar = '@';

    private bool m_ShouldShutdown;
    private float m_TimeSinceLastConnected;
    private float m_TimeSinceLastKeepAlive;

    public static TestCoordinator Instance;

    private Dictionary<ulong, List<float>> m_TestResultsLocal = new Dictionary<ulong, List<float>>(); // this isn't super efficient, but since it's used for signaling around the tests, shouldn't be too bad
    private Dictionary<ulong, bool> m_ClientIsFinished = new Dictionary<ulong, bool>();

    public static List<ulong> AllClientIdsWithResults => Instance.m_TestResultsLocal.Keys.ToList();
    public static List<ulong> AllClientIdsExceptMine => NetworkManager.Singleton.ConnectedClients.Keys.ToList().FindAll(client => client != NetworkManager.Singleton.LocalClientId);
    private string m_ConnectAddress = "127.0.0.1";
    private string m_Port = "3076";

    private Stopwatch m_Stopwatch;
    private int m_ProcessId;

    private void Awake()
    {
        m_ProcessId = Process.GetCurrentProcess().Id;
        string[] cliargList = Environment.GetCommandLineArgs();
        string cliargs = "";
        for (int i = 0; i < cliargList.Length; i++)
        {
            cliargs += " ";
            cliargs += cliargList[i];
        }
        MultiprocessLogger.Log($"Awake - {m_ProcessId} with args: {cliargs}");
        if (Instance != null)
        {
            MultiprocessLogger.LogError("Multiple test coordinator, destroying this instance");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Start()
    {
        m_Stopwatch = Stopwatch.StartNew();
        bool isClient = Environment.GetCommandLineArgs().Any(value => value == MultiprocessOrchestration.IsWorkerArg);
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; ++i)
        {
            string arg = args[i];
            if (arg.Equals("-ip"))
            {
                m_ConnectAddress = args[i + 1];
                MultiprocessLogger.Log($"command line ip was {m_ConnectAddress}");

            }

            if (arg.Equals("-p"))
            {
                m_Port = args[i + 1];
                MultiprocessLogger.Log($"command line port was {m_Port}");
            }
        }
        MultiprocessLogger.Log($"{m_Port}");
        var ushortport = ushort.Parse(m_Port);
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        MultiprocessLogger.Log($"Transport is {transport.ToString()}");
        if (!isClient)
        {
            m_ConnectAddress = "0.0.0.0";
        }

        switch (transport)
        {
            case UNetTransport unetTransport:
                MultiprocessLogger.Log("ERROR - UNetTransport should not be the transport in this case");
                unetTransport.ConnectPort = ushortport;
                unetTransport.ServerListenPort = ushortport;
                if (isClient)
                {
                    MultiprocessLogger.Log($"Setting ConnectAddress to {m_ConnectAddress}");
                    unetTransport.ConnectAddress = m_ConnectAddress;
                }
                break;
            case UnityTransport unityTransport:
                MultiprocessLogger.Log($"Setting unityTransport.ConnectionData.Port {ushortport}, isClient: {isClient}, Address {m_ConnectAddress}");
                unityTransport.ConnectionData.Port = ushortport;
                unityTransport.ConnectionData.Address = m_ConnectAddress;
                break;
            default:
                MultiprocessLogger.LogError($"The transport {transport} has no case");
                break;
        }

        if (isClient)
        {
            MultiprocessLogger.Log($"Starting netcode client on {Environment.MachineName} with pid {m_ProcessId}");
            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
            MultiprocessLogger.Log($"started netcode client {NetworkManager.Singleton.IsConnectedClient}");
        }
        else
        {
            m_TimeSinceLastKeepAlive = Time.time;
        }
        ExecuteStepInContext.InitializeAllSteps();
        MultiprocessLogger.Log($"Start - IsInvoking: {NetworkManager.Singleton.IsInvoking()};\n" +
                $" IsActiveAndEnabled:       {NetworkManager.Singleton.isActiveAndEnabled};\n" +
                $" NetworkTransport.name:    {NetworkManager.NetworkConfig.NetworkTransport.name};\n" +
                $" isConnectedClient:        {NetworkManager.Singleton.IsConnectedClient};\n" +
                $" m_TimeSinceLastKeepAlive: {m_TimeSinceLastKeepAlive}; \n" +
                $"           pid:            {m_ProcessId}");
    }

    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        MultiprocessLogger.Log($"started netcode client {NetworkManager.Singleton.IsConnectedClient}");
    }

    public void Update()
    {
        if (Time.time - m_TimeSinceLastKeepAlive > PerTestTimeoutSec)
        {
            MultiprocessLogger.Log($"{m_ProcessId} Stayed idle too long, quitting: {Time.time} - {m_TimeSinceLastKeepAlive} > {PerTestTimeoutSec}");
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
                MultiprocessLogger.Log($"something wrong happened, was not connected for {Time.time - m_TimeSinceLastConnected} seconds");
                QuitApplication();
                Assert.Fail($"something wrong happened, was not connected for {Time.time - m_TimeSinceLastConnected} seconds");
            }
        }
        else if (m_Stopwatch.ElapsedMilliseconds > 5000)
        {
            m_Stopwatch.Restart();
            MultiprocessLogger.Log($"Update - IsInvoking: {NetworkManager.Singleton.IsInvoking()}" +
                $"IsActiveAndEnabled: {NetworkManager.Singleton.isActiveAndEnabled}" +
                $" NetworkManager.NetworkConfig.NetworkTransport.name {NetworkManager.NetworkConfig.NetworkTransport.name} " +
                $" isConnectedClient: {NetworkManager.Singleton.IsConnectedClient};\n" +
                $" pid: {m_ProcessId}");
        }
    }

    private static void QuitApplication()
    {
        int pid = Process.GetCurrentProcess().Id;
#if UNITY_EDITOR
        MultiprocessLogger.Log($"Setting UnityEditor isPlaying to false for pid {pid}");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        MultiprocessLogger.Log($"Calling Application.Quit for pid {pid}");
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
        MultiprocessLogger.Log("OnEnable - Setting OnClientDisconnectCallback");
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
        MultiprocessLogger.Log("OnEnable - Setting OnClientConnectedCallback");
        NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        MultiprocessLogger.Log($"OnClientConnectedCallback triggered - {obj}");
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

    private static void OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
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
        MultiprocessLogger.Log($"m_TimeSinceLastKeepAlive is {m_TimeSinceLastKeepAlive}");
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

