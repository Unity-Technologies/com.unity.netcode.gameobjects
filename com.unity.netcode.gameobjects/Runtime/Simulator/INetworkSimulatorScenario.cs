// NetSim Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NETSIM_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE))
#define UNITY_MP_TOOLS_NETSIM_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

#if UNITY_MP_TOOLS_NETSIM_ENABLED

using System.Threading.Tasks;

namespace Unity.Netcode
{
    public interface INetworkSimulatorScenario
    {
        Task Run(INetworkEventsApi networkEventsApi);
    }

    public class NoOpScenario : INetworkSimulatorScenario
    {
        public Task Run(INetworkEventsApi networkEventsApi)
        {
            return Task.CompletedTask;
        }
    }
}

#endif
