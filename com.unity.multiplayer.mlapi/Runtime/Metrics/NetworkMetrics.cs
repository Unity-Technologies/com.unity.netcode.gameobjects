using Unity.Multiplayer.NetStats.Data;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;

namespace MLAPI.Metrics
{
    public interface INetworkMetrics
    {
        void TrackNamedMessageSent(string messageName, ulong bytesCount);

        void TrackNamedMessageReceived(string messageName, ulong bytesCount);

        void DispatchFrame();
    }

    public class NullNetworkMetrics : INetworkMetrics
    {
        public void TrackNamedMessageSent(string messageName, ulong bytesCount)
        {
        }

        public void TrackNamedMessageReceived(string messageName, ulong bytesCount)
        {
        }

        public void DispatchFrame()
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
            
            m_Dispatcher.RegisterObserver(MLAPIObserver.Observer);
        }

        public void TrackNamedMessageSent(string messageName, ulong bytesCount)
        {
            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), messageName, bytesCount));
        }

        public void TrackNamedMessageReceived(string messageName, ulong bytesCount)
        {
            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), messageName, bytesCount));
        }

        public void DispatchFrame()
        {
            m_Dispatcher.Dispatch();
        }
    }

    public class MLAPIObserver
    {
        public static IMetricObserver Observer { get; }

        static MLAPIObserver()
        {
            Observer = new NetStatObserver();
        }
    }
#endif
}