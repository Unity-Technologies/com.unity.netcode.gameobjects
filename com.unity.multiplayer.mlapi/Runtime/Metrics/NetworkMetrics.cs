using System.Collections.Generic;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;

namespace MLAPI.Metrics
{
    public interface INetworkMetrics
    {
        void TrackNetworkObject(NetworkObject networkObject);

        void TrackNamedMessageSent(ulong receiverClientId, string messageName, ulong bytesCount);

        void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, ulong bytesCount);

        void TrackNamedMessageReceived(ulong senderClientId, string messageName, ulong bytesCount);

        void TrackUnnamedMessageSent(ulong bytesCount);

        void TrackUnnamedMessageReceived(ulong bytesCount);

        void DispatchFrame();
    }

    public class NullNetworkMetrics : INetworkMetrics
    {
        public void TrackNetworkObject(NetworkObject networkObject)
        {
        }

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, ulong bytesCount)
        {
        }

        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, ulong bytesCount)
        {
        }

        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, ulong bytesCount)
        {
        }

        public void TrackUnnamedMessageSent(ulong bytesCount)
        {
        }

        public void TrackUnnamedMessageReceived(ulong bytesCount)
        {
        }

        public void DispatchFrame()
        {
        }
    }

#if true
    public class NetworkMetrics : INetworkMetrics
    {
        private readonly NetworkManager m_NetworkManager;

        readonly EventMetric<NamedMessageEvent> m_NamedMessageSentEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageSent);
        readonly EventMetric<NamedMessageEvent> m_NamedMessageReceivedEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageReceived);

        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageSentEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageSent);
        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageReceivedEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageReceived);

        private Dictionary<ulong, NetworkObjectIdentifier> m_NetworkGameObjects = new Dictionary<ulong, NetworkObjectIdentifier>();

        public NetworkMetrics(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            Dispatcher = new MetricDispatcherBuilder()
                .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
                .WithMetricEvents(m_UnnamedMessageSentEvent, m_UnnamedMessageReceivedEvent)
                .Build();
            
            Dispatcher.RegisterObserver(MLAPIObserver.Observer);
        }

        internal IMetricDispatcher Dispatcher { get; }

        public void TrackNetworkObject(NetworkObject networkObject)
        {
            if (!m_NetworkGameObjects.ContainsKey(networkObject.NetworkObjectId))
            {
                m_NetworkGameObjects[networkObject.NetworkObjectId] = new NetworkObjectIdentifier(networkObject.name, networkObject.NetworkObjectId);
            }
        }

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, ulong bytesCount)
        {
            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(new ConnectionInfo(receiverClientId), messageName, bytesCount));
        }

        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, ulong bytesCount)
        {
            foreach (var receiver in receiverClientIds)
            {
                TrackNamedMessageSent(receiver, messageName, bytesCount);
            }
        }

        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, ulong bytesCount)
        {
            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(new ConnectionInfo(senderClientId), messageName, bytesCount));
        }

        public void TrackUnnamedMessageSent(ulong bytesCount)
        {
            m_UnnamedMessageSentEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), bytesCount));
        }

        public void TrackUnnamedMessageReceived(ulong bytesCount)
        {
            m_UnnamedMessageReceivedEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), bytesCount));
        }

        public void DispatchFrame()
        {
            Dispatcher.Dispatch();
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
