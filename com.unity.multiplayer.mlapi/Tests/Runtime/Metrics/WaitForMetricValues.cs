using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using UnityEngine;

namespace MLAPI.RuntimeTests.Metrics
{
    public class WaitForMetricValues<TMetric> : IMetricObserver
    {
        private readonly string m_MetricName;
        private bool m_Found = false;
        private bool m_HasError = false;
        private string m_Error = string.Empty;
        private uint m_NbFrames = 0;
        private IReadOnlyCollection<TMetric> m_Values;

        public WaitForMetricValues(IMetricDispatcher dispatcher, string metricName)
        {
            m_MetricName = metricName;

            dispatcher.RegisterObserver(this);
        }

        public IEnumerator WaitForMetricsDispatch()
        {
            yield return Wait(2);
        }

        public IEnumerator WaitForAFewFrames()
        {
            yield return Wait(20);
        }

        public IReadOnlyCollection<TMetric> EnsureMetricValuesHaveBeenFound()
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
            if (m_Found || m_HasError)
            {
                return;
            }

            if (collection.Metrics.Count(x => x.Name == m_MetricName) > 1)
            {
                m_HasError = true;
                m_Error = $"Multiple matches found for metric with name '{m_MetricName}'.";

                return;
            }

            var metric = collection.Metrics.FirstOrDefault(x => x.Name == m_MetricName);
            if (metric == default)
            {
                m_HasError = true;
                m_Error = $"Metric with name '{m_MetricName}' was not found.";

                return;
            }

            var typedMetric = metric as IEventMetric<TMetric>;
            if (typedMetric == null)
            {
                m_HasError = true;
                m_Error = $"Metric with name '{m_MetricName}' was expected to be of type '{typeof(TMetric).Name}' but was '{metric.GetType().Name}'.";

                return;
            }

            if (typedMetric.Values.Any())
            {
                m_Values = typedMetric.Values.ToList();
                m_Found = true;
            }
        }

        public IEnumerator Wait(uint maxNbFrames)
        {
            while (!m_Found && m_NbFrames < maxNbFrames && !m_HasError)
            {
                m_NbFrames++;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
