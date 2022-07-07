// NetSim Implementation compilation boilerplate
// All references to UNITY_MP_TOOLS_NETSIM_ENABLED should be defined in the same way,
// as any discrepancies are likely to result in build failures
// ---------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD && !UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP) || (!DEVELOPMENT_BUILD && UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE))
    #define UNITY_MP_TOOLS_NETSIM_ENABLED
#endif
// ---------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Unity.Netcode
{
    public class NetworkTypePresets
    {
        const string k_ScriptableObjectsPath = "Packages/com.unity.netcode.gameobjects/Runtime/Simulator/Presets/";
        const string k_BroadbandDescription = "Typical of desktop and console platforms (and generally speaking most mobile players too).";
        const string k_PoorMobileDescription = "Extremely poor connection, completely unsuitable for synchronous multiplayer gaming due to exceptionally high ping. Turn based games may work.";
        const string k_MediumMobileDescription = "This is the minimum supported mobile connection for synchronous gameplay. Expect high pings, jitter, stuttering and packet loss.";
        const string k_DecentMobileDescription = "Suitable for synchronous multiplayer, except that ping (and overall connection quality and stability) may be quite poor.\n\nExpect to handle players dropping all packets in bursts of 1-60s. I.e. Ensure you handle reconnections.";
        const string k_GoodMobileDescription = "In many places, expect this to be 'as good as' or 'better than' home broadband.";

#if UNITY_MP_TOOLS_NETSIM_ENABLED
        public static readonly NetworkSimulatorConfigurationObject None = NetworkSimulatorConfigurationObject.Create("None", string.Empty, 0, 0, 0, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject HomeBroadband = NetworkSimulatorConfigurationObject.Create("Home Broadband [WIFI, Cable, Console, PC]", k_BroadbandDescription, 2, 2, 1, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject Mobile2G = NetworkSimulatorConfigurationObject.Create("Mobile 2G [CDMA & GSM, '00]", k_PoorMobileDescription, 400, 200, 5, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject Mobile2_5G = NetworkSimulatorConfigurationObject.Create("Mobile 2.5G [GPRS, G, '00]", k_PoorMobileDescription, 200, 100, 5, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject Mobile2_75G = NetworkSimulatorConfigurationObject.Create("Mobile 2.75G [Edge, E, '06]", k_PoorMobileDescription, 200, 100, 5, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject Mobile3G = NetworkSimulatorConfigurationObject.Create("Mobile 3G [WCDMA & UMTS, '03]", k_PoorMobileDescription, 200, 100, 5, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject Mobile3_5G = NetworkSimulatorConfigurationObject.Create("Mobile 3.5G [HSDPA, H, '06]", k_MediumMobileDescription, 75, 50, 5, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject Mobile3_75G = NetworkSimulatorConfigurationObject.Create("Mobile 3.75G [HDSDPA+, H+, '11]", k_DecentMobileDescription, 75, 50, 5, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject Mobile4G = NetworkSimulatorConfigurationObject.Create("Mobile 4G [4G, LTE, '13]", k_DecentMobileDescription, 50, 25, 3, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject Mobile4_5G = NetworkSimulatorConfigurationObject.Create("Mobile 4.5G [4G+, LTE-A, '16]", k_DecentMobileDescription, 50, 25, 3, 0, 0);
        public static readonly NetworkSimulatorConfigurationObject Mobile5G = NetworkSimulatorConfigurationObject.Create("Mobile 5G ['20]", k_GoodMobileDescription, 1, 10, 1, 0, 0);
#else
        public static readonly NetworkSimulatorConfiguration None = NetworkSimulatorConfiguration.Create("None", string.Empty, 0, 0, 0, 0, 0);
        public static readonly NetworkSimulatorConfiguration HomeBroadband = NetworkSimulatorConfiguration.Create("Home Broadband [WIFI, Cable, Console, PC]", k_BroadbandDescription, 2, 2, 1, 0, 0);
        public static readonly NetworkSimulatorConfiguration Mobile2G = NetworkSimulatorConfiguration.Create("Mobile 2G [CDMA & GSM, '00]", k_PoorMobileDescription, 400, 200, 5, 0, 0);
        public static readonly NetworkSimulatorConfiguration Mobile2_5G = NetworkSimulatorConfiguration.Create("Mobile 2.5G [GPRS, G, '00]", k_PoorMobileDescription, 200, 100, 5, 0, 0);
        public static readonly NetworkSimulatorConfiguration Mobile2_75G = NetworkSimulatorConfiguration.Create("Mobile 2.75G [Edge, E, '06]", k_PoorMobileDescription, 200, 100, 5, 0, 0);
        public static readonly NetworkSimulatorConfiguration Mobile3G = NetworkSimulatorConfiguration.Create("Mobile 3G [WCDMA & UMTS, '03]", k_PoorMobileDescription, 200, 100, 5, 0, 0);
        public static readonly NetworkSimulatorConfiguration Mobile3_5G = NetworkSimulatorConfiguration.Create("Mobile 3.5G [HSDPA, H, '06]", k_MediumMobileDescription, 75, 50, 5, 0, 0);
        public static readonly NetworkSimulatorConfiguration Mobile3_75G = NetworkSimulatorConfiguration.Create("Mobile 3.75G [HDSDPA+, H+, '11]", k_DecentMobileDescription, 75, 50, 5, 0, 0);
        public static readonly NetworkSimulatorConfiguration Mobile4G = NetworkSimulatorConfiguration.Create("Mobile 4G [4G, LTE, '13]", k_DecentMobileDescription, 50, 25, 3, 0, 0);
        public static readonly NetworkSimulatorConfiguration Mobile4_5G = NetworkSimulatorConfiguration.Create("Mobile 4.5G [4G+, LTE-A, '16]", k_DecentMobileDescription, 50, 25, 3, 0, 0);
        public static readonly NetworkSimulatorConfiguration Mobile5G = NetworkSimulatorConfiguration.Create("Mobile 5G ['20]", k_GoodMobileDescription, 1, 10, 1, 0, 0);
#endif
        
        public static readonly INetworkSimulatorConfiguration[] Values = {
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

#if UNITY_EDITOR
        static NetworkTypePresets()
        {
            foreach (var configuration in Values)
            {
                var path = $"{k_ScriptableObjectsPath}/{configuration.Name}.asset";
                var assetDoesntExists = string.IsNullOrEmpty(UnityEditor.AssetDatabase.AssetPathToGUID(path));

                if (Directory.Exists(k_ScriptableObjectsPath) == false)
                {
                    Directory.CreateDirectory(k_ScriptableObjectsPath);
                }
                
                if (configuration is NetworkSimulatorConfigurationObject scriptableObject && assetDoesntExists)
                {
                    UnityEditor.AssetDatabase.CreateAsset(scriptableObject, path);
                }
            }
        }
#endif
    }
}
