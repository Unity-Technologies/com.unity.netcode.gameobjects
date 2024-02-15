using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    internal class AnticipationSystem
    {
        internal ulong LastAnticipationAck;
        internal double LastAnticipationAckTime;

        internal ulong AnticipationCounter;

        private NetworkManager m_NetworkManager;

        public delegate void NetworkVariableReanticipationDelegate(NetworkVariableBase variable);

        public struct NetworkVariableCallbackData
        {
            public NetworkVariableBase Variable;
            public NetworkVariableReanticipationDelegate Callback;
        }

        public Dictionary<NetworkVariableBase, NetworkVariableCallbackData> NetworkVariableReanticipationCallbacks = new Dictionary<NetworkVariableBase, NetworkVariableCallbackData>();

        public delegate void NetworkBehaviourReanticipateDelegate(NetworkBehaviour behaviour);

        public struct NetworkBehaviourCallbackData
        {
            public NetworkBehaviour Behaviour;
            public NetworkBehaviourReanticipateDelegate Callback;
        }

        public Dictionary<NetworkBehaviour, NetworkBehaviourCallbackData> NetworkBehaviourReanticipationCallbacks = new Dictionary<NetworkBehaviour, NetworkBehaviourCallbackData>();

        public HashSet<NetworkVariableBase> ChangedVariables = new HashSet<NetworkVariableBase>();
        public HashSet<NetworkBehaviour> ChangedBehaviours = new HashSet<NetworkBehaviour>();

        public AnticipationSystem(NetworkManager manager)
        {
            m_NetworkManager = manager;
        }

        public event Action<HashSet<NetworkVariableBase>, HashSet<NetworkBehaviour>> OnReanticipate;

        public void ProcessReanticipation()
        {
            ChangedVariables.Clear();
            ChangedBehaviours.Clear();
            foreach (var item in NetworkVariableReanticipationCallbacks)
            {
                ChangedVariables.Add(item.Value.Variable);
                item.Value.Callback?.Invoke(item.Value.Variable);
            }
            NetworkVariableReanticipationCallbacks.Clear();
            foreach (var item in NetworkBehaviourReanticipationCallbacks)
            {
                ChangedBehaviours.Add(item.Value.Behaviour);
                item.Value.Callback?.Invoke(item.Value.Behaviour);
            }
            NetworkBehaviourReanticipationCallbacks.Clear();
            OnReanticipate?.Invoke(ChangedVariables, ChangedBehaviours);
        }

        public void Sync()
        {
            if (!m_NetworkManager.ShutdownInProgress && !m_NetworkManager.ConnectionManager.LocalClient.IsServer && m_NetworkManager.ConnectionManager.LocalClient.IsConnected)
            {
                var message = new AnticipationCounterSyncPingMessage { Counter = AnticipationCounter, Time = m_NetworkManager.LocalTime.Time };
                m_NetworkManager.MessageManager.SendMessage(ref message, NetworkDelivery.Reliable, NetworkManager.ServerClientId);
            }

            ++AnticipationCounter;
        }
    }
}
