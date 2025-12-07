#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// Used to collection network session configuration information
    /// </summary>
    internal struct NetworkSessionInfo
    {
        public int SessionIndex;
        public bool SessionStopped;
        public bool WasServer;
        public bool WasClient;
        public bool UsedCMBService;
        public string Transport;
        public NetworkConfig NetworkConfig;
    }

    /// <summary>
    /// Netcode for GameObjects Analytics Class
    /// </summary>
    internal class NetcodeAnalytics : NetworkManager.NetcodeAnalytics
    {
        /// <summary>
        /// Determines if we are running an integration test of the analytics integration
        /// </summary>
        internal static bool IsIntegrationTest = false;
        internal static bool EnableIntegrationTestAnalytics = false;
#if ENABLE_NGO_ANALYTICS_LOGGING
        internal static bool EnableLogging = true;
#else
        internal static bool EnableLogging = false;
#endif

        internal override void OnOneTimeSetup()
        {
            IsIntegrationTest = true;
        }

        internal override void OnOneTimeTearDown()
        {
            IsIntegrationTest = false;
        }

        internal List<NetworkManagerAnalyticsHandler> AnalyticsTestResults = new List<NetworkManagerAnalyticsHandler>();

        internal List<NetworkSessionInfo> RecentSessions = new List<NetworkSessionInfo>();
        /// <summary>
        /// Invoked from <see cref="NetworkManager.ModeChanged(PlayModeStateChange)"/>.
        /// </summary>
        /// <param name="playModeState">The new <see cref="PlayModeStateChange"/> state.</param>
        /// <param name="networkManager">The current <see cref="NetworkManager"/> instance when play mode was entered.</param>
        internal override void ModeChanged(PlayModeStateChange playModeState, NetworkManager networkManager)
        {
            switch (playModeState)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    {
                        if (IsIntegrationTest)
                        {
                            AnalyticsTestResults.Clear();
                        }
                        break;
                    }
                case PlayModeStateChange.ExitingPlayMode:
                    {
                        // Update analytics
                        UpdateAnalytics(networkManager);
                        break;
                    }
            }
        }

        private bool ShouldLogAnalytics()
        {
            return (IsIntegrationTest && EnableIntegrationTestAnalytics) || (!IsIntegrationTest && EditorAnalytics.enabled);
        }

        /// <summary>
        /// Editor Only
        /// Invoked when the session is started.
        /// </summary>
        /// <param name="networkManager">The <see cref="NetworkManager"/> instance when the session is started.</param>
        internal override void SessionStarted(NetworkManager networkManager)
        {
            if (!ShouldLogAnalytics())
            {
                return;
            }

            var newSession = new NetworkSessionInfo()
            {
                SessionIndex = RecentSessions.Count,
                WasClient = networkManager.IsClient,
                WasServer = networkManager.IsServer,
                NetworkConfig = networkManager.NetworkConfig.Copy(),
                Transport = networkManager.NetworkConfig.NetworkTransport != null ? networkManager.NetworkConfig.NetworkTransport.GetType().Name : "None",
            };
            RecentSessions.Add(newSession);
        }

        /// <summary>
        /// Editor Only
        /// Invoked when the session is stopped or upon exiting play mode.
        /// </summary>
        /// <param name="networkManager">The <see cref="NetworkManager"/> instance.</param>
        internal override void SessionStopped(NetworkManager networkManager)
        {
            // If analytics is disabled and we are not running an integration test or there are no sessions, then exit early.
            if (!ShouldLogAnalytics() || RecentSessions.Count == 0)
            {
                return;
            }

            var lastIndex = RecentSessions.Count - 1;
            var recentSession = RecentSessions[lastIndex];
            // If the session has already been finalized, then exit early.
            if (recentSession.SessionStopped)
            {
                return;
            }
            recentSession.UsedCMBService = networkManager.CMBServiceConnection;
            recentSession.SessionStopped = true;
            RecentSessions[lastIndex] = recentSession;
        }

        /// <summary>
        /// Invoked from within <see cref="NetworkManager.ModeChanged"/> when exiting play mode.
        /// </summary>
        private void UpdateAnalytics(NetworkManager networkManager)
        {
            // If analytics is disabled and we are not running an integration test or there are no sessions to process, then exit early.
            if (!ShouldLogAnalytics() || RecentSessions.Count == 0)
            {
                return;
            }

            // If the NetworkManager isn't null, then make sure the last entry is marked off as stopped.
            // If the last session is stopped, then SessionStopped will exit early.
            if (networkManager != null)
            {
                SessionStopped(networkManager);
            }

            // Parse through all of the recent network sessions to generate and send NetworkManager analytics
            for (int i = 0; i < RecentSessions.Count; i++)
            {
                var networkManagerAnalytics = GetNetworkManagerAnalytics(RecentSessions[i]);

                var isDuplicate = false;
                foreach (var analytics in AnalyticsTestResults)
                {
                    // If we have any sessions with identical configurations,
                    // then we want to ignore those.
                    if (analytics.Data.Equals(networkManagerAnalytics))
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (isDuplicate)
                {
                    continue;
                }

                // If not running an integration test, then go ahead and send the anlytics event data.
                if (!IsIntegrationTest)
                {
                    var result = EditorAnalytics.SendAnalytic(new NetworkManagerAnalyticsHandler(networkManagerAnalytics));
                    if (EnableLogging && result != AnalyticsResult.Ok)
                    {
                        Debug.LogWarning($"[Analytics] Problem sending analytics: {result}");
                    }
                }
                else
                {
                    AnalyticsTestResults.Add(new NetworkManagerAnalyticsHandler(networkManagerAnalytics));
                }
            }

            if (IsIntegrationTest && EnableLogging)
            {
                var count = 0;
                foreach (var entry in AnalyticsTestResults)
                {
                    entry.Data.LogAnalyticData(count);
                    count++;
                }
            }
            RecentSessions.Clear();
        }

        /// <summary>
        /// Generates a <see cref="NetworkManagerAnalytics"/> based on the <see cref="NetworkManager.NetworkSessionInfo"/> passed in
        /// </summary>
        /// <param name="networkSession">Represents a network session with the used NetworkManager configuration</param>
        /// <returns></returns>
        private NetworkManagerAnalytics GetNetworkManagerAnalytics(NetworkSessionInfo networkSession)
        {
            var multiplayerSDKInstalled = false;
#if MULTIPLAYER_SERVICES_SDK_INSTALLED
            multiplayerSDKInstalled = true;
#endif
            if (EnableLogging && !networkSession.SessionStopped)
            {
                Debug.LogWarning($"Session-{networkSession.SessionIndex} was not considered stopped!");
            }
            var networkManagerAnalytics = new NetworkManagerAnalytics()
            {
                IsDistributedAuthority = networkSession.NetworkConfig.NetworkTopology == NetworkTopologyTypes.DistributedAuthority,
                WasServer = networkSession.WasServer,
                WasClient = networkSession.WasClient,
                UsedCMBService = networkSession.UsedCMBService,
                IsUsingMultiplayerSDK = multiplayerSDKInstalled,
                NetworkTransport = networkSession.Transport,
                EnableSceneManagement = networkSession.NetworkConfig.EnableSceneManagement,
                TickRate = (int)networkSession.NetworkConfig.TickRate,
            };
            return networkManagerAnalytics;
        }
    }
}
#endif
