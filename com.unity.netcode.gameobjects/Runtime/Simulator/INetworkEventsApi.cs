using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Netcode.Simulator
{
    public interface INetworkEventsApi
    {
        bool IsDisabledBySimulator { get; }

        void TriggerDisconnect();

        void TriggerReconnect();

        void TriggerLagSpike(TimeSpan duration);

        void ChangeNetworkType(NetworkTypeConfiguration newNetworkType);
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

        public void ChangeNetworkType(NetworkTypeConfiguration newNetworkType)
        {
            Debug.Log($"Changing network type to {newNetworkType.Name}.");
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

        public void ChangeNetworkType(NetworkTypeConfiguration newNetworkType)
        {
            m_NetworkSimulator.NetworkTypeConfiguration = newNetworkType;
        }
    }
}