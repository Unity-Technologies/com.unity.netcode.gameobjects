// NetSim Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE))
    #define UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Unity.Netcode
{
    [RequireComponent(typeof(UnityTransport))]
    public class NetworkSimulator : MonoBehaviour, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [SerializeField]
        internal Object m_Configuration;

        [SerializeReference, HideInInspector]
        internal INetworkSimulatorConfiguration m_ConfigurationReference = new NetworkSimulatorConfiguration();

        INetworkEventsApi m_NetworkEventsApi;

        internal INetworkEventsApi NetworkEventsApi => m_NetworkEventsApi ??= new NoOpNetworkEventsApi();

        internal bool IsInitialized { get; private set; }

        public INetworkSimulatorConfiguration SimulatorConfiguration
        {
            get => m_Configuration != null
                ? m_Configuration as INetworkSimulatorConfiguration
                : m_ConfigurationReference;
            set
            {
                if (value is Object networkTypeConfigurationObject)
                {
                    m_Configuration = networkTypeConfigurationObject;
                }
                else
                {
                    m_ConfigurationReference = value;
                }

                UpdateLiveParameters();
                OnPropertyChanged();
            }
        }

        public void UpdateLiveParameters()
        {
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            if (!Application.isPlaying)
            {
                return;
            }

            var transport = NetworkManager.Singleton.NetworkConfig?.NetworkTransport as UnityTransport;
            if (transport != null)
            {
                transport.UpdateSimulationPipelineParameters(SimulatorConfiguration);
            }
#endif
        }

        void Start()
        {
            var unityTransport = GetComponent<UnityTransport>();
            m_NetworkEventsApi = new NetworkEventsApi(this, unityTransport);
            IsInitialized = true;
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
