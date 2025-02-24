#if MULTIPLAYER_TOOLS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;

namespace Unity.Netcode.TestHelpers.Runtime.Metrics
{
    internal abstract class WaitForMetricValues<TMetric> : IMetricObserver
    {
        protected readonly string m_MetricName;
        protected bool m_Found;
        protected bool m_HasError;
        protected string m_Error;
        protected uint m_NbFrames = 0;

        public WaitForMetricValues(IMetricDispatcher dispatcher, DirectionalMetricInfo directionalMetricName)
        {
            m_MetricName = directionalMetricName.Id;
            dispatcher.RegisterObserver(this);
        }

        public abstract void Observe(MetricCollection collection);

        public void AssertMetricValuesHaveNotBeenFound()
        {
            if (m_HasError)
            {
                Assert.Fail(m_Error);
            }

            if (!m_Found)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail();
            }
        }

        public IEnumerator WaitForMetricsReceived()
        {
            yield return WaitForFrames(60);
        }

        protected void AssertHasError()
        {
            if (m_HasError)
            {
                Assert.Fail(m_Error);
            }
        }

        protected void AssertIsFound()
        {
            if (!m_Found)
            {
                Assert.Fail($"Found no matching values for metric of type '{typeof(TMetric).Name}', with name '{m_MetricName}' during '{m_NbFrames}' frames.");
            }
        }

        protected bool FindMetric(MetricCollection collection, out IMetric metric)
        {
            if (m_Found || m_HasError)
            {
                metric = null;
                return false;
            }

            metric = collection.Metrics.SingleOrDefault(x => x.Name == m_MetricName);
            if (metric == default)
            {
                m_HasError = true;
                m_Error = $"Metric collection does not contain metric named '{m_MetricName}'.";

                return false;
            }

            return true;
        }

        protected void SetError(IMetric metric)
        {
            m_HasError = true;
            m_Error = $"Metric collection contains a metric of type '{metric.GetType().Name}' for name '{m_MetricName}', but was expecting '{typeof(TMetric).Name}'.";
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
