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
            if (!m_Found)
            {
                Assert.Fail($"Found no matching values for metric of type '{typeof(TMetric).Name}', with name '{m_MetricName}' during '{m_NbFrames}' frames.");
            }

            return m_Values;
        }

        public void Observe(MetricCollection collection)
        {
            if (m_Found)
            {
                return;
            }

            var metric = collection.Metrics.SingleOrDefault(x => x.Name == m_MetricName);
            Assert.NotNull(metric);

            var typedMetric = metric as IEventMetric<TMetric>;
            Assert.NotNull(typedMetric);

            if (typedMetric.Values.Any())
            {
                m_Values = typedMetric.Values.ToList();
                m_Found = true;
            }
        }

        private IEnumerator Wait(uint maxNbFrames)
        {
            while (!m_Found && m_NbFrames < maxNbFrames)
            {
                m_NbFrames++;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
