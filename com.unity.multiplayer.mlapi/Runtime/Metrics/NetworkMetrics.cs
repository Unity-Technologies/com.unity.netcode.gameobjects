using System.Collections.Generic;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;

namespace MLAPI.Metrics
{
#if MULTIPLAYER_TOOLS
    public class NetworkMetrics : INetworkMetrics
    {
        private readonly NetworkManager m_NetworkManager;

        readonly EventMetric<NamedMessageEvent> m_NamedMessageSentEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageSent);
        readonly EventMetric<NamedMessageEvent> m_NamedMessageReceivedEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageReceived);

        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageSentEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageSent);
        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageReceivedEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageReceived);

        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaSentEvent = new EventMetric<NetworkVariableEvent>(MetricNames.NetworkVariableDeltaSent);
        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaReceivedEvent = new EventMetric<NetworkVariableEvent>(MetricNames.NetworkVariableDeltaReceived);

        readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeSentEvent = new EventMetric<OwnershipChangeEvent>(MetricNames.OwnershipChangeSent);
        readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeReceivedEvent = new EventMetric<OwnershipChangeEvent>(MetricNames.OwnershipChangeReceived);

        private Dictionary<ulong, NetworkObjectIdentifier> m_NetworkGameObjects = new Dictionary<ulong, NetworkObjectIdentifier>();

        public NetworkMetrics(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            Dispatcher = new MetricDispatcherBuilder()
                .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
                .WithMetricEvents(m_UnnamedMessageSentEvent, m_UnnamedMessageReceivedEvent)
                .WithMetricEvents(m_NetworkVariableDeltaSentEvent, m_NetworkVariableDeltaReceivedEvent)
                .WithMetricEvents(m_OwnershipChangeSentEvent, m_OwnershipChangeReceivedEvent)
                .Build();
            
            Dispatcher.RegisterObserver(MLAPIObserver.Observer);
        }

        internal IMetricDispatcher Dispatcher { get; }

        public void TrackNetworkObject(NetworkObject networkObject)
        {
            if (!m_NetworkGameObjects.ContainsKey(networkObject.NetworkObjectId))
            {
                m_NetworkGameObjects[networkObject.NetworkObjectId] = new NetworkObjectIdentifier(networkObject.name, networkObject.NetworkObjectId);
            }
        }

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, ulong bytesCount)
        {
            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(new ConnectionInfo(receiverClientId), messageName, bytesCount));
        }

        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, ulong bytesCount)
        {
            foreach (var receiver in receiverClientIds)
            {
                TrackNamedMessageSent(receiver, messageName, bytesCount);
            }
        }

        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, ulong bytesCount)
        {
            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(new ConnectionInfo(senderClientId), messageName, bytesCount));
        }

        public void TrackUnnamedMessageSent(ulong receiverClientId, ulong bytesCount)
        {
            m_UnnamedMessageSentEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(receiverClientId), bytesCount));
        }

        public void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, ulong bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackUnnamedMessageSent(receiverClientId, bytesCount);
            }
        }

        public void TrackUnnamedMessageReceived(ulong senderClientId, ulong bytesCount)
        {
            m_UnnamedMessageReceivedEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(senderClientId), bytesCount));
        }
        
        public void TrackNetworkVariableDeltaSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, string variableName, ulong bytesCount)
        {
            variableName = PrettyPrintVariableName(variableName);
            m_NetworkVariableDeltaSentEvent.Mark(new NetworkVariableEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), variableName, bytesCount));
        }

        public void TrackNetworkVariableDeltaReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, string variableName, ulong bytesCount)
        {
            variableName = PrettyPrintVariableName(variableName);
            m_NetworkVariableDeltaReceivedEvent.Mark(new NetworkVariableEvent(new ConnectionInfo(senderClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), variableName, bytesCount));
        }

        public void TrackOwnershipChangeSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount)
        {
            m_OwnershipChangeSentEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackOwnershipChangeReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount)
        {
            m_OwnershipChangeReceivedEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(senderClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void DispatchFrame()
        {
            Dispatcher.Dispatch();
        }

        private static string PrettyPrintVariableName(string variableName)
        {
            return variableName.Replace("<", string.Empty).Replace(">k__BackingField", string.Empty);
        }
    }

    public class MLAPIObserver
    {
        public static IMetricObserver Observer { get; }

        static MLAPIObserver()
        {
            Observer = new NetStatObserver();
        }
    }
#endif
}
