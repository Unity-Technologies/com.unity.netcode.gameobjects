using System.Collections.Generic;
using MLAPI.Connection;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;

namespace MLAPI.Metrics
{
    public interface INetworkMetrics
    {
        void TrackNetworkObject(NetworkObject networkObject);

        void TrackNamedMessageSent(ulong clientId, string messageName, ulong bytesCount);

        void TrackNamedMessageSent(List<ulong> clientIds, string messageName, ulong bytesCount);

        void TrackNamedMessageReceived(ulong clientId, string messageName, ulong bytesCount);

        void DispatchFrame();
    }

    public class NullNetworkMetrics : INetworkMetrics
    {
        public void TrackNetworkObject(NetworkObject networkObject)
        {
        }

        public void TrackNamedMessageSent(ulong clientId, string messageName, ulong bytesCount)
        {
        }

        public void TrackNamedMessageSent(List<ulong> clientIds, string messageName, ulong bytesCount)
        {
        }

        public void TrackNamedMessageReceived(ulong clientId, string messageName, ulong bytesCount)
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

        private Dictionary<ulong, NetworkObjectIdentifier> m_NetworkGameObjects = new Dictionary<ulong, NetworkObjectIdentifier>();

        public NetworkMetrics(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_Dispatcher = new MetricDispatcherBuilder()
                .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
                .Build();

            m_Dispatcher.RegisterObserver(MLAPIObserver.Observer);
        }

        public void TrackNetworkObject(NetworkObject networkObject)
        {
            if (!m_NetworkGameObjects.ContainsKey(networkObject.NetworkObjectId))
            {
                m_NetworkGameObjects[networkObject.NetworkObjectId] = new NetworkObjectIdentifier(networkObject.name, networkObject.NetworkObjectId);
            }
        }

        public void TrackNamedMessageSent(ulong clientId, string messageName, ulong bytesCount)
        {
            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(new ConnectionInfo(clientId), messageName, bytesCount));
        }

        public void TrackNamedMessageSent(List<ulong> clientIds, string messageName, ulong bytesCount)
        {
            foreach (var clientId in clientIds)
            {
                TrackNamedMessageSent(clientId, messageName, bytesCount);
            }
        }

        public void TrackNamedMessageReceived(ulong clientId, string messageName, ulong bytesCount)
        {
            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(new ConnectionInfo(clientId), messageName, bytesCount));
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
