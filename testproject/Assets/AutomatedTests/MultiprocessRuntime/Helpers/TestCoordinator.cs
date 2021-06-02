using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(NetworkObject))]
internal class TestCoordinator : NetworkBehaviour
{
    /// <summary>
    /// TestCoordinator
    /// Used for coordinating multiprocess end to end tests. Used to call RPCs on other nodes and gather results
    /// /// Needed since the current remote player test runner hardcodes test to run in a bootstrap scene before launching the player.
    /// There's not per-test communication to run these tests, only to get the results per test as they are running
    /// </summary>
    public const float maxWaitTimeout = 10;
    public const char methodFullNameSplitChar = '@';
    public const string isClientArg = "-isClient";
    public const string buildInfoFileName = "buildInfo.txt";

    public bool isRegistering;
    public bool hasRegistered;

    public static List<ulong> AllClientIdExceptMine
    {
        get { return NetworkManager.Singleton.ConnectedClients.Keys.ToList().FindAll(client => client != NetworkManager.Singleton.LocalClientId); }
    }

    private bool m_ShouldShutdown;

    // private NetworkDictionary<ulong, float> m_TestResults = new NetworkDictionary<ulong, float>(new NetworkVariableSettings
    // {
    //     WritePermission = NetworkVariablePermission.Everyone,
    //     ReadPermission = NetworkVariablePermission.Everyone
    // }); // todo this is starting to look like the wrong way to sync results. If we have multiple results arriving one after the other, there's a risk they'll overwrite one another

    private Dictionary<ulong, Queue<float>> m_TestResultsLocal = new Dictionary<ulong, Queue<float>>();

    private NetworkList<string> m_ErrorMessages = new NetworkList<string>(new NetworkVariableSettings
    {
        ReadPermission = NetworkVariablePermission.Everyone,
        WritePermission = NetworkVariablePermission.Everyone
    });

    public static TestCoordinator Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Multiple test coordinator! destroying self");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public class CustomTest : ITest
    {
        public TNode ToXml(bool recursive)
        {
            throw new NotImplementedException();
        }

        public TNode AddToXml(TNode parentNode, bool recursive)
        {
            throw new NotImplementedException();
        }

        public string Id { get; }
        public string Name { get; }
        public string FullName { get; }
        public string ClassName { get; }
        public string MethodName { get; }
        public ITypeInfo TypeInfo { get; }
        public IMethodInfo Method { get; set; }
        public RunState RunState { get; }
        public int TestCaseCount { get; }
        public IPropertyBag Properties { get; }
        public ITest Parent { get; }
        public bool IsSuite { get; }
        public bool HasChildren { get; }
        public IList<ITest> Tests { get; }
        public object Fixture { get; }
    }

    public void Start()
    {
        bool isClient = Environment.GetCommandLineArgs().Any(value => value == isClientArg);
        if (isClient)
        {
            Debug.Log("starting MLAPI client");
            NetworkManager.Singleton.StartClient();

            // StartCoroutine(WaitForClientConnected()); // in case builds fail, can't have the old builds just stay idle. If they can't connect after a certain amount of time, disconnect
        }

        m_ErrorMessages.OnListChanged += OnErrorMessageChanged;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;

        // registering magically all method steps
        isRegistering = true;
        var registeredMethods = typeof(TestCoordinator).Assembly.GetTypes().SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(ExecuteInContext.MultiprocessContextBasedTestAttribute), true).Length > 0)
            .ToArray();
        foreach (var method in registeredMethods)
        {
            var type = method.ReflectedType;
            var instance = Activator.CreateInstance(type);

            ExecuteInContext.InitTest(method);
            var result = (IEnumerator)method.Invoke(instance, null);
            while (result.MoveNext()) { }
        }

        isRegistering = false;
        hasRegistered = true;
    }

    public void OnDestroy()
    {
        m_ErrorMessages.OnListChanged -= OnErrorMessageChanged;
        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }
    }

    private void OnErrorMessageChanged(NetworkListEvent<string> listChangedEvent)
    {
        switch (listChangedEvent.Type)
        {
            case NetworkListEvent<string>.EventType.Add:
            case NetworkListEvent<string>.EventType.Insert:
            case NetworkListEvent<string>.EventType.Value:
                Debug.LogError($"Got Exception client side {listChangedEvent.Value}, type is {listChangedEvent.Type} and index is {listChangedEvent.Index}");
                m_ErrorMessages.RemoveAt(listChangedEvent.Index); // consume error message
                break;
        }
    }

    // private static void OnResultsChanged(NetworkDictionaryEvent<ulong, float> dictChangedEvent)
    // {
    //     if (dictChangedEvent.Key != NetworkManager.Singleton.LocalClientId)
    //     {
    //         switch (dictChangedEvent.Type)
    //         {
    //             case NetworkDictionaryEvent<ulong, float>.EventType.Add:
    //             case NetworkDictionaryEvent<ulong, float>.EventType.Value:
    //                 Instance.AllClientIdWithResults.Add(dictChangedEvent.Key);
    //                 break;
    //         }
    //     }
    // }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
        {
            // if disconnect callback is for me or for server, quit, we're done here
            Debug.Log($"received disconnect from {clientId}, quitting");
            Application.Quit();
        }
    }

    public static string GetMethodInfo(Action<byte[]> method)
    {
        return $"{method.Method.DeclaringType.FullName}{methodFullNameSplitChar}{method.Method.Name}";
    }

    public static string GetMethodInfo(Action method)
    {
        return $"{method.Method.DeclaringType.FullName}{methodFullNameSplitChar}{method.Method.Name}";
    }

    [ServerRpc(RequireOwnership = false)]
    public void WriteTestResultsServerRpc(float result, ServerRpcParams receiveParams = default)
    {
        Debug.Log($"received test results {result}");
        var senderId = receiveParams.Receive.SenderClientId;
        if (!m_TestResultsLocal.ContainsKey(senderId))
        {
            m_TestResultsLocal[senderId] = new Queue<float>();
        }

        m_TestResultsLocal[senderId].Enqueue(result);

        // Instance.CurrentClientIdWithResults = receiveParams.SenderClientId;
    }

    // public static void WriteResults(float result)
    // {
    //
    //     // Instance.m_TestResults[NetworkManager.Singleton.LocalClientId] = result;
    // }

    public static void WriteError(string errorMessage)
    {
        var localId = NetworkManager.Singleton.LocalClientId;

        Instance.m_ErrorMessages.Add($"{localId}@{errorMessage}");
    }

    public static IEnumerable<(ulong clientId, float result)> ConsumeCurrentResult()
    {
        foreach (var kv in Instance.m_TestResultsLocal)
        {
            while (kv.Value.Count > 0)
            {
                var toReturn = (kv.Key, kv.Value.Dequeue());
                yield return toReturn;
            }
        }

        // throw new NotImplementedException("Should not be here");
    }

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

            foreach (var kv in Instance.m_TestResultsLocal)
            {
                if (kv.Value.Count > 0)
                {
                    return true;
                }
            }

            // if (Instance.AllClientIdWithResults.Count > 0)
            // {
            //     Instance.CurrentClientIdWithResults = Instance.AllClientIdWithResults[0];
            //     Instance.AllClientIdWithResults.RemoveAt(0);
            //     return true;
            // }

            return false;
        };
    }

    private Dictionary<ulong, bool> m_ClientIsFinished = new Dictionary<ulong, bool>();

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

    public void TriggerRpc(Action<byte[]> methodInfo, params byte[] args)
    {
        var methodInfoString = GetMethodInfo(methodInfo);
        TriggerInternalClientRpc(methodInfoString, args, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = AllClientIdExceptMine.ToArray() } });
    }

    public void TriggerRpc(Action methodInfo)
    {
        var methodInfoString = GetMethodInfo(methodInfo);
        TriggerInternalClientRpc(methodInfoString, null, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = AllClientIdExceptMine.ToArray() } });
    }

    [ClientRpc]
    public void TriggerActionIDClientRpc(string actionID, byte[] args, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"received RPC from server, client side triggering action ID {actionID}");
        ExecuteInContext.allActions[actionID].Invoke(args);
    }

    [ServerRpc]
    public void TriggerActionIDServerRpc(string actionID, byte[] args, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"received RPC from client, server side triggering action ID {actionID}");
        ExecuteInContext.allActions[actionID].Invoke(args);
    }

    [ClientRpc]
    public void TriggerInternalClientRpc(string methodInfoString, byte[] args, ClientRpcParams clientRpcParams = default)
    {
        try
        {
            Debug.Log($"received client RPC for method {methodInfoString}");
            var split = methodInfoString.Split(methodFullNameSplitChar);
            (string classToExecute, string staticMethodToExecute) info = (split[0], split[1]);

            var foundType = Type.GetType(info.classToExecute);
            if (foundType == null)
            {
                throw new Exception($"couldn't find {info.classToExecute}");
            }

            var foundMethod = foundType.GetMethod(info.staticMethodToExecute);
            if (foundMethod == null)
            {
                throw new Exception($"couldn't find method {info.staticMethodToExecute}");
            }

            if (args != null)
            {
                foundMethod.Invoke(null, new[] { args });
            }
            else
            {
                foundMethod.Invoke(null, null);
            }
        }
        catch (Exception e)
        {
            WriteError(e.Message);

            // throw e;
        }
    }

    [ClientRpc]
    public void CloseRemoteClientRpc()
    {
        NetworkManager.Singleton.StopClient();
        m_ShouldShutdown = true; // wait until isConnectedClient is false to run Application Quit in next update
        Debug.Log("Quitting player cleanly");
        Application.Quit();
    }

    private float m_TimeSinceLastConnected;

    public void Update()
    {
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            m_TimeSinceLastConnected = Time.time;
        }
        else if (Time.time - m_TimeSinceLastConnected > maxWaitTimeout || m_ShouldShutdown)
        {
            // Make sure we don't have zombie processes
            Application.Quit();
        }
    }

    public void TestRunTeardown()
    {
        // m_TestResults.Clear();
        m_TestResultsLocal.Clear();
        m_ErrorMessages.Clear();

        // AllClientIdWithResults.Clear();
        // CurrentClientIdWithResults = 0;
    }

    public static void StartWorkerNode()
    {
// #if UNITY_EDITOR

        var workerNode = new Process();

        //TODO this should be replaced eventually by proper orchestration
        // TODO test on windows
        var exeName = "testproject";
        string buildInstructions = $"You probably didn't generate your build. Please make sure you build a player using the '{BuildAndRunMultiprocessTests.BuildAndExecuteMenuName}' menu";
        try
        {
            var buildInfo = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, buildInfoFileName));
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            workerNode.StartInfo.FileName = $"{buildInfo}.app/Contents/MacOS/{exeName}";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            workerNode.StartInfo.FileName = $"{buildInfo}/{exeName}.exe";
#else
            throw new NotImplementedException("StartWorkerNode: Current platform not supported");
#endif
        }
        catch (FileNotFoundException e)
        {
            throw new Exception($"Couldn't find build info file. {buildInstructions}");
        }

        workerNode.StartInfo.UseShellExecute = false;
        workerNode.StartInfo.RedirectStandardError = true;
        workerNode.StartInfo.RedirectStandardOutput = true;
        workerNode.StartInfo.Arguments = $"{isClientArg} -popupwindow -screen-width 100 -screen-height 100";

        // workerNode.StartInfo.Arguments = $"-startAsServer";
        try
        {
            var newProcessStarted = workerNode.Start();
            Debug.Log($"new process started? {newProcessStarted}");
            if (!newProcessStarted)
            {
                throw new Exception("Process not started!");
            }
        }
        catch (Win32Exception e)
        {
            Debug.LogError($"Error starting player, {buildInstructions}, {e.Message} {e.Data} {e.ErrorCode}");
            throw e;
        }

// #else
//         throw new NotImplementedException("worker node launching not supported");
// #endif
    }

    public class ExecuteInContext : CustomYieldInstruction
    {
        public enum StepExecutionContext
        {
            Server,
            Clients
        }

        [AttributeUsage(AttributeTargets.Method)]
        public class MultiprocessContextBasedTestAttribute : NUnitAttribute, IOuterUnityTestAction
        {
            public IEnumerator BeforeTest(ITest test)
            {
                yield return new WaitUntil(() => TestCoordinator.Instance != null && TestCoordinator.Instance.hasRegistered);

                // var method = test.Method.MethodInfo.ReturnType.GetMethod(nameof(IEnumerator.MoveNext));
                // ExecuteInContext.InitTest(method);
                TestCoordinator.ExecuteInContext.InitTest(test.Method.MethodInfo);
            }

            public IEnumerator AfterTest(ITest test)
            {
                yield break;
            }
        }

        private StepExecutionContext m_ActionContextContext;
        private Action<byte[]> m_Todo;
        public static Dictionary<string, ExecuteInContext> allActions = new Dictionary<string, ExecuteInContext>();

        // private static int s_ActionID;
        private static Dictionary<string, int> s_MethodIDCounter = new Dictionary<string, int>();

        // private int m_CurrentActionID;
        private NetworkManager m_NetworkManager;
        private bool m_IsRegistering;
        private List<Func<bool>> m_WaitForClientCheck = new List<Func<bool>>();
        private bool m_FinishOnInvoke;

        // assumes this is called from same callsite as ExecuteInContext
        public static void InitTest(MethodBase callerMethod)
        {
            // var callerMethod = new StackFrame(1).GetMethod();
            var methodIdentifier = GetMethodIdentifier(callerMethod, callerMethod.Name, callerMethod.DeclaringType.FullName);
            s_MethodIDCounter[methodIdentifier] = 0;
        }

        public static string GetMethodIdentifier(MethodBase method, string methodName, string callerTypeName)
        {
            string allParameters = "";
            foreach (var param in method.GetParameters())
            {
                allParameters += param.Name;
            }

            var info = callerTypeName + methodName + allParameters;
            Debug.Log($"GetMethodIdentifier!!!! {info}");
            return info;
        }

        private bool ShouldExecuteLocally => (m_ActionContextContext == StepExecutionContext.Server && m_NetworkManager.IsServer) || (m_ActionContextContext == StepExecutionContext.Clients && !m_NetworkManager.IsServer);

        /// <summary>
        /// Executes an action with the specified context. This allows writing tests all in the same sequential flow,
        /// making it more readable. This allows not having to jump between static client methods and test method
        /// </summary>
        /// <param name="actionContext">context to use. for example, should execute client side? server side?</param>
        /// <param name="todo">action to execute</param>
        /// <param name="isRegistering">is it currently registering this action. This should be passed by the test method on process startup</param>
        /// <param name="paramToPass">parameters to pass to action</param>
        /// <param name="networkManager"></param>
        /// <param name="finishOnInvoke"> waits multiple frames before allowing the execution to continue. This means ClientFinishedServerRpc must be called manually</param>
        public ExecuteInContext(StepExecutionContext actionContext, Action<byte[]> todo, byte[] paramToPass = default, NetworkManager networkManager = null, bool finishOnInvoke = true, [CallerMemberName] string callerName = "")
        {
            m_IsRegistering = TestCoordinator.Instance.isRegistering;
            m_ActionContextContext = actionContext;
            m_Todo = todo;
            m_FinishOnInvoke = finishOnInvoke;
            if (networkManager == null)
            {
                networkManager = NetworkManager.Singleton;
            }

            m_NetworkManager = networkManager; // todo test using this for in-process tests too?

            var callerMethod = new StackFrame(1).GetMethod(); // one skip frame for current method

            var methodId = GetMethodIdentifier(callerMethod, callerName, callerMethod.DeclaringType.DeclaringType.FullName); // assumes called from IEnumerator MoveNext, which should be the case since we're a CustomYieldInstruction
            if (!s_MethodIDCounter.ContainsKey(methodId))
            {
                s_MethodIDCounter[methodId] = 0;
            }

            string currentActionID = methodId + s_MethodIDCounter[methodId]++;

            if (m_IsRegistering)
            {
                Debug.Log($"registering action with id {currentActionID}");
                allActions[currentActionID] = this;
            }
            else
            {
                if (ShouldExecuteLocally)
                {
                    m_Todo.Invoke(paramToPass);
                }
                else
                {
                    if (networkManager.IsServer)
                    {
                        TestCoordinator.Instance.TriggerActionIDClientRpc(currentActionID, paramToPass,
                            clientRpcParams: new ClientRpcParams()
                            {
                                Send = new ClientRpcSendParams() { TargetClientIds = TestCoordinator.AllClientIdExceptMine.ToArray() }
                            });
                        foreach (var clientId in TestCoordinator.AllClientIdExceptMine)
                        {
                            m_WaitForClientCheck.Add(TestCoordinator.ConsumeClientIsFinished(clientId));
                        }
                    }
                    else
                    {
                        TestCoordinator.Instance.TriggerActionIDServerRpc(currentActionID, paramToPass);
                    }
                }
            }
        }

        public void Invoke(byte[] args)
        {
            m_Todo.Invoke(args);
            if (m_FinishOnInvoke)
            {
                if (!m_NetworkManager.IsServer)
                {
                    TestCoordinator.Instance.ClientFinishedServerRpc();
                }
                else
                {
                    throw new NotImplementedException("todo implement");
                }
            }
        }

        public override bool keepWaiting
        {
            get
            {
                if (m_IsRegistering || ShouldExecuteLocally || m_WaitForClientCheck == null)
                {
                    return false;
                }

                for (int i = m_WaitForClientCheck.Count - 1; i >= 0; i--)
                {
                    var waiter = m_WaitForClientCheck[i];
                    var receivedResponse = waiter.Invoke();
                    if (receivedResponse)
                    {
                        m_WaitForClientCheck.RemoveAt(i);
                    }
                    else
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

}
