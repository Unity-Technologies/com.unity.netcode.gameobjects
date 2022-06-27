// NetSim Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NETSIM_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE))
    #define UNITY_MP_TOOLS_NETSIM_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

#if UNITY_MP_TOOLS_NETSIM_ENABLED

using UnityEngine;

namespace Unity.Netcode
{
    [RequireComponent(typeof(NetworkSimulator))]
    public class NetworkScenario : MonoBehaviour
    {
        NetworkSimulator m_NetworkSimulator;

        [SerializeReference]
        internal INetworkSimulatorScenario m_NetworkSimulatorScenario;

        public INetworkSimulatorScenario NetworkSimulatorScenario
        {
            get => m_NetworkSimulatorScenario;
            set => m_NetworkSimulatorScenario = value;
        }

        public NetworkSimulator NetworkSimulatorSimulator => m_NetworkSimulator == null
            ? m_NetworkSimulator = GetComponent<NetworkSimulator>()
            : m_NetworkSimulator;

        void Start()
        {
            NetworkSimulatorScenario?.Start(NetworkSimulatorSimulator.NetworkEventsApi);
        }
        
        void OnDestroy()
        {
            NetworkSimulatorScenario?.Dispose();
        }
                
        void Update()
        {
            if (NetworkSimulatorScenario is INetworkSimulatorScenarioUpdateHandler updatableSimulator)
            {
                updatableSimulator.Update(Time.deltaTime);
            }
        }
    }
}

#endif
