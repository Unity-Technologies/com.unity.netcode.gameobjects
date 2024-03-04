using System.Collections.Generic;

namespace Unity.Netcode
{
    internal interface IAnticipationEventReceiver
    {
        public void SetupForUpdate();
        public void SetupForRender();
    }

    internal class AnticipationSystem
    {
        internal ulong LastAnticipationAck;
        internal double LastAnticipationAckTime;

        internal double NumberOfAnticipatedObjects = 0;

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

        public event NetworkManager.ReanticipateDelegate OnReanticipate;

        private HashSet<IAnticipationEventReceiver> m_AnticipationEventReceivers = new HashSet<IAnticipationEventReceiver>();

        public void RegisterForAnticipationEvents(IAnticipationEventReceiver receiver)
        {
            m_AnticipationEventReceivers.Add(receiver);
        }
        public void DeregisterForAnticipationEvents(IAnticipationEventReceiver receiver)
        {
            m_AnticipationEventReceivers.Remove(receiver);
        }

        public void SetupForUpdate()
        {
            foreach (var receiver in m_AnticipationEventReceivers)
            {
                receiver.SetupForUpdate();
            }
        }

        public void SetupForRender()
        {
            foreach (var receiver in m_AnticipationEventReceivers)
            {
                receiver.SetupForRender();
            }
        }

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
            OnReanticipate?.Invoke(ChangedVariables, ChangedBehaviours, LastAnticipationAckTime);
        }

        public void Sync()
        {
            if (NumberOfAnticipatedObjects != 0 && !m_NetworkManager.ShutdownInProgress && !m_NetworkManager.ConnectionManager.LocalClient.IsServer && m_NetworkManager.ConnectionManager.LocalClient.IsConnected)
            {
                var message = new AnticipationCounterSyncPingMessage { Counter = AnticipationCounter, Time = m_NetworkManager.LocalTime.Time };
                m_NetworkManager.MessageManager.SendMessage(ref message, NetworkDelivery.Reliable, NetworkManager.ServerClientId);
            }

            ++AnticipationCounter;
        }
    }
}
