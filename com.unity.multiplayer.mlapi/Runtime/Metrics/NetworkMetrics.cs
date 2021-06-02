using Unity.Multiplayer.NetStats.Dispatch;
using Unity.Multiplayer.NetStats.Metrics;
using Unity.Multiplayer.NetworkProfiler;
using Unity.Multiplayer.NetworkProfiler.Models;

namespace MLAPI.Metrics
{
    public interface INetworkMetrics
    {
        void TrackNamedMessageSent(string messageName, ulong bytesCount);

        void TrackNamedMessageReceived(string messageName, ulong bytesCount);

        void TrackSceneSwitchInitiated(string lastSceneName, string nextSceneName); // NetworkSceneManager
        void TrackSceneSwitchRequested(string lastSceneName, string nextSceneName); // internalMessageHandler
        void TrackSceneSwitchCompleted(string lastSceneName, string nextSceneName); // called from internalMessageHandler
        void TrackSceneSwitchCompletedAllClients(string lastSceneName, string nextSceneName); // called from internalMessageHandler

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

        public void TrackSceneSwitchInitiated(string lastSceneName, string nextSceneName)
        {
        }

        public void TrackSceneSwitchRequested(string lastSceneName, string nextSceneName)
        {
        }

        public void TrackSceneSwitchCompleted(string lastSceneName, string nextSceneName)
        {
        }

        public void TrackSceneSwitchCompletedAllClients(string lastSceneName, string nextSceneName)
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

        private EventMetric<SceneSwitchEvent> m_SceneSwitchInitiated = new EventMetric<SceneSwitchEvent>("Scene Switch Initiated");
        private EventMetric<SceneSwitchEvent> m_SceneSwitchRequested = new EventMetric<SceneSwitchEvent>("Scene Switch Requested");
        private EventMetric<SceneSwitchEvent> m_SceneSwitchCompleted = new EventMetric<SceneSwitchEvent>("Scene Switch Completed");
        private EventMetric<SceneSwitchEvent> m_SceneSwitchCompletedAllClients = new EventMetric<SceneSwitchEvent>("Scene Switch Completed for All Clients");

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
            m_NamedMessageSentEvent.Mark(new NamedMessageEvent(
                    new ConnectionInfo(m_NetworkManager.LocalClientId),
                    messageName,
                    bytesCount));
        }

        public void TrackNamedMessageReceived(string messageName, ulong bytesCount)
        {
            m_NamedMessageReceivedEvent.Mark(new NamedMessageEvent(
                new ConnectionInfo(m_NetworkManager.LocalClientId),
                messageName,
                bytesCount));
        }

        public void TrackSceneSwitchInitiated(string lastSceneName, string nextSceneName)
        {
            m_SceneSwitchInitiated.Mark(new SceneSwitchEvent(
                new ConnectionInfo(m_NetworkManager.LocalClientId),
                lastSceneName,
                nextSceneName));
        }

        public void TrackSceneSwitchRequested(string lastSceneName, string nextSceneName)
        {
            m_SceneSwitchRequested.Mark(new SceneSwitchEvent(
                new ConnectionInfo(m_NetworkManager.LocalClientId),
                lastSceneName,
                nextSceneName));
        }

        public void TrackSceneSwitchCompleted(string lastSceneName, string nextSceneName)
        {
            m_SceneSwitchCompleted.Mark(new SceneSwitchEvent(
                new ConnectionInfo(m_NetworkManager.LocalClientId),
                lastSceneName,
                nextSceneName));
        }

        public void TrackSceneSwitchCompletedAllClients(string lastSceneName, string nextSceneName)
        {
            m_SceneSwitchCompletedAllClients.Mark(new SceneSwitchEvent(
                new ConnectionInfo(m_NetworkManager.LocalClientId),
                lastSceneName,
                nextSceneName));
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