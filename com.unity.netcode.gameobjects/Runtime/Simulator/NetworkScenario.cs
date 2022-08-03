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
using System.Collections;
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

        NetworkSimulator NetworkSimulatorSimulator => m_NetworkSimulator == null
            ? m_NetworkSimulator = GetComponent<NetworkSimulator>()
            : m_NetworkSimulator;

        IEnumerator Start()
        {
            if (NetworkSimulatorSimulator == null)
            {
                throw new ArgumentNullException($"{nameof(NetworkSimulatorSimulator)} cannot be null.");
            }

            if (NetworkSimulatorScenario == null)
            {
                Debug.LogWarning($"You need to select a valid {nameof(NetworkScenario)}.");
                yield break;
            }

            while (NetworkSimulatorSimulator.IsInitialized == false)
            {
                yield return null;
            }

            NetworkSimulatorScenario.Start(NetworkSimulatorSimulator.NetworkEventsApi);
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
