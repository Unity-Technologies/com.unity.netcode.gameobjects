// NetSim Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NETSIM_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE))
    #define UNITY_MP_TOOLS_NETSIM_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

#if UNITY_MP_TOOLS_NETSIM_ENABLED

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

        void ChangeNetworkType(NetworkSimulatorConfiguration newNetworkSimulatorConfiguration);
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

        public void ChangeNetworkType(NetworkSimulatorConfiguration newNetworkSimulatorConfiguration)
        {
            Debug.Log($"Changing network type to {newNetworkSimulatorConfiguration.Name}.");
        }
    }

    public class NetworkEventsApi : INetworkEventsApi
    {
        readonly NetworkSimulator m_NetworkSimulator;
        readonly UnityTransport m_UnityTransport;

        public NetworkEventsApi(NetworkSimulator networkSimulator, UnityTransport unityTransport)
        {
            m_NetworkSimulator = networkSimulator;
            m_UnityTransport = unityTransport;
        }

        public bool IsDisabledBySimulator => m_UnityTransport.IsDisabledBySimulator;

        public void TriggerDisconnect()
        {
            m_UnityTransport.TriggerDisconnect();
        }

        public void TriggerReconnect()
        {
            m_UnityTransport.TriggerReconnect();
        }

        public void TriggerLagSpike(TimeSpan duration)
        {
            Task.Run(async () =>
            {
                TriggerDisconnect();

                await Task.Delay(duration);

                TriggerReconnect();
            });
        }

        public void ChangeNetworkType(NetworkSimulatorConfiguration newNetworkSimulatorConfiguration)
        {
            m_NetworkSimulator.SimulatorConfiguration = newNetworkSimulatorConfiguration;
        }
    }
}

#endif
