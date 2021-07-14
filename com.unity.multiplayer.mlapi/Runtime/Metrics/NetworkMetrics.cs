#if MULTIPLAYER_TOOLS

using System.Collections.Generic;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;

namespace MLAPI.Metrics
{
    public class NetworkMetrics : INetworkMetrics
    {
        private readonly NetworkManager m_NetworkManager;

        readonly EventMetric<NamedMessageEvent> m_NamedMessageSentEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageSent);
        readonly EventMetric<NamedMessageEvent> m_NamedMessageReceivedEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageReceived);

        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageSentEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageSent);
        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageReceivedEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageReceived);

        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaSentEvent = new EventMetric<NetworkVariableEvent>(MetricNames.NetworkVariableDeltaSent);
        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaReceivedEvent = new EventMetric<NetworkVariableEvent>(MetricNames.NetworkVariableDeltaReceived);

        private readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnSentEvent = new EventMetric<ObjectSpawnedEvent>(MetricNames.ObjectSpawnedSent);
        private readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnReceivedEvent = new EventMetric<ObjectSpawnedEvent>(MetricNames.ObjectSpawnedReceived);
        private readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroySentEvent = new EventMetric<ObjectDestroyedEvent>(MetricNames.ObjectDestroyedSent);
        private readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroyReceivedEvent = new EventMetric<ObjectDestroyedEvent>(MetricNames.ObjectDestroyedReceived);

        readonly EventMetric<RpcEvent> m_RpcSentEvent = new EventMetric<RpcEvent>(MetricNames.RpcSent);
        readonly EventMetric<RpcEvent> m_RpcReceivedEvent = new EventMetric<RpcEvent>(MetricNames.RpcReceived);

        private Dictionary<ulong, NetworkObjectIdentifier> m_NetworkGameObjects = new Dictionary<ulong, NetworkObjectIdentifier>();

        public NetworkMetrics(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            Dispatcher = new MetricDispatcherBuilder()
                .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
                .WithMetricEvents(m_UnnamedMessageSentEvent, m_UnnamedMessageReceivedEvent)
                .WithMetricEvents(m_NetworkVariableDeltaSentEvent, m_NetworkVariableDeltaReceivedEvent)
                .WithMetricEvents(m_ObjectSpawnSentEvent, m_ObjectSpawnReceivedEvent)
                .WithMetricEvents(m_ObjectDestroySentEvent, m_ObjectDestroyReceivedEvent)
                .WithMetricEvents(m_RpcSentEvent, m_RpcReceivedEvent)
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

        public void TrackObjectSpawnSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount)
        {
            m_ObjectSpawnSentEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackObjectSpawnReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount)
        {
            m_ObjectSpawnReceivedEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(senderClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackObjectDestroySent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount)
        {
            m_ObjectDestroySentEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackObjectDestroySent(IReadOnlyCollection<ulong> receiverClientIds, ulong networkObjectId, string gameObjectName, ulong bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackObjectDestroySent(receiverClientId, networkObjectId, gameObjectName, bytesCount);
            }
        }

        public void TrackObjectDestroyReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, ulong bytesCount)
        {
            m_ObjectDestroyReceivedEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(senderClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackRpcSent(ulong receiverClientId, ulong networkObjectId, string rpcName, ulong bytesCount)
        {
            if (!m_NetworkGameObjects.TryGetValue(networkObjectId, out var networkObjectIdentifier))
            {
                networkObjectIdentifier = new NetworkObjectIdentifier("", networkObjectId);
            }

            m_RpcSentEvent.Mark(new RpcEvent(new ConnectionInfo(receiverClientId), networkObjectIdentifier, rpcName, bytesCount));
        }

        public void TrackRpcSent(ulong[] receiverClientIds, ulong networkObjectId, string rpcName, ulong bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackRpcSent(receiverClientId, networkObjectId, rpcName, bytesCount);
            }
        }

        public void TrackRpcReceived(ulong senderClientId, ulong networkObjectId, string rpcName, ulong bytesCount)
        {
            if (!m_NetworkGameObjects.TryGetValue(networkObjectId, out var networkObjectIdentifier))
            {
                networkObjectIdentifier = new NetworkObjectIdentifier("", networkObjectId);
            }

            m_RpcReceivedEvent.Mark(new RpcEvent(new ConnectionInfo(senderClientId), networkObjectIdentifier, rpcName, bytesCount));
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
}

#endif
