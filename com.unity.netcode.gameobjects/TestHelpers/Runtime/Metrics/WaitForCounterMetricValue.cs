#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;

namespace Unity.Netcode.TestHelpers.Runtime.Metrics
{
    internal class WaitForCounterMetricValue : WaitForMetricValues<Counter>
    {
        private long m_Value;

        public delegate bool CounterFilter(long metric);
        private CounterFilter m_CounterFilterDelegate;

        public WaitForCounterMetricValue(IMetricDispatcher dispatcher, DirectionalMetricInfo directionalMetricName)
            : base(dispatcher, directionalMetricName)
        {
        }

        public WaitForCounterMetricValue(IMetricDispatcher dispatcher, DirectionalMetricInfo directionalMetricName, CounterFilter counterFilter)
            : this(dispatcher, directionalMetricName)
        {
            m_CounterFilterDelegate = counterFilter;
        }

        public long AssertMetricValueHaveBeenFound()
        {
            AssertHasError();
            AssertIsFound();

            return m_Value;
        }

        public override void Observe(MetricCollection collection)
        {
            if (FindMetric(collection, out var metric))
            {
                var typedMetric = metric as Counter;
                if (typedMetric == default)
                {
                    SetError(metric);
                    return;
                }

                m_Value = typedMetric.Value;
                m_Found = m_CounterFilterDelegate != null ? m_CounterFilterDelegate(m_Value) : true;
            }
        }
    }
}
#endif
