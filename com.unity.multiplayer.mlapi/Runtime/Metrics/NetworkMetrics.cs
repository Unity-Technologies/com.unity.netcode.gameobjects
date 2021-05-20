using Unity.Multiplayer.NetStats.Data;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;

namespace MLAPI.Metrics
{
    public interface INetworkMetrics
    {
        void TrackNamedMessageSent(string messageName, ulong bytesCount);

        void TrackNamedMessageReceived(string messageName, ulong bytesCount);
    }

    public class NullNetworkMetrics : INetworkMetrics
    {
        public void TrackNamedMessageSent(string messageName, ulong bytesCount)
        {
        }

        public void TrackNamedMessageReceived(string messageName, ulong bytesCount)
        {
        }
    }
    
#if true
    public class NetworkMetrics : INetworkMetrics
    {
        private readonly IMetricDispatcher m_Dispatcher;
        private readonly NetworkManager m_NetworkManager;

        private EventMetric<NamedMessageEvent> m_NamedMessageSentEvent = new EventMetric<NamedMessageEvent>("Named Message Sent");
        private EventMetric<NamedMessageEvent> m_NamedMessageReceivedEvent = new EventMetric<NamedMessageEvent>("Named Message Received");

        public NetworkMetrics(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_Dispatcher = new MetricDispatcherBuilder()
                .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
                .Build();
        }

        public void TrackNamedMessageSent(string messageName, ulong bytesCount)
        {
            m_NamedMessageSentEvent.Mark(new NamedMessageEvent
            {
                Name = messageName,
                BytesCount = bytesCount,
                Connection = new ConnectionInfo
                {
                    Id = m_NetworkManager.LocalClientId,
                }
            });
        }

        public void TrackNamedMessageReceived(string messageName, ulong bytesCount)
        {
            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent
            {
                Name = messageName,
                BytesCount = bytesCount,
                Connection = new ConnectionInfo
                {
                    Id = m_NetworkManager.LocalClientId,
                }
            });
        }
    }
#endif
}