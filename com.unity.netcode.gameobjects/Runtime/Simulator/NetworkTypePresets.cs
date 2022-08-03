// NetSim Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE))
    #define UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

namespace Unity.Netcode
{
    public class NetworkTypePresets
    {
        const string k_NoNetworkDescription = "Effectively no network. Offline simulation.";
        const string k_BroadbandDescription = "Typical of desktop and console platforms (and generally speaking most mobile players too).";
        const string k_PoorMobileDescription = "Extremely poor connection, completely unsuitable for synchronous multiplayer gaming due to exceptionally high ping. Turn based games may work.";
        const string k_MediumMobileDescription = "This is the minimum supported mobile connection for synchronous gameplay. Expect high pings, jitter, stuttering and packet loss.";
        const string k_DecentMobileDescription = "Suitable for synchronous multiplayer, except that ping (and overall connection quality and stability) may be quite poor.\n\nExpect to handle players dropping all packets in bursts of 1-60s. I.e. Ensure you handle reconnections.";
        const string k_GoodMobileDescription = "In many places, expect this to be 'as good as' or 'better than' home broadband.";

        public static readonly NetworkSimulatorConfiguration None
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("None", string.Empty, 0, 0, 0, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration HomeBroadband
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Home Broadband [WIFI, Cable, Console, PC]", k_BroadbandDescription, 2, 2, 1, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration Mobile2G
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Mobile 2G [CDMA & GSM, '00]", k_PoorMobileDescription, 400, 200, 5, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration Mobile2_5G
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Mobile 2.5G [GPRS, G, '00]", k_PoorMobileDescription, 200, 100, 5, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration Mobile2_75G
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Mobile 2.75G [Edge, E, '06]", k_PoorMobileDescription, 200, 100, 5, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration Mobile3G
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Mobile 3G [WCDMA & UMTS, '03]", k_PoorMobileDescription, 200, 100, 5, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration Mobile3_5G
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Mobile 3.5G [HSDPA, H, '06]", k_MediumMobileDescription, 75, 50, 5, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration Mobile3_75G
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Mobile 3.75G [HDSDPA+, H+, '11]", k_DecentMobileDescription, 75, 50, 5, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration Mobile4G
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Mobile 4G [4G, LTE, '13]", k_DecentMobileDescription, 50, 25, 3, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration Mobile4_5G
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Mobile 4.5G [4G+, LTE-A, '16]", k_DecentMobileDescription, 50, 25, 3, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration Mobile5G
#if UNITY_MP_TOOLS_NETSIM_IMPLEMENTATION_ENABLED
            = NetworkSimulatorConfiguration.Create("Mobile 5G ['20]", k_GoodMobileDescription, 1, 10, 1, 0, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration TotalPacketLoss
#if UNITY_MP_TOOLS_NETSIM_ENABLED
            = NetworkSimulatorConfiguration.Create("100% Packet Loss", k_NoNetworkDescription, 0, 0, 0, 100, 0);
#else
            = null;
#endif
        public static readonly NetworkSimulatorConfiguration DSL
#if UNITY_MP_TOOLS_NETSIM_ENABLED
            = NetworkSimulatorConfiguration.Create("DSL", k_BroadbandDescription, 5, 5, 1, 0, 0);
#else
            = null;
#endif

        public static NetworkSimulatorConfiguration[] Values = {
            None,
            HomeBroadband,
            Mobile2G,
            Mobile2_5G,
            Mobile2_75G,
            Mobile3G,
            Mobile3_5G,
            Mobile3_75G,
            Mobile4G,
            Mobile4_5G,
            Mobile5G,
            TotalPacketLoss,
            DSL
        };
    }
}
