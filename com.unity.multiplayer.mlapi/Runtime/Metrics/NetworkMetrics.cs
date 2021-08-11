#if MULTIPLAYER_TOOLS
using System.Collections.Generic;
using Unity.Multiplayer.MetricTypes;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetStatsReporting;
using UnityEngine.Profiling;

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

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackNamedMessageSent");

            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(new ConnectionInfo(receiverClientId), messageName, bytesCount));
            
            Profiler.EndSample();
        }

        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackNamedMessageSent - Multiple");

            foreach (var receiver in receiverClientIds)
            {
                TrackNamedMessageSent(receiver, messageName, bytesCount);
            }

            Profiler.EndSample();
        }

        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackNamedMessageReceived");

            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(new ConnectionInfo(senderClientId), messageName, bytesCount));

            Profiler.EndSample();
        }

        public void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackUnnamedMessageSent");

            m_UnnamedMessageSentEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(receiverClientId), bytesCount));

            Profiler.EndSample();
        }

        public void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackUnnamedMessageSent");

            foreach (var receiverClientId in receiverClientIds)
            {
                TrackUnnamedMessageSent(receiverClientId, bytesCount);
            }

            Profiler.EndSample();
        }

        public void TrackUnnamedMessageReceived(ulong senderClientId, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackUnnamedMessageReceived");

            m_UnnamedMessageReceivedEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(senderClientId), bytesCount));

            Profiler.EndSample();
        }

        public void TrackNetworkVariableDeltaSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, string variableName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackNetworkVariableDeltaSent");

            m_NetworkVariableDeltaSentEvent.Mark(new NetworkVariableEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), variableName, bytesCount));

            Profiler.EndSample();
        }

        public void TrackNetworkVariableDeltaReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, string variableName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackNetworkVariableDeltaReceived");

            m_NetworkVariableDeltaReceivedEvent.Mark(new NetworkVariableEvent(new ConnectionInfo(senderClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), variableName, bytesCount));

            Profiler.EndSample();
        }

        public void TrackOwnershipChangeSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackOwnershipChangeSent");

            m_OwnershipChangeSentEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));

            Profiler.EndSample();
        }

        public void TrackOwnershipChangeReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackOwnershipChangeReceived");

            m_OwnershipChangeReceivedEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(senderClientId),
                new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));

            Profiler.EndSample();
        }

        public void TrackObjectSpawnSent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackObjectSpawnSent");

            m_ObjectSpawnSentEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));

            Profiler.EndSample();
        }

        public void TrackObjectSpawnReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackObjectSpawnReceived");

            m_ObjectSpawnReceivedEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(senderClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));

            Profiler.EndSample();
        }

        public void TrackObjectDestroySent(ulong receiverClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackObjectDestroySent");

            m_ObjectDestroySentEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(receiverClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));

            Profiler.EndSample();
        }

        public void TrackObjectDestroySent(IReadOnlyCollection<ulong> receiverClientIds, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackObjectDestroySent - Multiple");

            foreach (var receiverClientId in receiverClientIds)
            {
                TrackObjectDestroySent(receiverClientId, networkObjectId, gameObjectName, bytesCount);
            }

            Profiler.EndSample();
        }

        public void TrackObjectDestroyReceived(ulong senderClientId, ulong networkObjectId, string gameObjectName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackObjectDestroyReceived");

            m_ObjectDestroyReceivedEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(senderClientId), new NetworkObjectIdentifier(gameObjectName, networkObjectId), bytesCount));

            Profiler.EndSample();
        }

        public void TrackRpcSent(ulong receiverClientId, ulong networkObjectId, string rpcName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackRpcSent");

            if (!m_NetworkGameObjects.TryGetValue(networkObjectId, out var networkObjectIdentifier))
            {
                networkObjectIdentifier = new NetworkObjectIdentifier("", networkObjectId);
            }

            m_RpcSentEvent.Mark(new RpcEvent(new ConnectionInfo(receiverClientId), networkObjectIdentifier, rpcName, bytesCount));

            Profiler.EndSample();
        }

        public void TrackRpcSent(ulong[] receiverClientIds, ulong networkObjectId, string rpcName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackRpcSent - Multiple");

            foreach (var receiverClientId in receiverClientIds)
            {
                TrackRpcSent(receiverClientId, networkObjectId, rpcName, bytesCount);
            }

            Profiler.EndSample();
        }

        public void TrackRpcReceived(ulong senderClientId, ulong networkObjectId, string rpcName, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackRpcReceived");

            if (!m_NetworkGameObjects.TryGetValue(networkObjectId, out var networkObjectIdentifier))
            {
                networkObjectIdentifier = new NetworkObjectIdentifier("", networkObjectId);
            }

            m_RpcReceivedEvent.Mark(new RpcEvent(new ConnectionInfo(senderClientId), networkObjectIdentifier, rpcName, bytesCount));

            Profiler.EndSample();
        }

        public void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackServerLogSent");

            m_ServerLogSentEvent.Mark(new ServerLogEvent(new ConnectionInfo(receiverClientId), (Unity.Multiplayer.MetricTypes.LogLevel)logType, bytesCount));

            Profiler.EndSample();
        }

        public void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount)
        {
            Profiler.BeginSample("NetworkMetrics - TrackServerLogReceived");

            m_ServerLogReceivedEvent.Mark(new ServerLogEvent(new ConnectionInfo(senderClientId), (Unity.Multiplayer.MetricTypes.LogLevel)logType, bytesCount));

            Profiler.EndSample();
        }

        public void DispatchFrame()
        {
            Profiler.BeginSample("NetworkMetrics - DispatchFrame");

            Dispatcher.Dispatch();

            Profiler.EndSample();
        }
    }

    public class MLAPIObserver
    {
        public static IMetricObserver Observer { get; } = MetricObserverFactory.Construct();
    }
}
#endif
