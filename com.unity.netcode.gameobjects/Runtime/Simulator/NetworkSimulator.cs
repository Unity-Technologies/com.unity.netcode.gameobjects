using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Netcode.Simulator
{
    [RequireComponent(typeof(UnityTransport))]
    public class NetworkSimulator : MonoBehaviour
    {
        [field: SerializeField]
        public NetworkTypeConfiguration NetworkTypeConfiguration { get; set; }

        [field: SerializeField]
        public INetworkSimulatorScenario NetworkSimulatorScenario { get; set; }

        public void UpdateLiveParameters()
        {
            var transport = GetComponent<UnityTransport>();
            transport.RefreshSimulationPipelineParameters(NetworkTypeConfiguration);
        }
    }
}