using System.Threading.Tasks;

namespace Unity.Netcode.Simulator
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