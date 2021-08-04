#if MULTIPLAYER_TOOLS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using UnityEngine;

namespace MLAPI.RuntimeTests.Metrics.Utility
{
    public class WaitForMetricValues<TMetric> : IMetricObserver
    {
        readonly string m_MetricName;
        bool m_Found;
        bool m_HasError;
        string m_Error;
        uint m_NbFrames = 0;
        IReadOnlyCollection<TMetric> m_Values;

        public WaitForMetricValues(IMetricDispatcher dispatcher, string metricName)
        {
            m_MetricName = metricName;

            dispatcher.RegisterObserver(this);
        }

        public IEnumerator WaitForMetricsReceived()
        {
            yield return WaitForFrames(60);
        }

        public IReadOnlyCollection<TMetric> AssertMetricValuesHaveBeenFound()
        {
            if (m_HasError)
            {
                Assert.Fail(m_Error);
            }

            if (!m_Found)
            {
                Assert.Fail($"Found no matching values for metric of type '{typeof(TMetric).Name}', with name '{m_MetricName}' during '{m_NbFrames}' frames.");
            }

            return m_Values;
        }

        public void Observe(MetricCollection collection)
        {
            if (m_Found || m_HasError)
            {
                return;
            }

            var metric = collection.Metrics.SingleOrDefault(x => x.Name == m_MetricName);
            if (metric == default)
            {
                m_HasError = true;
                m_Error = $"Metric collection does not contain metric named '{m_MetricName}'.";

                return;
            }

            var typedMetric = metric as IEventMetric<TMetric>;
            if (typedMetric == default)
            {
                m_HasError = true;
                m_Error = $"Metric collection contains a metric of type '{metric.GetType().Name}' for name '{m_MetricName}', but was expecting '{typeof(TMetric).Name}'.";

                return;
            }

            if (typedMetric.Values.Any())
            {
                m_Values = typedMetric.Values.ToList();
                m_Found = true;
            }
        }

        private IEnumerator WaitForFrames(uint maxNbFrames)
        {
            while (!m_Found && m_NbFrames < maxNbFrames)
            {
                m_NbFrames++;
                yield return null;
            }
        }
    }
}
#endif
