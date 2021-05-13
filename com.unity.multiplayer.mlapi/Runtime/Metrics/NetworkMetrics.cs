using Unity.Multiplayer.NetStats.Data;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;

namespace MLAPI.Metrics
{
    public class NetworkMetrics
    {
#if true
        private IMetricDispatcher m_Dispatcher;

        private Counter m_NbConnections = new Counter("Connections") { ShouldResetOnDispatch = false };
        private Counter m_BytesReceived = new Counter("Bytes Received");
        private Counter m_BytesSent = new Counter("Bytes Sent");
        private EventMetric<RpcEvent> m_RpcEvent = new EventMetric<RpcEvent>("RPC Event Sent");
        
#endif
        
        public NetworkMetrics()
        {
            m_Dispatcher = new MetricDispatcherBuilder()
                .WithCounters(m_NbConnections, m_BytesReceived, m_BytesSent)
                .WithGauges()
                .WithMetricEvents()
                .Build();
        }

        public void TrackRpcSent()
        {
                m_RpcEvent.Mark(new RpcEvent());
        }

        public void TrackConnectionCount(long amount)
        {
#if true
            m_NbConnections.Increment(amount);
#endif
        }

        public void TrackBytesReceived(long amount)
        {
#if true
            m_BytesReceived.Increment(amount);
#endif
        }

        public void TrackBytesSent(long amount)
        {
#if true
            m_BytesSent.Increment(amount);
#endif
        }
    }
}