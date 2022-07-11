using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Unity.Netcode.Editor
{
    /// <summary>
    /// Methods to control whether the Network Simulator is included in the build.
    /// When making automated builds of your project, you can use this to dynamically
    /// control whether the simulator is included in release or development builds.
    /// </summary>
    public class NetworkSimulatorBuildSettings
    {
        internal static IReadOnlyList<NamedBuildTarget> k_AllBuildTargets => new List<NamedBuildTarget>
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.CloudRendering,
            NamedBuildTarget.EmbeddedLinux,
            NamedBuildTarget.iOS,
            NamedBuildTarget.NintendoSwitch,
            NamedBuildTarget.PS4,

            // For some reason BuildTarget.PS5 is defined but NamedBuildTarget.PS5 isn't
            // (as of 2021.2.3f1)
            NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.PS5),

            NamedBuildTarget.Server,
            NamedBuildTarget.Stadia,
            NamedBuildTarget.Standalone,
            NamedBuildTarget.tvOS,
            NamedBuildTarget.WebGL,
            NamedBuildTarget.WindowsStoreApps,
            NamedBuildTarget.XboxOne,
        };

        // NOTE: These top four public, parameterless methods are needed because our CI can only call
        // methods that are public and parameterless (not even optional parameters are allowed).

        /// <summary>
        /// Enables the Network Simulator in development builds for all build targets
        /// </summary>
        public static void EnableInDevelopForAllBuildTargets()
        {
            SetSymbolForAllBuildTargets(NetworkSimulatorBuildSymbol.DisableInDevelop, false);
        }

        /// <summary>
        /// Disables the Network Simulator in development builds for all build targets
        /// </summary>
        public static void DisableInDevelopForAllBuildTargets()
        {
            SetSymbolForAllBuildTargets(NetworkSimulatorBuildSymbol.DisableInDevelop, true);
        }

        /// <summary>
        /// Enables the Network Simulator in release builds for all build targets
        /// </summary>
        public static void EnableInReleaseForAllBuildTargets()
        {
            SetSymbolForAllBuildTargets(NetworkSimulatorBuildSymbol.EnableInRelease, true);
        }

        /// <summary>
        /// Disables the Network Simulator in release builds for all build targets
        /// </summary>
        public static void DisableInReleaseForAllBuildTargets()
        {
            SetSymbolForAllBuildTargets(NetworkSimulatorBuildSymbol.EnableInRelease, false);
        }

        /// Adds the symbol to the build target
        static void AddSymbolToBuildTarget(
            NamedBuildTarget buildTarget,
            NetworkSimulatorBuildSymbol symbol)
        {
            var symbolString = symbol.GetBuildSymbolString();
            PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
            if (!defines.Contains(symbolString))
            {
                PlayerSettings.SetScriptingDefineSymbols(
                    buildTarget,
                    defines.Append(symbolString).ToArray());
            }
        }

        /// Removes the symbol from the build target
        static void RemoveSymbolFromBuildTarget(
            NamedBuildTarget buildTarget,
            NetworkSimulatorBuildSymbol symbol)
        {
            var symbolString = symbol.GetBuildSymbolString();
            PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] symbols);
            if (symbols.Contains(symbolString))
            {
                var symbolsExcludingNetSimEnable = symbols
                    .Where(scriptingSymbol => scriptingSymbol != symbolString)
                    .ToArray();
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, symbolsExcludingNetSimEnable);
            }
        }

        /// Enables or disables the symbol in the build target
        static void SetSymbolForBuildTarget(
            NamedBuildTarget buildTarget,
            NetworkSimulatorBuildSymbol symbol,
            bool enabled)
        {
            if (enabled)
            {
                AddSymbolToBuildTarget(buildTarget, symbol);
            }
            else
            {
                RemoveSymbolFromBuildTarget(buildTarget, symbol);
            }
        }

        /// Returns true if the symbol is defined in the build targets
        static bool GetSymbolInBuildTarget(
            NamedBuildTarget buildTarget,
            NetworkSimulatorBuildSymbol symbol)
        {
            PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
            return defines.Contains(symbol.GetBuildSymbolString());
        }

        /// Adds the symbol to all build targets
        internal static void AddSymbolToAllBuildTargets(NetworkSimulatorBuildSymbol symbol)
        {
            foreach (var buildTarget in k_AllBuildTargets)
            {
                AddSymbolToBuildTarget(buildTarget, symbol);
            }
        }

        /// Removes the symbol from all build targets
        internal static void RemoveSymbolFromAllBuildTargets(NetworkSimulatorBuildSymbol symbol)
        {
            foreach (var buildTarget in k_AllBuildTargets)
            {
                RemoveSymbolFromBuildTarget(buildTarget, symbol);
            }
        }

        /// Enables or disables the symbol in all build targets
        internal static void SetSymbolForAllBuildTargets(
            NetworkSimulatorBuildSymbol symbol,
            bool enabled)
        {
            foreach (var buildTarget in k_AllBuildTargets)
            {
                SetSymbolForBuildTarget(buildTarget, symbol, enabled);
            }
        }

        /// Returns true if the symbol is defined in all build targets
        internal static bool GetSymbolInAllBuildTargets(NetworkSimulatorBuildSymbol symbol)
        {
            return k_AllBuildTargets.All(target => GetSymbolInBuildTarget(target, symbol));
        }

        /// Returns true if the symbol is defined in any build targets
        internal static bool GetEnabledForAnyBuildTargets(NetworkSimulatorBuildSymbol symbol)
        {
            return k_AllBuildTargets.Any(target => GetSymbolInBuildTarget(target, symbol));
        }
    }
}
