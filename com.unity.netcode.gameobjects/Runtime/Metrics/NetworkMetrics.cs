#if MULTIPLAYER_TOOLS
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Multiplayer.Tools;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Netcode
{
    internal class NetworkMetrics : INetworkMetrics
    {
        private const ulong k_MaxMetricsPerFrame = 1000L;
        private static Dictionary<uint, string> s_SceneEventTypeNames;
        private static ProfilerMarker s_FrameDispatch = new($"{nameof(NetworkMetrics)}.DispatchFrame");

        static NetworkMetrics()
        {
            ResetCounters();

            s_SceneEventTypeNames = new Dictionary<uint, string>();
            foreach (SceneEventType type in Enum.GetValues(typeof(SceneEventType)))
            {
                s_SceneEventTypeNames[(uint)type] = type.ToString();
            }
        }

        private static string GetSceneEventTypeName(uint typeCode)
        {
            if (!s_SceneEventTypeNames.TryGetValue(typeCode, out string name))
            {
                name = "Unknown";
            }

            return name;
        }

//         private static ProfilerCounterValue<float> s_TotalBytesSent = new (ProfilerCategory.Network, $"{NetworkMetricTypes.TotalBytesSent.Id.Name} {NetworkMetricTypes.TotalBytesSent.Id.TypeIndex} {NetworkMetricTypes.TotalBytesSent.Id.EnumValue}", ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//         private static ProfilerCounterValue<float> s_TotalBytesReceived = new (ProfilerCategory.Network, $"{NetworkMetricTypes.TotalBytesReceived.Id.Name} {NetworkMetricTypes.TotalBytesReceived.Id.TypeIndex} {NetworkMetricTypes.TotalBytesReceived.Id.EnumValue}", ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
// #if MULTIPLAYER_TOOLS_1_0_0_PRE_7
//         private static ProfilerCounterValue<float> s_PacketsSent = new(ProfilerCategory.Network, $"{NetworkMetricTypes.PacketsSent.Id.Name} {NetworkMetricTypes.PacketsSent.Id.TypeIndex} {NetworkMetricTypes.PacketsSent.Id.EnumValue}", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//         private static ProfilerCounterValue<float> s_PacketsReceived = new(ProfilerCategory.Network, $"{NetworkMetricTypes.PacketsReceived.Id.Name} {NetworkMetricTypes.PacketsReceived.Id.TypeIndex} {NetworkMetricTypes.PacketsReceived.Id.EnumValue}", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
// #endif
//         private static ProfilerCounterValue<float> s_RpcSent = new(ProfilerCategory.Network, $"{NetworkMetricTypes.RpcSent.Id.Name} {NetworkMetricTypes.RpcSent.Id.TypeIndex} {NetworkMetricTypes.RpcSent.Id.EnumValue}", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
//         private static ProfilerCounterValue<float> s_RpcReceived = new(ProfilerCategory.Network, $"{NetworkMetricTypes.RpcReceived.Id.Name} {NetworkMetricTypes.RpcReceived.Id.TypeIndex} {NetworkMetricTypes.RpcReceived.Id.EnumValue}", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static ProfilerCounterValue<float> s_TotalBytesSent = new (ProfilerCategory.Network, NetworkMetricTypes.TotalBytesSent.Id.Name, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        private static ProfilerCounterValue<float> s_TotalBytesReceived = new (ProfilerCategory.Network, NetworkMetricTypes.TotalBytesReceived.Id.Name, ProfilerMarkerDataUnit.Bytes, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
        private static ProfilerCounterValue<float> s_PacketsSent = new(ProfilerCategory.Network, NetworkMetricTypes.PacketsSent.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        private static ProfilerCounterValue<float> s_PacketsReceived = new(ProfilerCategory.Network, NetworkMetricTypes.PacketsReceived.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
#endif
        private static ProfilerCounterValue<float> s_RpcSent = new(ProfilerCategory.Network, NetworkMetricTypes.RpcSent.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        private static ProfilerCounterValue<float> s_RpcReceived = new(ProfilerCategory.Network, NetworkMetricTypes.RpcReceived.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private static ProfilerCounterValue<float> s_RttToServer = new(ProfilerCategory.Network, NetworkMetricTypes.RttToServer.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
        private static ProfilerCounterValue<float> s_NetworkObjects = new(ProfilerCategory.Network, NetworkMetricTypes.NetworkObjects.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
        private static ProfilerCounterValue<float> s_ConnectedClients = new(ProfilerCategory.Network, NetworkMetricTypes.ConnectedClients.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
        private static ProfilerCounterValue<float> s_PacketLoss = new(ProfilerCategory.Network, NetworkMetricTypes.PacketLoss.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

        private static ProfilerCounterValue<float> s_NetworkVariableDeltaSent = new(ProfilerCategory.Network, NetworkMetricTypes.NetworkVariableDeltaSent.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        private static ProfilerCounterValue<float> s_NetworkVariableDeltaReceived = new(ProfilerCategory.Network, NetworkMetricTypes.NetworkVariableDeltaReceived.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        private static ProfilerCounterValue<int> s_NetworkVariableDeltaCount = new(ProfilerCategory.Network, NetworkMetricTypes.NetworkVariableDeltaReceived.Id.Name + "Count", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);


        // public struct NetworkIdNetworkVariableDeltaSent
        // {
        //     public ulong networkId;
        //     public float bytesCount;
        // }
        //
        // private static ProfilerCounterValue<NetworkIdNetworkVariableDeltaSent> s_NetworkVariableDeltaReceivedWithId = new(ProfilerCategory.Network, NetworkMetricTypes.NetworkVariableDeltaReceived.Id.Name, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        private readonly Counter m_TransportBytesSent = new Counter(NetworkMetricTypes.TotalBytesSent.Id)
        {
            ShouldResetOnDispatch = true,
        };
        private readonly Counter m_TransportBytesReceived = new Counter(NetworkMetricTypes.TotalBytesReceived.Id)
        {
            ShouldResetOnDispatch = true,
        };

        private readonly EventMetric<NetworkMessageEvent> m_NetworkMessageSentEvent = new EventMetric<NetworkMessageEvent>(NetworkMetricTypes.NetworkMessageSent.Id);
        private readonly EventMetric<NetworkMessageEvent> m_NetworkMessageReceivedEvent = new EventMetric<NetworkMessageEvent>(NetworkMetricTypes.NetworkMessageReceived.Id);
        private readonly EventMetric<NamedMessageEvent> m_NamedMessageSentEvent = new EventMetric<NamedMessageEvent>(NetworkMetricTypes.NamedMessageSent.Id);
        private readonly EventMetric<NamedMessageEvent> m_NamedMessageReceivedEvent = new EventMetric<NamedMessageEvent>(NetworkMetricTypes.NamedMessageReceived.Id);
        private readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageSentEvent = new EventMetric<UnnamedMessageEvent>(NetworkMetricTypes.UnnamedMessageSent.Id);
        private readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageReceivedEvent = new EventMetric<UnnamedMessageEvent>(NetworkMetricTypes.UnnamedMessageReceived.Id);
        private readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaSentEvent = new EventMetric<NetworkVariableEvent>(NetworkMetricTypes.NetworkVariableDeltaSent.Id);
        private readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaReceivedEvent = new EventMetric<NetworkVariableEvent>(NetworkMetricTypes.NetworkVariableDeltaReceived.Id);
        private readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeSentEvent = new EventMetric<OwnershipChangeEvent>(NetworkMetricTypes.OwnershipChangeSent.Id);
        private readonly EventMetric<OwnershipChangeEvent> m_OwnershipChangeReceivedEvent = new EventMetric<OwnershipChangeEvent>(NetworkMetricTypes.OwnershipChangeReceived.Id);
        private readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnSentEvent = new EventMetric<ObjectSpawnedEvent>(NetworkMetricTypes.ObjectSpawnedSent.Id);
        private readonly EventMetric<ObjectSpawnedEvent> m_ObjectSpawnReceivedEvent = new EventMetric<ObjectSpawnedEvent>(NetworkMetricTypes.ObjectSpawnedReceived.Id);
        private readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroySentEvent = new EventMetric<ObjectDestroyedEvent>(NetworkMetricTypes.ObjectDestroyedSent.Id);
        private readonly EventMetric<ObjectDestroyedEvent> m_ObjectDestroyReceivedEvent = new EventMetric<ObjectDestroyedEvent>(NetworkMetricTypes.ObjectDestroyedReceived.Id);
        private readonly EventMetric<RpcEvent> m_RpcSentEvent = new EventMetric<RpcEvent>(NetworkMetricTypes.RpcSent.Id);
        private readonly EventMetric<RpcEvent> m_RpcReceivedEvent = new EventMetric<RpcEvent>(NetworkMetricTypes.RpcReceived.Id);
        private readonly EventMetric<ServerLogEvent> m_ServerLogSentEvent = new EventMetric<ServerLogEvent>(NetworkMetricTypes.ServerLogSent.Id);
        private readonly EventMetric<ServerLogEvent> m_ServerLogReceivedEvent = new EventMetric<ServerLogEvent>(NetworkMetricTypes.ServerLogReceived.Id);
        private readonly EventMetric<SceneEventMetric> m_SceneEventSentEvent = new EventMetric<SceneEventMetric>(NetworkMetricTypes.SceneEventSent.Id);
        private readonly EventMetric<SceneEventMetric> m_SceneEventReceivedEvent = new EventMetric<SceneEventMetric>(NetworkMetricTypes.SceneEventReceived.Id);

#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
        private readonly Counter m_PacketSentCounter = new Counter(NetworkMetricTypes.PacketsSent.Id)
        {
            ShouldResetOnDispatch = true,
        };
        private readonly Counter m_PacketReceivedCounter = new Counter(NetworkMetricTypes.PacketsReceived.Id)
        {
            ShouldResetOnDispatch = true,
        };
        private readonly Gauge m_RttToServerGauge = new Gauge(NetworkMetricTypes.RttToServer.Id)
        {
            ShouldResetOnDispatch = true,
        };
        private readonly Gauge m_NetworkObjectsGauge = new Gauge(NetworkMetricTypes.NetworkObjects.Id)
        {
            ShouldResetOnDispatch = true,
        };
        private readonly Gauge m_ConnectionsGauge = new Gauge(NetworkMetricTypes.ConnectedClients.Id)
        {
            ShouldResetOnDispatch = true,
        };
        private readonly Gauge m_PacketLossGauge = new Gauge(NetworkMetricTypes.PacketLoss.Id);
#endif

        private ulong m_NumberOfMetricsThisFrame;

        public NetworkMetrics()
        {
            ResetCounters();

            // MonobehaviourHelper.InitMonoBehaviourHelper();
            // MonobehaviourHelper.OnUpdate += ResetCounters;
            // MonobehaviourHelper.OnOneSecondPassed += ResetCountersEverySecond;

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
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
                .WithCounters(m_PacketSentCounter, m_PacketReceivedCounter)
                .WithGauges(m_RttToServerGauge)
                .WithGauges(m_NetworkObjectsGauge)
                .WithGauges(m_ConnectionsGauge)
                .WithGauges(m_PacketLossGauge)
#endif
                .Build();

            Dispatcher.RegisterObserver(NetcodeObserver.Observer);
        }

        internal IMetricDispatcher Dispatcher { get; }

        private bool CanSendMetrics => m_NumberOfMetricsThisFrame < k_MaxMetricsPerFrame;

        public void SetConnectionId(ulong connectionId)
        {
            Dispatcher.SetConnectionId(connectionId);
        }

        private readonly Queue<Tuple<long, float>> m_BytesSentQueue = new();
        public void TrackTransportBytesSent(long bytesCount)
        {
            m_TransportBytesSent.Increment(bytesCount);
            s_TotalBytesSent.Value += bytesCount;

            m_BytesSentQueue.Enqueue(new Tuple<long, float>(bytesCount, Time.realtimeSinceStartup));
            var oneSecondAgo = Time.realtimeSinceStartup - 1f;
            while (m_BytesSentQueue.Count > 0 && m_BytesSentQueue.Peek().Item2 < oneSecondAgo)
            {
                m_BytesSentQueue.Dequeue();
            }

            // Debug.Log($"NGO NetworkMetrics, Frame: {Time.frameCount} TotalBytesSent: {bytesCount}, total: {s_TransportBytesSent.Value}");
        }

        public void TrackTransportBytesReceived(long bytesCount)
        {
            m_TransportBytesReceived.Increment(bytesCount);
            s_TotalBytesReceived.Value += bytesCount;

            // Debug.Log($"NGO NetworkMetrics, Frame: {Time.frameCount} TotalBytesReceived: {bytesCount}, total: {s_TransportBytesReceived.Value}");
        }

        public void TrackNetworkMessageSent(ulong receivedClientId, string messageType, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_NetworkMessageSentEvent.Mark(new NetworkMessageEvent(new ConnectionInfo(receivedClientId), messageType, bytesCount));
            IncrementMetricCount();
        }

        public void TrackNetworkMessageReceived(ulong senderClientId, string messageType, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_NetworkMessageReceivedEvent.Mark(new NetworkMessageEvent(new ConnectionInfo(senderClientId), messageType, bytesCount));
            IncrementMetricCount();
        }

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(new ConnectionInfo(receiverClientId), messageName, bytesCount));
            IncrementMetricCount();
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
            if (!CanSendMetrics)
            {
                return;
            }

            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(new ConnectionInfo(senderClientId), messageName, bytesCount));
            IncrementMetricCount();
        }

        public void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_UnnamedMessageSentEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(receiverClientId), bytesCount));
            IncrementMetricCount();
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
            if (!CanSendMetrics)
            {
                return;
            }

            m_UnnamedMessageReceivedEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(senderClientId), bytesCount));
            IncrementMetricCount();
        }
        private static readonly int k_NetworkVarDeltaTag = 2;
        public struct NetworkVarDeltaMetadata
        {
            public static readonly Guid MyProjectId = Guid.NewGuid();
            public ulong networkId;
            public ulong receiverClientId;
            public int bytesCount;
            public uint variableNameHash;
            public uint behaviorNameHash;
            public float timestamp;
        }

        public void TrackNetworkVariableDeltaSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_NetworkVariableDeltaSentEvent.Mark(
                new NetworkVariableEvent(
                    new ConnectionInfo(receiverClientId),
                    GetObjectIdentifier(networkObject),
                    variableName,
                    networkBehaviourName,
                    bytesCount));

            s_NetworkVariableDeltaSent.Value += bytesCount;

            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.NetworkVariableDeltaSent, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});

            Debug.Log($"NGO NetworkMetrics NetworkVariableEvent.SentEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: {s_NetworkVariableDeltaSent.Value}");

            // if (Profiler.enabled)
            // {
            //     var deltaMetadata = new NetworkVarDeltaMetadata
            //     {
            //         networkId = networkObject.NetworkObjectId,
            //         receiverClientId = receiverClientId,
            //         bytesCount = (int)bytesCount,
            //         variableNameHash = (uint)variableName.GetHashCode(),
            //         behaviorNameHash = (uint)networkBehaviourName.GetHashCode(),
            //         timestamp = Time.realtimeSinceStartup
            //     };
            //
            //     Profiler.EmitFrameMetaData(, k_NetworkVarDeltaTag, new[] { deltaMetadata });
            // }

            IncrementMetricCount();
        }
        public void TrackNetworkVariableDeltaReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_NetworkVariableDeltaReceivedEvent.Mark(
                new NetworkVariableEvent(
                    new ConnectionInfo(senderClientId),
                    GetObjectIdentifier(networkObject),
                    variableName,
                    networkBehaviourName,
                    bytesCount));

            s_NetworkVariableDeltaReceived.Value += bytesCount;

            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.NetworkVariableDeltaReceived, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});
            // Profiler.EmitSessionMetaData(NetworkVariableEvent.EventId, s_NetworkVariableDeltaCount.Value, new List<NetworkVariableEvent.DataStruct>{data});

            Debug.Log($"NGO NetworkMetrics NetworkVariableEvent.ReceivedEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: {s_NetworkVariableDeltaReceived.Value}");

            // s_NetworkVariableDeltaCount.Value++;

            IncrementMetricCount();
        }

        public void TrackOwnershipChangeSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_OwnershipChangeSentEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));

            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.OwnershipChangeSent, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});

            Debug.Log($"NGO NetworkMetrics OwnershipChangeEvent.SentEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: N/A");

            IncrementMetricCount();
        }

        public void TrackOwnershipChangeReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_OwnershipChangeReceivedEvent.Mark(new OwnershipChangeEvent(new ConnectionInfo(senderClientId),
                GetObjectIdentifier(networkObject), bytesCount));

            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.OwnershipChangeReceived, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});

            Debug.Log($"NGO NetworkMetrics OwnershipChangeEvent.ReceivedEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: N/A");

            IncrementMetricCount();
        }

        public void TrackObjectSpawnSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_ObjectSpawnSentEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));

            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.ObjectSpawnedSent, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});

            Debug.Log($"NGO NetworkMetrics ObjectSpawnedEvent.SentEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: N/A");

            IncrementMetricCount();
        }

        public void TrackObjectSpawnReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_ObjectSpawnReceivedEvent.Mark(new ObjectSpawnedEvent(new ConnectionInfo(senderClientId), GetObjectIdentifier(networkObject), bytesCount));

            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.ObjectSpawnedReceived, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});

            Debug.Log($"NGO NetworkMetrics ObjectSpawnedEvent.ReceivedEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: N/A");

            IncrementMetricCount();
        }

        public void TrackObjectDestroySent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_ObjectDestroySentEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));

            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.ObjectDestroyedSent, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});

            Debug.Log($"NGO NetworkMetrics ObjectDestroyedEvent.SentEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: N/A");

            IncrementMetricCount();
        }

        public void TrackObjectDestroyReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_ObjectDestroyReceivedEvent.Mark(new ObjectDestroyedEvent(new ConnectionInfo(senderClientId), GetObjectIdentifier(networkObject), bytesCount));

            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.ObjectDestroyedReceived, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});

            Debug.Log($"NGO NetworkMetrics ObjectDestroyedEvent.ReceivedEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: N/A");

            IncrementMetricCount();
        }

        public void TrackRpcSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_RpcSentEvent.Mark(
                new RpcEvent(
                    new ConnectionInfo(receiverClientId),
                    GetObjectIdentifier(networkObject),
                    rpcName,
                    networkBehaviourName,
                    bytesCount));

            s_RpcSent.Value++;

            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.RpcSent, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});

            Debug.Log($"NGO NetworkMetrics RpcEvent.SentEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: {s_RpcSent.Value}");

            IncrementMetricCount();
        }

        public void TrackRpcSent(
            ulong[] receiverClientIds,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackRpcSent(receiverClientId, networkObject, rpcName, networkBehaviourName, bytesCount);
            }
        }

        public void TrackRpcReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_RpcReceivedEvent.Mark(
                new RpcEvent(new ConnectionInfo(senderClientId),
                    GetObjectIdentifier(networkObject),
                    rpcName,
                    networkBehaviourName,
                    bytesCount));

            s_RpcReceived.Value++;



            NetworkedObjectByteCounter data = new NetworkedObjectByteCounter {Type = DirectedMetricType.RpcReceived, NetworkId = GetObjectIdentifier(networkObject).NetworkId, BytesCount = bytesCount};
            Profiler.EmitFrameMetaData(NetworkedObjectByteCounter.Id, 0, new List<NetworkedObjectByteCounter>{data});

            Debug.Log($"NGO NetworkMetrics RpcEvent.ReceivedEvent, Frame: {Time.frameCount} Id: {GetObjectIdentifier(networkObject).NetworkId}, NetworkVariableDeltaReceived: {bytesCount}, total: {s_RpcReceived.Value}");

            IncrementMetricCount();
        }

        public void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_ServerLogSentEvent.Mark(new ServerLogEvent(new ConnectionInfo(receiverClientId), (Multiplayer.Tools.MetricTypes.LogLevel)logType, bytesCount));
            IncrementMetricCount();
        }

        public void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_ServerLogReceivedEvent.Mark(new ServerLogEvent(new ConnectionInfo(senderClientId), (Multiplayer.Tools.MetricTypes.LogLevel)logType, bytesCount));
            IncrementMetricCount();
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
            if (!CanSendMetrics)
            {
                return;
            }

            m_SceneEventSentEvent.Mark(new SceneEventMetric(new ConnectionInfo(receiverClientId), GetSceneEventTypeName(sceneEventType), sceneName, bytesCount));
            IncrementMetricCount();
        }

        public void TrackSceneEventReceived(ulong senderClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            if (!CanSendMetrics)
            {
                return;
            }

            m_SceneEventReceivedEvent.Mark(new SceneEventMetric(new ConnectionInfo(senderClientId), GetSceneEventTypeName(sceneEventType), sceneName, bytesCount));
            IncrementMetricCount();
        }

        public void TrackPacketSent(uint packetCount)
        {
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
            if (!CanSendMetrics)
            {
                return;
            }
            m_PacketSentCounter.Increment(packetCount);
            s_PacketsSent.Value += packetCount;
            IncrementMetricCount();

            // Debug.Log($"NGO NetworkMetrics, Frame: {Time.frameCount} PacketsSent: {packetCount}, total: {s_PacketsSent.Value}");
#endif
        }

        public void TrackPacketReceived(uint packetCount)
        {
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
            if (!CanSendMetrics)
            {
                return;
            }
            m_PacketReceivedCounter.Increment(packetCount);
            s_PacketsReceived.Value += packetCount;
            IncrementMetricCount();

            // Debug.Log($"NGO NetworkMetrics, Frame: {Time.frameCount} PacketsReceived: {packetCount}, total: {s_PacketsReceived.Value}");
#endif
        }


        public void UpdateRttToServer(int rttMilliseconds)
        {
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
            if (!CanSendMetrics)
            {
                return;
            }
            var rttSeconds = rttMilliseconds * 1e-3;
            m_RttToServerGauge.Set(rttSeconds);
            s_RttToServer.Value = (float)rttSeconds;
#endif
        }

        public void UpdateNetworkObjectsCount(int count)
        {
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
            if (!CanSendMetrics)
            {
                return;
            }

            m_NetworkObjectsGauge.Set(count);
            s_NetworkObjects.Value = count;
#endif
        }

        public void UpdateConnectionsCount(int count)
        {
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
            if (!CanSendMetrics)
            {
                return;
            }

            m_ConnectionsGauge.Set(count);
            s_ConnectedClients.Value = count;
#endif
        }

        public void UpdatePacketLoss(float packetLoss)
        {
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
            if (!CanSendMetrics)
            {
                return;
            }

            m_PacketLossGauge.Set(packetLoss);
            s_PacketLoss.Value = packetLoss;
#endif
        }

        private long CreateByteCounterMagic(NetworkedObjectByteCounter counter)
        {
            long result = 0;

            // Pack bits: [NetworkId 24 bits][Type 8 bits][BytesCount 32 bits]

            // NetworkId: Take lowest 24 bits and shift left by 40 positions
            result |= ((long)counter.NetworkId & 0xFFFFFF) << 40;

            // Type: Shift left by 32 positions
            result |= ((long)counter.Type & 0xFF) << 32;

            // BytesCount: Place in lowest 32 bits
            result |= (counter.BytesCount & 0xFFFFFFFF);

            return result;
        }



#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        private static void ResetCounters()
        {
            Debug.Log($"Counters reseted in frame: {Time.frameCount}");

            s_TotalBytesSent.Value = 0;
            s_TotalBytesReceived.Value = 0;
#if MULTIPLAYER_TOOLS_1_0_0_PRE_7
            s_PacketsSent.Value = 0;
            s_PacketsReceived.Value = 0;
#endif
            s_RpcSent.Value = 0;
            s_RpcReceived.Value = 0;
            s_RttToServer.Value = 0;
            s_NetworkObjects.Value = 0;
            s_ConnectedClients.Value = 0;
            s_PacketLoss.Value = 0;

            s_NetworkVariableDeltaSent.Value = 0;
            s_NetworkVariableDeltaReceived.Value = 0;
            s_NetworkVariableDeltaCount.Value = 0;
        }

        static readonly NetStatSerializer s_NetStatSerializer = new();

        public void DispatchFrame()
        {
            s_FrameDispatch.Begin();

            using var result = s_NetStatSerializer.Serialize(((MetricDispatcher)Dispatcher).MetricCollection);
            Profiler.EmitFrameMetaData(
                new Guid("42c5aec2-fb86-4172-a384-34063f1bd332"),
                0,
                result);


            Dispatcher.Dispatch();
            s_FrameDispatch.End();
            m_NumberOfMetricsThisFrame = 0;
        }

        private void IncrementMetricCount()
        {
            m_NumberOfMetricsThisFrame++;
        }

        private static NetworkObjectIdentifier GetObjectIdentifier(NetworkObject networkObject)
        {
            return new NetworkObjectIdentifier(networkObject.GetNameForMetrics(), networkObject.NetworkObjectId);
        }
    }

    internal class NetcodeObserver
    {
        public static IMetricObserver Observer { get; } = MetricObserverFactory.Construct();
    }
}
#endif
