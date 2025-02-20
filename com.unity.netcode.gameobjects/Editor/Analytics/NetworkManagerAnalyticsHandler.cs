#if UNITY_EDITOR
using UnityEngine.Analytics;

namespace Unity.Netcode.Editor
{
    [AnalyticInfo("NGO_NetworkManager", "unity.netcode", 3, 100, 1000)]
    internal class NetworkManagerAnalyticsHandler : AnalyticsHandler<NetworkManagerAnalytics>
    {
        public NetworkManagerAnalyticsHandler(NetworkManagerAnalytics networkManagerAnalytics) : base(networkManagerAnalytics) { }
    }
}
#endif
