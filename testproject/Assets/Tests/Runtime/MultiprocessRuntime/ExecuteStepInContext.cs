using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MLAPI;
using MLAPI.Messaging;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

/// <summary>
/// Allows for context based delegate execution.
/// Can specify where you want that lambda executed (client side? server side?) and it'll automatically wait for the end
/// of a clientRPC server side and vice versa.
/// todo this could be used as an in-game tool too? for protocols that require a lot of back and forth?
/// </summary>
public class ExecuteStepInContext : CustomYieldInstruction
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
            yield return new WaitUntil(() => TestCoordinator.Instance != null && hasRegistered);
        }

        public IEnumerator AfterTest(ITest test)
        {
            yield break;
        }
    }

    private StepExecutionContext m_ActionContext;
    private Action<byte[]> m_StepToExecute;
    private string m_CurrentActionID;

    // as a remote worker, I store all available actions so I can execute them when triggered from RPCs
    public static Dictionary<string, ExecuteStepInContext> allActions = new Dictionary<string, ExecuteStepInContext>();
    private static Dictionary<string, int> s_MethodIDCounter = new Dictionary<string, int>();

    private NetworkManager m_NetworkManager;
    private bool m_IsRegistering;
    private List<Func<bool>> m_ClientIsFinishedChecks = new List<Func<bool>>();
    private Func<bool> m_AdditionalIsFinishedWaiter;

    private bool m_WaitMultipleUpdates;
    private bool m_IgnoreTimeoutException;

    private float m_StartTime;
    private bool isTimingOut => Time.time - m_StartTime > TestCoordinator.maxWaitTimeout;
    private bool ShouldExecuteLocally => (m_ActionContext == StepExecutionContext.Server && m_NetworkManager.IsServer) || (m_ActionContext == StepExecutionContext.Clients && !m_NetworkManager.IsServer);

    public static bool isRegistering;
    public static bool hasRegistered;
    private static List<object> m_AllClientTestInstances = new List<object>(); // to keep an instance for each tests, so captured context in each step is kept

    /// <summary>
    /// This MUST be called at the beginning of each test in order to use context based steps.
    /// Assumes this is called from same callsite as ExecuteInContext (and assumes this is called from IEnumerator).
    /// This relies on the name to be unique for each generated IEnumerator state machines
    /// </summary>
    public static void InitContextSteps()
    {
        var callerMethod = new StackFrame(1).GetMethod();
        var methodIdentifier = GetMethodIdentifier(callerMethod.DeclaringType.FullName); // since this is called from IEnumerator, this should be a generated class
        s_MethodIDCounter[methodIdentifier] = 0;
    }

    private static string GetMethodIdentifier(string callerTypeName)
    {
        var info = callerTypeName;
        return info;
    }

    internal static void InitializeAllSteps()
    {
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

    /// <summary>
    /// Executes an action with the specified context. This allows writing tests all in the same sequential flow,
    /// making it more readable. This allows not having to jump between static client methods and test method
    /// </summary>
    /// <param name="actionContext">context to use. for example, should execute client side? server side?</param>
    /// <param name="stepToExecute">action to execute</param>
    /// <param name="ignoreTimeoutException">waits for timeout and just finishes step execution silently</param>
    /// <param name="paramToPass">parameters to pass to action</param>
    /// <param name="networkManager"></param>
    /// <param name="waitMultipleUpdates"> waits multiple frames before allowing the execution to continue. This means ClientFinishedServerRpc must be called manually</param>
    /// <param name="additionalIsFinishedWaiter"></param>
    public ExecuteStepInContext(StepExecutionContext actionContext, Action<byte[]> stepToExecute, bool ignoreTimeoutException = false, byte[] paramToPass = default, NetworkManager networkManager = null, bool waitMultipleUpdates = false, Func<bool> additionalIsFinishedWaiter = null)
    {
        m_StartTime = Time.time;
        m_IsRegistering = isRegistering;
        m_ActionContext = actionContext;
        m_StepToExecute = stepToExecute;
        m_WaitMultipleUpdates = waitMultipleUpdates;
        m_IgnoreTimeoutException = ignoreTimeoutException;

        if (additionalIsFinishedWaiter != null)
        {
            m_AdditionalIsFinishedWaiter = additionalIsFinishedWaiter;
        }

        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        m_NetworkManager = networkManager; // todo test using this for multiinstance tests too?

        var callerMethod = new StackFrame(1).GetMethod(); // one skip frame for current method

        var methodId = GetMethodIdentifier(callerMethod.DeclaringType.FullName); // assumes called from IEnumerator MoveNext, which should be the case since we're a CustomYieldInstruction
        if (!s_MethodIDCounter.ContainsKey(methodId))
        {
            s_MethodIDCounter[methodId] = 0;
        }

        string currentActionID = $"{methodId}-{s_MethodIDCounter[methodId]++}";
        m_CurrentActionID = currentActionID;

        if (m_IsRegistering)
        {
            Assert.That(allActions, Does.Not.Contain(currentActionID)); // sanity check
            allActions[currentActionID] = this;
        }
        else
        {
            if (ShouldExecuteLocally)
            {
                m_StepToExecute.Invoke(paramToPass);
            }
            else
            {
                if (networkManager.IsServer)
                {
                    TestCoordinator.Instance.TriggerActionIDClientRpc(currentActionID, paramToPass,
                        clientRpcParams: new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams { TargetClientIds = TestCoordinator.AllClientIdsExceptMine.ToArray() }
                        });
                    foreach (var clientId in TestCoordinator.AllClientIdsExceptMine)
                    {
                        m_ClientIsFinishedChecks.Add(TestCoordinator.ConsumeClientIsFinished(clientId));
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }

    public void Invoke(byte[] args)
    {
        m_StepToExecute.Invoke(args);
        if (!m_WaitMultipleUpdates)
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
            if (isTimingOut)
            {
                if (m_IgnoreTimeoutException)
                {
                    Debug.LogWarning($"Timeout ignored for action ID {m_CurrentActionID}");
                    return false;
                }

                throw new Exception($"timeout for Context Step with action ID {m_CurrentActionID}");
            }

            if (m_AdditionalIsFinishedWaiter != null)
            {
                var isFinished = m_AdditionalIsFinishedWaiter.Invoke();
                if (!isFinished)
                {
                    return true;
                }
            }

            if (m_IsRegistering || ShouldExecuteLocally || m_ClientIsFinishedChecks == null)
            {
                return false;
            }

            for (int i = m_ClientIsFinishedChecks.Count - 1; i >= 0; i--)
            {
                if (m_ClientIsFinishedChecks[i].Invoke())
                {
                    m_ClientIsFinishedChecks.RemoveAt(i);
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
