using System;
using System.Linq.Expressions;
using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;
using UnityEngine;

namespace MLAPI.Metrics
{
    public interface INetworkMetrics
    {
        void TrackNamedMessageSent(string messageName, ulong bytesCount);

        void TrackNamedMessageReceived(string messageName, ulong bytesCount);

        void TrackUnnamedMessageSent(ulong bytesCount);

        void TrackUnnamedMessageReceived(ulong bytesCount);

        void TrackNetworkVariableDelta(ulong networkObjectId, string info, ulong bytesCount);

        void TrackNetworkVariableUpdate(ulong networkObjectId, string info, ulong bytesCount);

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

        public void TrackUnnamedMessageSent(ulong bytesCount)
        {
        }

        public void TrackUnnamedMessageReceived(ulong bytesCount)
        {
        }

        public void TrackNetworkVariableDelta(ulong networkObjectId, string info, ulong bytesCount)
        {
        }

        public void TrackNetworkVariableUpdate(ulong networkObjectId, string info, ulong bytesCount)
        {
        }

        public void DispatchFrame()
        {
        }
    }
    
#if true
    public class NetworkMetrics : INetworkMetrics
    {
        readonly NetworkManager m_NetworkManager;

        readonly EventMetric<NamedMessageEvent> m_NamedMessageSentEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageSent);
        readonly EventMetric<NamedMessageEvent> m_NamedMessageReceivedEvent = new EventMetric<NamedMessageEvent>(MetricNames.NamedMessageReceived);

        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageSentEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageSent);
        readonly EventMetric<UnnamedMessageEvent> m_UnnamedMessageReceivedEvent = new EventMetric<UnnamedMessageEvent>(MetricNames.UnnamedMessageReceived);

        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableDeltaEvent = new EventMetric<NetworkVariableEvent>(MetricNames.NetworkVariableDelta);
        readonly EventMetric<NetworkVariableEvent> m_NetworkVariableUpdateEvent = new EventMetric<NetworkVariableEvent>(MetricNames.NetworkVariableUpdate);

        public NetworkMetrics(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            Dispatcher = new MetricDispatcherBuilder()
                .WithMetricEvents(m_NamedMessageSentEvent, m_NamedMessageReceivedEvent)
                .WithMetricEvents(m_UnnamedMessageSentEvent, m_UnnamedMessageReceivedEvent)
                .WithMetricEvents(m_NetworkVariableDeltaEvent, m_NetworkVariableUpdateEvent)
                .Build();
            
            Dispatcher.RegisterObserver(MLAPIObserver.Observer);
        }

        internal IMetricDispatcher Dispatcher { get; }

        public void TrackNamedMessageSent(string messageName, ulong bytesCount)
        {
            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), messageName, bytesCount));
        }

        public void TrackNamedMessageReceived(string messageName, ulong bytesCount)
        {
            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), messageName, bytesCount));
        }

        public void TrackUnnamedMessageSent(ulong bytesCount)
        {
            m_UnnamedMessageSentEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), bytesCount));
        }

        public void TrackUnnamedMessageReceived(ulong bytesCount)
        {
            m_UnnamedMessageReceivedEvent.Mark(new UnnamedMessageEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), bytesCount));
        }

        public void TrackNetworkVariableDelta(ulong networkObjectId, string info, ulong bytesCount)
        {
            m_NetworkVariableDeltaEvent.Mark(new NetworkVariableEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), new NetworkObjectIdentifier("", networkObjectId), info, bytesCount));
        }

        public void TrackNetworkVariableUpdate(ulong networkObjectId, string info, ulong bytesCount)
        {
            m_NetworkVariableUpdateEvent.Mark(new NetworkVariableEvent(new ConnectionInfo(m_NetworkManager.LocalClientId), new NetworkObjectIdentifier("", networkObjectId), info, bytesCount));
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
