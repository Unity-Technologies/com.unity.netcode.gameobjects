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

        public WaitForMetricValues(IMetricDispatcher dispatcher, string metricName)
        {
            m_MetricName = metricName;

            dispatcher.RegisterObserver(this);
        }

        public IReadOnlyCollection<TMetric> Values { get; private set; }

        public IEnumerator WaitForMetricsDispatch()
        {
            yield return Wait(2);
        }

        public IEnumerator WaitForAFewFrames()
        {
            yield return Wait(20);
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
                Values = typedMetric.Values.ToList();
                m_Found = true;
            }
        }

        private IEnumerator Wait(uint maxNbFrames)
        {
            var frame = 0U;
            while (!m_Found && frame < maxNbFrames)
            {
                frame++;
                yield return new WaitForEndOfFrame();
            }

            if (!m_Found)
            {
                Assert.Fail($"Found no matching values for metric of type '{typeof(TMetric).Name}', with name '{m_MetricName}' during '{maxNbFrames}' frames.");
            }
        }
    }
}