using System.Collections.Generic;

namespace Unity.Netcode
{
    internal interface IAnticipationEventReceiver
    {
        public void SetupForUpdate();
        public void SetupForRender();
    }

    internal interface IAnticipatedObject
    {
        public void Update();
        public void ResetAnticipation();
        public NetworkObject OwnerObject { get; }
    }

    internal class AnticipationSystem
    {
        internal ulong LastAnticipationAck;
        internal double LastAnticipationAckTime;

        internal HashSet<IAnticipatedObject> AllAnticipatedObjects = new HashSet<IAnticipatedObject>();

        internal ulong AnticipationCounter;

        private NetworkManager m_NetworkManager;

        public HashSet<IAnticipatedObject> ObjectsToReanticipate = new HashSet<IAnticipatedObject>();

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
            var lastRoundTripTime = m_NetworkManager.LocalTime.Time - LastAnticipationAckTime;
            foreach (var item in ObjectsToReanticipate)
            {
                foreach (var behaviour in item.OwnerObject.ChildNetworkBehaviours)
                {
                    behaviour.OnReanticipate(lastRoundTripTime);
                }
                item.ResetAnticipation();
            }

            ObjectsToReanticipate.Clear();
            OnReanticipate?.Invoke(lastRoundTripTime);
        }

        public void Update()
        {
            foreach (var item in AllAnticipatedObjects)
            {
                item.Update();
            }
        }

        public void Sync()
        {
            if (AllAnticipatedObjects.Count != 0 && !m_NetworkManager.ShutdownInProgress && !m_NetworkManager.ConnectionManager.LocalClient.IsServer && m_NetworkManager.ConnectionManager.LocalClient.IsConnected)
            {
                var message = new AnticipationCounterSyncPingMessage { Counter = AnticipationCounter, Time = m_NetworkManager.LocalTime.Time };
                m_NetworkManager.MessageManager.SendMessage(ref message, NetworkDelivery.Reliable, NetworkManager.ServerClientId);
            }

            ++AnticipationCounter;
        }
    }
}
