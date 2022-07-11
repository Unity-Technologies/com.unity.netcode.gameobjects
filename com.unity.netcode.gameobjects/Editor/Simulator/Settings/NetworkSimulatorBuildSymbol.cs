using System;

namespace Unity.Netcode.Editor
{
    internal enum NetworkSimulatorBuildSymbol
    {
        None,

        /// This is phrased as a negative so that the default state (not defined) matches the
        /// desired default behaviour (inclusion in develop builds)
        DisableInDevelop,

        EnableInRelease,

        /// By adding this scripting define symbol users can override our build logic and
        /// forcibly enable the Network Simulator in both development and release. This option takes
        /// precedence over DisableInDevelop
        OverrideEnabled,
    }

    internal static class NetworkSimulatorBuildSymbolStrings
    {
        public const string k_DisableInDevelop = "UNITY_MP_TOOLS_NETSIM_DISABLED_IN_DEVELOP";
        public const string k_EnableInRelease  = "UNITY_MP_TOOLS_NETSIM_ENABLED_IN_RELEASE";
        public const string k_OverrideEnabled  = "UNITY_MP_TOOLS_NETSIM_ENABLED";
    }

    internal static class NetworkSimulatorBuildSymbolExtensions
    {
        public static string GetBuildSymbolString(this NetworkSimulatorBuildSymbol symbol)
        {
            switch (symbol)
            {
                case NetworkSimulatorBuildSymbol.None:
                    return "";
                case NetworkSimulatorBuildSymbol.DisableInDevelop:
                    return NetworkSimulatorBuildSymbolStrings.k_DisableInDevelop;
                case NetworkSimulatorBuildSymbol.EnableInRelease:
                    return NetworkSimulatorBuildSymbolStrings.k_EnableInRelease;
                case NetworkSimulatorBuildSymbol.OverrideEnabled:
                    return NetworkSimulatorBuildSymbolStrings.k_OverrideEnabled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null);
            }
        }
    }
}
