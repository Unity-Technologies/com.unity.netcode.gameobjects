// NetSim Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE))
#define UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Netcode
{
    public interface INetworkSimulatorScenario : IDisposable
    {
        void Start(INetworkEventsApi networkEventsApi);
    }

    public interface INetworkSimulatorScenarioUpdateHandler : INetworkSimulatorScenario
    {
        void Update(float deltaTime);
    }

    public abstract class NetworkSimulatorScenarioTask : INetworkSimulatorScenario
    {
        readonly CancellationTokenSource m_Cancellation = new();

        void INetworkSimulatorScenario.Start(INetworkEventsApi networkEventsApi)
        {
            try
            {
                // use "_" (Discard operation) to remove the warning IDE0058: Because this call is not awaited, execution of the current method continues before the call is completed
                // https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/discards?WT.mc_id=DT-MVP-5003978#a-standalone-discard
                _ = ForgetAwaited(Run(networkEventsApi, m_Cancellation.Token));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        // Allocate the async/await state machine only when needed for performance reasons.
        // More info about the state machine: https://blogs.msdn.microsoft.com/seteplia/2017/11/30/dissecting-the-async-methods-in-c/?WT.mc_id=DT-MVP-5003978
        // Source: https://www.meziantou.net/fire-and-forget-a-task-in-dotnet.htm
        static async Task ForgetAwaited(Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        protected abstract Task Run(INetworkEventsApi networkEventsApi, CancellationToken cancellationToken);

        public void Dispose()
        {
            if (m_Cancellation.IsCancellationRequested == false)
            {
                m_Cancellation.Cancel();
            }

            m_Cancellation?.Dispose();
        }
    }

}

#endif
