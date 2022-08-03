// NetSim Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE))
    #define UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------


using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Netcode
{
    public interface INetworkEventsApi
    {
        bool IsDisabledBySimulator { get; }

        void TriggerDisconnect();

        void TriggerReconnect();

        void TriggerLagSpike(TimeSpan duration);

        void ChangeNetworkType(INetworkSimulatorConfiguration newNetworkSimulatorConfiguration);
    }

    public class NoOpNetworkEventsApi : INetworkEventsApi
    {
        public bool IsDisabledBySimulator { get; private set; }

        public void TriggerDisconnect()
        {
            IsDisabledBySimulator = true;

            Debug.Log("Triggering disconnect.");
        }

        public void TriggerReconnect()
        {
            IsDisabledBySimulator = false;

            Debug.Log("Triggering reconnect.");
        }

        public void TriggerLagSpike(TimeSpan duration)
        {
            Debug.Log($"Triggering lag spike for {duration.Milliseconds} ms.");
        }

        public void ChangeNetworkType(INetworkSimulatorConfiguration newNetworkSimulatorConfiguration)
        {
            Debug.Log($"Changing network type to {newNetworkSimulatorConfiguration.Name}.");
        }
    }

    public class NetworkEventsApi : INetworkEventsApi
    {
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
        readonly NetworkSimulator m_NetworkSimulator;
        readonly UnityTransport m_UnityTransport;
#endif

        public NetworkEventsApi(NetworkSimulator networkSimulator, UnityTransport unityTransport)
        {
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            m_NetworkSimulator = networkSimulator;
            m_UnityTransport = unityTransport;
#endif
        }

        public bool IsDisabledBySimulator
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            => m_UnityTransport.IsDisabledBySimulator;
#else
            => false;
#endif

        public void TriggerDisconnect()
        {
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            m_UnityTransport.TriggerDisconnect();
#endif
        }

        public void TriggerReconnect()
        {
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            m_UnityTransport.TriggerReconnect();
#endif
        }

        public void TriggerLagSpike(TimeSpan duration)
        {
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            Task.Run(async () =>
            {
                TriggerDisconnect();

                await Task.Delay(duration);

                TriggerReconnect();
            });
#endif
        }

        public void ChangeNetworkType(INetworkSimulatorConfiguration newNetworkSimulatorConfiguration)
        {
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            m_NetworkSimulator.SimulatorConfiguration = newNetworkSimulatorConfiguration;
#endif
        }
    }
}
