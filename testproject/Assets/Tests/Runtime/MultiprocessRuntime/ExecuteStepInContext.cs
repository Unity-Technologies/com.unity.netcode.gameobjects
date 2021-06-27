using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
            yield return new WaitUntil(() => TestCoordinator.Instance != null && TestCoordinator.Instance.hasRegistered);
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
    private List<Func<bool>> m_RemoteIsFinishedChecks = new List<Func<bool>>();
    private Func<bool> m_AdditionalIsFinishedWaiter;


    private bool m_WaitMultipleUpdates;
    private bool m_IgnoreTimeoutException;

    private float m_StartTime;
    private bool isTimingOut => Time.time - m_StartTime > TestCoordinator.maxWaitTimeout;

    private bool ShouldExecuteLocally => (m_ActionContext == StepExecutionContext.Server && m_NetworkManager.IsServer) || (m_ActionContext == StepExecutionContext.Clients && !m_NetworkManager.IsServer);

    // Assumes this is called from same callsite as ExecuteInContext (and assumes this is called from IEnumerator).
    // This relies on the name to be unique for each generated IEnumerator state machines
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
        m_IsRegistering = TestCoordinator.Instance.isRegistering;
        m_ActionContext = actionContext;
        m_StepToExecute = stepToExecute;
        m_WaitMultipleUpdates = waitMultipleUpdates;
        m_IgnoreTimeoutException = ignoreTimeoutException;
        if (additionalIsFinishedWaiter != null)
        {
            m_AdditionalIsFinishedWaiter = additionalIsFinishedWaiter;

            // m_IsFinishedChecks.Add(additionalIsFinishedWaiter);
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
                            Send = new ClientRpcSendParams { TargetClientIds = TestCoordinator.AllClientIdExceptMine.ToArray() }
                        });
                    foreach (var clientId in TestCoordinator.AllClientIdExceptMine)
                    {
                        m_RemoteIsFinishedChecks.Add(TestCoordinator.ConsumeClientIsFinished(clientId));
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

            if (m_IsRegistering || ShouldExecuteLocally || m_RemoteIsFinishedChecks == null)
            {
                return false;
            }

            for (int i = m_RemoteIsFinishedChecks.Count - 1; i >= 0; i--)
            {
                if (m_RemoteIsFinishedChecks[i].Invoke())
                {
                    m_RemoteIsFinishedChecks.RemoveAt(i);
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
