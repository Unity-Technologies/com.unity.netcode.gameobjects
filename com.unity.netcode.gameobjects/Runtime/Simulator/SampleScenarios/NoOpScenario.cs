using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Unity.Netcode.SampleScenarios
{
    [UsedImplicitly]
    public class NoOpScenario : NetworkSimulatorScenarioTask
    {
        protected override Task Run(INetworkEventsApi networkEventsApi, CancellationToken _)
        {
            return Task.CompletedTask;
        }
    }
}
