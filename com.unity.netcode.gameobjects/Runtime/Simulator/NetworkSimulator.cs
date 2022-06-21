using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Netcode
{
    [RequireComponent(typeof(UnityTransport))]
    public class NetworkSimulator : MonoBehaviour
    {
        [field: SerializeField]
        private NetworkSimulationConfiguration m_simulationConfiguration;

        public NetworkSimulationConfiguration SimulationConfiguration
        {
            get => m_simulationConfiguration;
            set
            {
                m_simulationConfiguration = value;
                UpdateLiveParameters();
            }
        }

        public void UpdateLiveParameters()
        {
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport != null)
            {
                transport.RefreshSimulationPipelineParameters(SimulationConfiguration);
            }
        }
    }
}
