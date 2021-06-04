using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using UnityEngine;

namespace MLAPI.RuntimeTests.Metrics
{
#if true
    public abstract class MetricsTestBase
    {
        protected static IEnumerator WaitForMetricsDispatch()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
        }

        protected static IEnumerator WaitForAFewFrames()
        {
            yield return new WaitForSeconds(0.5f);
        }

        protected static IEventMetric<TEvent> AssertSingleMetricEventOfType<TEvent>(MetricCollection collection, string name)
        {
            var namedMessageSentMetric = collection.Metrics.SingleOrDefault(x => x.Name == name);
            Assert.NotNull(namedMessageSentMetric);

            var typedMetric = namedMessageSentMetric as IEventMetric<TEvent>;
            Assert.NotNull(typedMetric);
            Assert.IsNotEmpty(typedMetric.Values);

            return typedMetric;
        }

        protected class TestObserver : IMetricObserver
        {
            private readonly Action<MetricCollection> m_Assertion;

            public TestObserver(Action<MetricCollection> assertion)
            {
                m_Assertion = assertion;
            }

            public void Observe(MetricCollection collection)
            {
                m_Assertion.Invoke(collection);
            }
        }
    }
#endif
}
