namespace Unity.Netcode
{
    public class NetworkTypePresets
    {
        const string k_BroadbandDescription = "Typical of desktop and console platforms (and generally speaking most mobile players too).";
        const string k_PoorMobileDescription = "Extremely poor connection, completely unsuitable for synchronous multiplayer gaming due to exceptionally high ping. Turn based games may work.";
        const string k_MediumMobileDescription = "This is the minimum supported mobile connection for synchronous gameplay. Expect high pings, jitter, stuttering and packet loss.";
        const string k_DecentMobileDescription = "Suitable for synchronous multiplayer, except that ping (and overall connection quality and stability) may be quite poor.\n\nExpect to handle players dropping all packets in bursts of 1-60s. I.e. Ensure you handle reconnections.";
        const string k_GoodMobileDescription = "In many places, expect this to be 'as good as' or 'better than' home broadband.";

        public static readonly NetworkTypeConfiguration None = NetworkTypeConfiguration.Create("None", string.Empty, 0, 0, 0, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration HomeBroadband = NetworkTypeConfiguration.Create("Home Broadband [WIFI, Cable, Console, PC]", k_BroadbandDescription, 2, 2, 1, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration Mobile2G = NetworkTypeConfiguration.Create("Mobile 2G [CDMA & GSM, '00]", k_PoorMobileDescription, 400, 200, 5, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration Mobile2_5G = NetworkTypeConfiguration.Create("Mobile 2.5G [GPRS, G, '00]", k_PoorMobileDescription, 200, 100, 5, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration Mobile2_75G = NetworkTypeConfiguration.Create("Mobile 2.75G [Edge, E, '06]", k_PoorMobileDescription, 200, 100, 5, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration Mobile3G = NetworkTypeConfiguration.Create("Mobile 3G [WCDMA & UMTS, '03]", k_PoorMobileDescription, 200, 100, 5, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration Mobile3_5G = NetworkTypeConfiguration.Create("Mobile 3.5G [HSDPA, H, '06]", k_MediumMobileDescription, 75, 50, 5, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration Mobile3_75G = NetworkTypeConfiguration.Create("Mobile 3.75G [HDSDPA+, H+, '11]", k_DecentMobileDescription, 75, 50, 5, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration Mobile4G = NetworkTypeConfiguration.Create("Mobile 4G [4G, LTE, '13]", k_DecentMobileDescription, 50, 25, 3, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration Mobile4_5G = NetworkTypeConfiguration.Create("Mobile 4.5G [4G+, LTE-A, '16]", k_DecentMobileDescription, 50, 25, 3, 0, 0, 0, 0);
        public static readonly NetworkTypeConfiguration Mobile5G = NetworkTypeConfiguration.Create("Mobile 5G ['20]", k_GoodMobileDescription, 1, 10, 1, 0, 0, 0, 0);

        public static NetworkTypeConfiguration[] Values = new[]
        {
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
        };
    }
}
