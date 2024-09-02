using System;
using System.Collections.Generic;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;

namespace Unity.Multiplayer.Tools.Adapters.Ngo1
{
    internal class ObjectRpcCountCache
    {
        private Dictionary<ObjectId, int> m_MostRecentRpcCount = new Dictionary<ObjectId, int>();

        public int GetRpcCount(ObjectId objectId) =>
            m_MostRecentRpcCount.TryGetValue(objectId, out var bandwidth) ? bandwidth : 0;

        public void Update(MetricCollection collection)
        {
            m_MostRecentRpcCount.Clear();

            LookupAndCountRpcs(collection, DirectedMetricType.RpcSent);
            LookupAndCountRpcs(collection, DirectedMetricType.RpcReceived);
        }

        private void LookupAndCountRpcs(MetricCollection collection, DirectedMetricType metricType)
        {
            var metricId = MetricId.Create(metricType);
            var events = collection.TryGetEvent<RpcEvent>(metricId, out var metric)
                ? metric.Values
                : Array.Empty<RpcEvent>();
            CountRpcs(events);
        }

        private void CountRpcs(IReadOnlyList<RpcEvent> rpcs)
        {
            foreach (var rpc in rpcs)
            {
                var objectId = (ObjectId)rpc.NetworkId.NetworkId;
                if (m_MostRecentRpcCount.ContainsKey(objectId))
                {
                    m_MostRecentRpcCount[objectId] += 1;
                }
                else
                {
                    m_MostRecentRpcCount[objectId] = 1;
                }
            }
        }
    }
}
