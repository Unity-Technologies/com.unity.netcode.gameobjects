using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MLAPI;
using MLAPI.Configuration;
using MLAPI.Logging;
using MLAPI.Messaging;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

/// <summary>
/// TestCoordinator
/// Used for coordinating multiprocess end to end tests. Used to call RPCs on other nodes and gather results
/// This is needed to coordinate server and client execution steps. The current remote player test runner hardcodes test
/// to run in a bootstrap scene before launching the player and doesn't call each tests individually. There's not opportunity
/// to coordinate test execution between client and server with that model.
/// The only per tests communication already existing is to get the results per test as they are running
/// With this test coordinator, it's not possible to start a main test node with the test runner and have that server start other worker nodes
/// on which to execute client tests. We use MLAPI as both a test framework and as the target of our performance tests.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class TestCoordinator : NetworkBehaviour
{
    public const int perTestTimeout = 5 * 60; // seconds

    public const float maxWaitTimeout = 20;
    private const char k_MethodFullNameSplitChar = '@';

    public bool isRegistering;
    public bool hasRegistered;
    private bool m_ShouldShutdown;
    private float m_TimeSinceLastConnected;
    private float m_TimeSinceLastKeepAlive;

    public static TestCoordinator Instance;

    private Dictionary<ulong, List<float>> m_TestResultsLocal = new Dictionary<ulong, List<float>>(); // this isn't super efficient, but since it's used for signaling around the tests, shouldn't be too bad
    private Dictionary<ulong, bool> m_ClientIsFinished = new Dictionary<ulong, bool>();

    public static Dictionary<ulong, List<float>>.KeyCollection AllClientIdsWithResults => Instance.m_TestResultsLocal.Keys;
    public static List<ulong> AllClientIdExceptMine => NetworkManager.Singleton.ConnectedClients.Keys.ToList().FindAll(client => client != NetworkManager.Singleton.LocalClientId);
    private List<object> m_AllClientTestInstances = new List<object>(); // to keep an instance for each tests, so captured context in each step is kept

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Multiple test coordinator! destroying self");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Start()
    {
        bool isClient = Environment.GetCommandLineArgs().Any(value => value == MultiprocessOrchestration.isWorkerArg);
        if (isClient)
        {
            Debug.Log("starting MLAPI client");
            NetworkManager.Singleton.StartClient();
        }

        NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;

        // registering magically all context based steps
        isRegistering = true;
        var registeredMethods = typeof(TestCoordinator).Assembly.GetTypes().SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(ExecuteStepInContext.MultiprocessContextBasedTestAttribute), true).Length > 0)
            .ToArray();
        HashSet<Type> typesWithContextMethods = new HashSet<Type>();
        foreach (var method in registeredMethods)
        {
            typesWithContextMethods.Add(method.ReflectedType);
        }

        if (registeredMethods.Length == 0)
        {
            throw new Exception("Couldn't find any registered methods for multiprocess testing. Is TestCoordinator in same assembly as test methods?");
        }

        object[] GetParameterValuesToPass(ParameterInfo[] parameterInfo)
        {
            object[] parametersToReturn = new object[parameterInfo.Length];
            for (int i = 0; i < parameterInfo.Length; i++)
            {
                var paramType = parameterInfo[i].GetType();
                object defaultObj = null;
                if (paramType.IsValueType)
                {
                    defaultObj = Activator.CreateInstance(paramType);
                }

                parametersToReturn[i] = defaultObj;
            }

            return parametersToReturn;
        }

        foreach (var contextType in typesWithContextMethods)
        {
            var allConstructors = contextType.GetConstructors();
            if (allConstructors.Length > 1)
            {
                throw new NotImplementedException("Case not implemented where test has more than one contructor");
            }

            var instance = Activator.CreateInstance(contextType, allConstructors.Length > 0 ? GetParameterValuesToPass(allConstructors[0].GetParameters()) : null);
            m_AllClientTestInstances.Add(instance); // keeping that instance so tests can use captured local attributes

            List<MethodInfo> typeMethodsWithContextSteps = new List<MethodInfo>();
            foreach (var method in contextType.GetMethods())
            {
                if (method.GetCustomAttributes(typeof(ExecuteStepInContext.MultiprocessContextBasedTestAttribute), true).Length > 0)
                {
                    typeMethodsWithContextSteps.Add(method);
                }
            }
            foreach (var method in typeMethodsWithContextSteps)
            {
                var parametersToPass = GetParameterValuesToPass(method.GetParameters());
                var enumerator = (IEnumerator)method.Invoke(instance, parametersToPass.ToArray());
                while (enumerator.MoveNext()) { }
            }
        }

        isRegistering = false;
        hasRegistered = true;
    }

    public void Update()
    {
        if (Time.time - m_TimeSinceLastKeepAlive > perTestTimeout)
        {
            QuitApplication();
            Assert.Fail("Stayed idle too long");
        }
        if ((IsServer && NetworkManager.Singleton.IsListening) || (IsClient && NetworkManager.Singleton.IsConnectedClient))
        {
            m_TimeSinceLastConnected = Time.time;
        }
        else if (Time.time - m_TimeSinceLastConnected > maxWaitTimeout || m_ShouldShutdown)
        {
            // Make sure we don't have zombie processes
            Debug.Log($"quitting application, shouldShutdown set to {m_ShouldShutdown}, is listening {NetworkManager.Singleton.IsListening}, is connected client {NetworkManager.Singleton.IsConnectedClient}");
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

    public void OnDestroy()
    {
        if (NetworkObject != null && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }

    private static void OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
        {
            // if disconnect callback is for me or for server, quit, we're done here
            Debug.Log($"received disconnect from {clientId}, quitting");
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

    public static IEnumerable<float> ConsumeCurrentResult(ulong clientID)
    {
        var allResults = Instance.m_TestResultsLocal[clientID];
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
            if (Time.time - startWaitTime > maxWaitTimeout)
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
            if (Time.time - startWaitTime > maxWaitTimeout)
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
        InvokeFromMethodNameClientRpc(methodInfoString, args, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = AllClientIdExceptMine.ToArray() } });
    }

    public void InvokeFromMethodActionRpc(Action methodInfo)
    {
        var methodInfoString = GetMethodInfo(methodInfo);
        InvokeFromMethodNameClientRpc(methodInfoString, null, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = AllClientIdExceptMine.ToArray() } });
    }

    [ClientRpc]
    public void TriggerActionIDClientRpc(string actionID, byte[] args, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"received RPC from server, client side triggering action ID {actionID}");
        try
        {
            ExecuteStepInContext.allActions[actionID].Invoke(args);
        }
        catch (Exception e)
        {
            WriteErrorServerRpc(e.Message);
            throw;
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
                throw new Exception($"couldn't find method {staticMethodToExecute}");
            }

            foundMethod.Invoke(null, args != null ? new object[] { args } : null);
        }
        catch (Exception e)
        {
            WriteErrorServerRpc(e.Message);
            throw;
        }
    }

    [ClientRpc]
    public void CloseRemoteClientRpc()
    {
        try
        {
            NetworkManager.Singleton.StopClient();
            m_ShouldShutdown = true; // wait until isConnectedClient is false to run Application Quit in next update
            Debug.Log("Quitting player cleanly");
            Application.Quit();
        }
        catch (Exception e)
        {
            WriteErrorServerRpc(e.Message);
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
        if (!m_TestResultsLocal.ContainsKey(senderId))
        {
            m_TestResultsLocal[senderId] = new List<float>();
        }

        m_TestResultsLocal[senderId].Add(result);
    }

    [ServerRpc(RequireOwnership = false)]
    public void WriteErrorServerRpc(string errorMessage, ServerRpcParams receiveParams = default)
    {
        Debug.LogError($"Got Exception client side {errorMessage}, from client {receiveParams.Receive.SenderClientId}");
    }
}
