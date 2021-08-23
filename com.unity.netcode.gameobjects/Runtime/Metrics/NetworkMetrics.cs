#if MULTIPLAYER_TOOLS
using System.Collections.Generic;
using Unity.Multiplayer.MetricTypes;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetStatsReporting;

namespace Unity.Netcode
{
    internal class NetworkMetrics : INetworkMetrics
    {
        readonly EventMetric<NamedMessageEvent> m_NamedMessageSentEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageSent);
        readonly EventMetric<NamedMessageEvent> m_NamedMessageReceivedEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageReceived);
        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageSentEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageSent);
        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageReceivedEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageReceived);
        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaSentEvent = new EventMetric<NetworkVariableEvent>(MetricNames.NetworkVariableDeltaSent);
        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaReceivedEvent = new EventMetric<NetworkVariableEvent>(MetricNames.NetworkVariableDeltaReceived);
        readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeSentEvent = new EventMetric<OwnershipChangeEvent>(MetricNames.OwnershipChangeSent);
        readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeReceivedEvent = new EventMetric<OwnershipChangeEvent>(MetricNames.OwnershipChangeReceived);
        readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnSentEvent = new EventMetric<ObjectSpawnedEvent>(MetricNames.ObjectSpawnedSent);
        readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnReceivedEvent = new EventMetric<ObjectSpawnedEvent>(MetricNames.ObjectSpawnedReceived);
        readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroySentEvent = new EventMetric<ObjectDestroyedEvent>(MetricNames.ObjectDestroyedSent);
        readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroyReceivedEvent = new EventMetric<ObjectDestroyedEvent>(MetricNames.ObjectDestroyedReceived);
        readonly EventMetric<RpcEvent> m_RpcSentEvent = new EventMetric<RpcEvent>(MetricNames.RpcSent);
        readonly EventMetric<RpcEvent> m_RpcReceivedEvent = new EventMetric<RpcEvent>(MetricNames.RpcReceived);
        readonly EventMetric<ServerLogEvent> m_ServerLogSentEvent = new EventMetric<ServerLogEvent>(MetricNames.ServerLogSent);
        readonly EventMetric<ServerLogEvent> m_ServerLogReceivedEvent = new EventMetric<ServerLogEvent>(MetricNames.ServerLogReceived);

        readonly Dictionary<ulong, NetworkObjectIdentifier> m_NetworkGameObjects = new Dictionary<ulong, NetworkObjectIdentifier>();

        public NetworkMetrics()
        {
            Dispatcher = new MetricDispatcherBuilder()
                .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
                .WithMetricEvents(m_UnnamedMessageSentEvent, m_UnnamedMessageReceivedEvent)
                .WithMetricEvents(m_NetworkVariableDeltaSentEvent, m_NetworkVariableDeltaReceivedEvent)
                .WithMetricEvents(m_OwnershipChangeSentEvent, m_OwnershipChangeReceivedEvent)
                .WithMetricEvents(m_ObjectSpawnSentEvent, m_ObjectSpawnReceivedEvent)
                .WithMetricEvents(m_ObjectDestroySentEvent, m_ObjectDestroyReceivedEvent)
                .WithMetricEvents(m_RpcSentEvent, m_RpcReceivedEvent)
                .WithMetricEvents(m_ServerLogSentEvent, m_ServerLogReceivedEvent)
                .Build();

            Dispatcher.RegisterObserver(NetcodeObserver.Observer);
        }

        internal IMetricDispatcher Dispatcher { get; }

        public void TrackNetworkObject(NetworkObject networkObject)
        {
            if (!m_NetworkGameObjects.ContainsKey(networkObject.NetworkObjectId))
            {
                m_NetworkGameObjects[networkObject.NetworkObjectId] = new NetworkObjectIdentifier(networkObject.name, networkObject.NetworkObjectId);
            }
        }

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount)
        {
            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(new ConnectionInfo(receiverClientId), messageName, bytesCount));
        }

        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, long bytesCount)
        {
            foreach (var receiver in receiverClientIds)
            {
                TrackNamedMessageSent(receiver, messageName, bytesCount);
            }
        }

        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, long bytesCount)
        {
            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(new ConnectionInfo(senderClientId), messageName, bytesCount));
        }

        public void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount)
        {
            m_UnnamedMessageSentEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(receiverClientId), bytesCount));
        }

        public void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackUnnamedMessageSent(receiverClientId, bytesCount);
            }
        }

        public void TrackUnnamedMessageReceived(ulong senderClientId, long bytesCount)
        {
            m_UnnamedMessageReceivedEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(senderClientId), bytesCount));
        }

        public void TrackNetworkVariableDeltaSent(
            ulong receiverClientId,
            ulong networkObjectId,
            string gameObjectName,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_NetworkVariableDeltaSentEvent.Mark(
                new NetworkVariableEvent(
                    new ConnectionInfo(receiverClientId),
                    new NetworkObjectIdentifier(gameObjectName, networkObjectId),
                    variableName,
                    networkBehaviourName,
                    bytesCount));
        }

        public void TrackNetworkVariableDeltaReceived(
            ulong senderClientId,
            ulong networkObjectId,
            string gameObjectName,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_NetworkVariableDeltaReceivedEvent.Mark(
                new NetworkVariableEvent(
                    new ConnectionInfo(senderClientId),
                    new NetworkObjectIdentifier(gameObjectName, networkObjectId),
                    variableName,
                    networkBehaviourName,
                    bytesCount));
        }

        public void TrackOwnershipChangeSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            m_OwnershipChangeSentEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackOwnershipChangeReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            m_OwnershipChangeReceivedEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(senderClientId),
                new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackObjectSpawnSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            m_ObjectSpawnSentEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackObjectSpawnReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            m_ObjectSpawnReceivedEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(senderClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackObjectDestroySent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            m_ObjectDestroySentEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackObjectDestroySent(IReadOnlyCollection<ulong> receiverClientIds, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackObjectDestroySent(receiverClientId, networkObjectId, gameObjectName, bytesCount);
            }
        }

        public void TrackObjectDestroyReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            m_ObjectDestroyReceivedEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(senderClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));
        }

        public void TrackRpcSent(
            ulong receiverClientId,
            ulong networkObjectId,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!m_NetworkGameObjects.TryGetValue(networkObjectId, out var networkObjectIdentifier))
            {
                networkObjectIdentifier = new NetworkObjectIdentifier("", networkObjectId);
            }

            m_RpcSentEvent.Mark(
                new RpcEvent(
                    new ConnectionInfo(receiverClientId),
                    networkObjectIdentifier,
                    rpcName,
                    networkBehaviourName,
                    bytesCount));
        }

        public void TrackRpcSent(
            ulong[] receiverClientIds,
            ulong networkObjectId,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackRpcSent(receiverClientId, networkObjectId, rpcName, networkBehaviourName, bytesCount);
            }
        }

        public void TrackRpcReceived(
            ulong senderClientId,
            ulong networkObjectId,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!m_NetworkGameObjects.TryGetValue(networkObjectId, out var networkObjectIdentifier))
            {
                networkObjectIdentifier = new NetworkObjectIdentifier("", networkObjectId);
            }

            m_RpcReceivedEvent.Mark(
                new RpcEvent(new ConnectionInfo(senderClientId),
                    networkObjectIdentifier,
                    rpcName,
                    networkBehaviourName,
                    bytesCount));
        }

        public void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount)
        {
            m_ServerLogSentEvent.Mark(new ServerLogEvent(new ConnectionInfo(receiverClientId), (Unity.Multiplayer.MetricTypes.LogLevel)logType, bytesCount));
        }

        public void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount)
        {
            m_ServerLogReceivedEvent.Mark(new ServerLogEvent(new ConnectionInfo(senderClientId), (Unity.Multiplayer.MetricTypes.LogLevel)logType, bytesCount));
        }

        public void DispatchFrame()
        {
            Dispatcher.Dispatch();
        }
    }

    public class NetcodeObserver
    {
        public static IMetricObserver Observer { get; } = MetricObserverFactory.Construct();
    }
}
#endif
