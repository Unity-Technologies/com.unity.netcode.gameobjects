using System;
using UnityEngine;

namespace Unity.Netcode
{
    public interface INetworkEventsApi
    {
        void TriggerDisconnect();

        void TriggerReconnect();

        void TriggerLagSpike(TimeSpan duration);

        void ChangeNetworkType(NetworkSimulationConfiguration newNetworkSimulation);
    }

    public class NoOpNetworkEventsApi : INetworkEventsApi
    {
        public void TriggerDisconnect()
        {
            Debug.Log("Triggering disconnect.");
        }

        public void TriggerReconnect()
        {
            Debug.Log("Triggering reconnect.");
        }

        public void TriggerLagSpike(TimeSpan duration)
        {
            Debug.Log($"Triggering lag spike for {duration.Milliseconds} ms.");
        }

        public void ChangeNetworkType(NetworkSimulationConfiguration newNetworkSimulation)
        {
            Debug.Log($"Changing network type to {newNetworkSimulation.Name}.");
        }
    }
}
