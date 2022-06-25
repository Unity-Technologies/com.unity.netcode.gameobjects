using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Netcode
{
    [RequireComponent(typeof(UnityTransport))]
    public class NetworkSimulator : MonoBehaviour
    {
        [SerializeField]
        NetworkSimulatorConfiguration m_SimulatorConfiguration;

        public NetworkSimulatorConfiguration SimulatorConfiguration
        {
            get => m_SimulatorConfiguration;
            set
            {
                m_SimulatorConfiguration = value;
                UpdateLiveParameters();
            }
        }

        public void UpdateLiveParameters()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var transport = NetworkManager.Singleton.NetworkConfig?.NetworkTransport as UnityTransport;
            if (transport != null)
            {
                transport.UpdateSimulationPipelineParameters(SimulatorConfiguration);
            }
        }
    }
}
