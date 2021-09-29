#if MULTIPLAYER_TOOLS
using System.Collections.Generic;
using Unity.Multiplayer.Tools;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;

namespace Unity.Netcode
{
    internal class NetworkMetrics : INetworkMetrics
    {
        readonly Counter m_TransportBytesSent = new Counter(NetworkMetricTypes.TotalBytesSent.Id)
        {
            ShouldResetOnDispatch = true,
        };
        readonly Counter m_TransportBytesReceived = new Counter(NetworkMetricTypes.TotalBytesReceived.Id)
        {
            ShouldResetOnDispatch = true,
        };

        readonly EventMetric<NetworkMessageEvent> m_NetworkMessageSentEvent = new EventMetric<NetworkMessageEvent>(NetworkMetricTypes.NetworkMessageSent.Id);
        readonly EventMetric<NetworkMessageEvent> m_NetworkMessageReceivedEvent = new EventMetric<NetworkMessageEvent>(NetworkMetricTypes.NetworkMessageReceived.Id);
        readonly EventMetric<NamedMessageEvent> m_NamedMessageSentEvent = new EventMetric<NamedMessageEvent>(NetworkMetricTypes.NamedMessageSent.Id);
        readonly EventMetric<NamedMessageEvent> m_NamedMessageReceivedEvent = new EventMetric<NamedMessageEvent>(NetworkMetricTypes.NamedMessageReceived.Id);
        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageSentEvent = new EventMetric<UnnamedMessageEvent>(NetworkMetricTypes.UnnamedMessageSent.Id);
        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageReceivedEvent = new EventMetric<UnnamedMessageEvent>(NetworkMetricTypes.UnnamedMessageReceived.Id);
        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaSentEvent = new EventMetric<NetworkVariableEvent>(NetworkMetricTypes.NetworkVariableDeltaSent.Id);
        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaReceivedEvent = new EventMetric<NetworkVariableEvent>(NetworkMetricTypes.NetworkVariableDeltaReceived.Id);
        readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeSentEvent = new EventMetric<OwnershipChangeEvent>(NetworkMetricTypes.OwnershipChangeSent.Id);
        readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeReceivedEvent = new EventMetric<OwnershipChangeEvent>(NetworkMetricTypes.OwnershipChangeReceived.Id);
        readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnSentEvent = new EventMetric<ObjectSpawnedEvent>(NetworkMetricTypes.ObjectSpawnedSent.Id);
        readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnReceivedEvent = new EventMetric<ObjectSpawnedEvent>(NetworkMetricTypes.ObjectSpawnedReceived.Id);
        readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroySentEvent = new EventMetric<ObjectDestroyedEvent>(NetworkMetricTypes.ObjectDestroyedSent.Id);
        readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroyReceivedEvent = new EventMetric<ObjectDestroyedEvent>(NetworkMetricTypes.ObjectDestroyedReceived.Id);
        readonly EventMetric<RpcEvent> m_RpcSentEvent = new EventMetric<RpcEvent>(NetworkMetricTypes.RpcSent.Id);
        readonly EventMetric<RpcEvent> m_RpcReceivedEvent = new EventMetric<RpcEvent>(NetworkMetricTypes.RpcReceived.Id);
        readonly EventMetric<ServerLogEvent> m_ServerLogSentEvent = new EventMetric<ServerLogEvent>(NetworkMetricTypes.ServerLogSent.Id);
        readonly EventMetric<ServerLogEvent> m_ServerLogReceivedEvent = new EventMetric<ServerLogEvent>(NetworkMetricTypes.ServerLogReceived.Id);
        readonly EventMetric<SceneEventMetric> m_SceneEventSentEvent = new EventMetric<SceneEventMetric>(NetworkMetricTypes.SceneEventSent.Id);
        readonly EventMetric<SceneEventMetric> m_SceneEventReceivedEvent = new EventMetric<SceneEventMetric>(NetworkMetricTypes.SceneEventReceived.Id);

        readonly Dictionary<ulong, NetworkObjectIdentifier> m_NetworkGameObjects = new Dictionary<ulong, NetworkObjectIdentifier>();

        public NetworkMetrics()
        {
            Dispatcher = new MetricDispatcherBuilder()
                .WithCounters(m_TransportBytesSent, m_TransportBytesReceived)
                .WithMetricEvents(m_NetworkMessageSentEvent, m_NetworkMessageReceivedEvent)
                .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
                .WithMetricEvents(m_UnnamedMessageSentEvent, m_UnnamedMessageReceivedEvent)
                .WithMetricEvents(m_NetworkVariableDeltaSentEvent, m_NetworkVariableDeltaReceivedEvent)
                .WithMetricEvents(m_OwnershipChangeSentEvent, m_OwnershipChangeReceivedEvent)
                .WithMetricEvents(m_ObjectSpawnSentEvent, m_ObjectSpawnReceivedEvent)
                .WithMetricEvents(m_ObjectDestroySentEvent, m_ObjectDestroyReceivedEvent)
                .WithMetricEvents(m_RpcSentEvent, m_RpcReceivedEvent)
                .WithMetricEvents(m_ServerLogSentEvent, m_ServerLogReceivedEvent)
                .WithMetricEvents(m_SceneEventSentEvent, m_SceneEventReceivedEvent)
                .Build();

            Dispatcher.RegisterObserver(NetcodeObserver.Observer);
        }

        internal IMetricDispatcher Dispatcher { get; }

        public void TrackTransportBytesSent(long bytesCount)
        {
            m_TransportBytesSent.Increment(bytesCount);
        }

        public void TrackTransportBytesReceived(long bytesCount)
        {
            m_TransportBytesReceived.Increment(bytesCount);
        }

        public void TrackNetworkObject(NetworkObject networkObject)
        {
            if (!m_NetworkGameObjects.ContainsKey(networkObject.NetworkObjectId))
            {
                m_NetworkGameObjects[networkObject.NetworkObjectId] = new NetworkObjectIdentifier(networkObject.name, networkObject.NetworkObjectId);
            }
        }

        public void TrackNetworkMessageSent(ulong receivedClientId, string messageType, long bytesCount)
        {
            m_NetworkMessageSentEvent.Mark(new NetworkMessageEvent(new ConnectionInfo(receivedClientId), messageType, bytesCount));
        }

        public void TrackNetworkMessageReceived(ulong senderClientId, string messageType, long bytesCount)
        {
            m_NetworkMessageReceivedEvent.Mark(new NetworkMessageEvent(new ConnectionInfo(senderClientId), messageType, bytesCount));
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
            m_ServerLogSentEvent.Mark(new ServerLogEvent(new ConnectionInfo(receiverClientId), (Unity.Multiplayer.Tools.MetricTypes.LogLevel)logType, bytesCount));
        }

        public void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount)
        {
            m_ServerLogReceivedEvent.Mark(new ServerLogEvent(new ConnectionInfo(senderClientId), (Unity.Multiplayer.Tools.MetricTypes.LogLevel)logType, bytesCount));
        }

        public void TrackSceneEventSent(IReadOnlyList<ulong> receiverClientIds, uint sceneEventType, string sceneName, long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackSceneEventSent(receiverClientId, sceneEventType, sceneName, bytesCount);
            }
        }

        public void TrackSceneEventSent(ulong receiverClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            m_SceneEventSentEvent.Mark(new SceneEventMetric(new ConnectionInfo(receiverClientId), (SceneEventType)sceneEventType, sceneName, bytesCount));
        }

        public void TrackSceneEventReceived(ulong senderClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            m_SceneEventReceivedEvent.Mark(new SceneEventMetric(new ConnectionInfo(senderClientId), (SceneEventType)sceneEventType, sceneName, bytesCount));
        }

        public void DispatchFrame()
        {
            Dispatcher.Dispatch();
        }
    }

    internal class NetcodeObserver
    {
        public static IMetricObserver Observer { get; } = MetricObserverFactory.Construct();
    }
}
#endif
