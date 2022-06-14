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

        private UnityTransport m_Transport;

        public void Start()
        {
            m_Transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            UpdateLiveParameters();
        }

        public void UpdateLiveParameters()
        {
            if (m_Transport != null)
            {
                m_Transport.RefreshSimulationPipelineParameters(SimulationConfiguration);
            }
        }
    }
}
