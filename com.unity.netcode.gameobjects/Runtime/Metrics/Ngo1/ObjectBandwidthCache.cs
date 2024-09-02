using System;
using System.Collections.Generic;
using Unity.Multiplayer.Tools.Common;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;

namespace Unity.Multiplayer.Tools.Adapters.Ngo1
{
    internal class ObjectBandwidthCache
    {
        public bool IsCold { get; private set; } = true;
        private readonly Dictionary<ObjectId, BytesSentAndReceived> m_OtherBandwidth = new();
        private readonly Dictionary<ObjectId, BytesSentAndReceived> m_NetVarBandwidth = new();
        private readonly Dictionary<ObjectId, BytesSentAndReceived> m_RpcBandwidth = new();

        public float GetBandwidth(
            ObjectId objectId,
            BandwidthTypes bandwidthTypes,
            NetworkDirection networkDirection)
        {
            var total = new BytesSentAndReceived();
            if ( (bandwidthTypes & BandwidthTypes.Other) != 0 &&
                m_OtherBandwidth.TryGetValue(objectId, out var otherBandwidth))
            {
                total += otherBandwidth;
            }
            if ( (bandwidthTypes & BandwidthTypes.NetVar) != 0 &&
                m_NetVarBandwidth.TryGetValue(objectId, out var netVarBandwidth))
            {
                total += netVarBandwidth;
            }
            if ( (bandwidthTypes & BandwidthTypes.Rpc) != 0 &&
                m_RpcBandwidth.TryGetValue(objectId, out var rpcBandwidth))
            {
                total += rpcBandwidth;
            }
            return total[networkDirection];
        }

        private static readonly NetworkDirection[] k_SentAndReceived = { NetworkDirection.Sent, NetworkDirection.Received };

        public void Update(MetricCollection collection)
        {
            IsCold = false;
            m_OtherBandwidth.Clear();
            m_NetVarBandwidth.Clear();
            m_RpcBandwidth.Clear();

            foreach (var direction in k_SentAndReceived)
            {
                LookupAndCountBytes<RpcEvent>(collection, direction, MetricType.Rpc, m_RpcBandwidth);
                LookupAndCountBytes<NetworkVariableEvent>(collection, direction, MetricType.NetworkVariableDelta, m_NetVarBandwidth);
                LookupAndCountBytes<ObjectSpawnedEvent>(collection, direction, MetricType.ObjectSpawned, m_OtherBandwidth);
                LookupAndCountBytes<ObjectDestroyedEvent>(collection, direction, MetricType.ObjectDestroyed, m_OtherBandwidth);
                LookupAndCountBytes<OwnershipChangeEvent>(collection, direction, MetricType.OwnershipChange, m_OtherBandwidth);
            }
        }

        private static void LookupAndCountBytes<TEvent>(
            MetricCollection collection,
            NetworkDirection direction,
            MetricType metricType,
            Dictionary<ObjectId, BytesSentAndReceived> bandwidthBuffer)
            where TEvent : INetworkMetricEvent, INetworkObjectEvent
        {
            var directedMetric = metricType.GetDirectedMetric(direction);
            var metricId = MetricId.Create(directedMetric);
            var events = collection.TryGetEvent<TEvent>(metricId, out var metric)
                ? metric.Values
                : Array.Empty<TEvent>();
            CountEventBytesForObjects(events, direction, bandwidthBuffer);
        }

        private static void CountEventBytesForObjects<TEvent>(
            IReadOnlyList<TEvent> events,
            NetworkDirection direction,
            Dictionary<ObjectId, BytesSentAndReceived> bandwidthBuffer)
            where TEvent : INetworkMetricEvent, INetworkObjectEvent
        {
            foreach (var objectEvent in events)
            {
                var objectId = (ObjectId)objectEvent.NetworkId.NetworkId;
                var bytesSentAndReceived = new BytesSentAndReceived(objectEvent.BytesCount, direction);
                if (bandwidthBuffer.TryGetValue(objectId, out var value))
                {
                    bandwidthBuffer[objectId] = value + bytesSentAndReceived;
                }
                else
                {
                    bandwidthBuffer[objectId] = bytesSentAndReceived;
                }
            }
        }
    }
}
