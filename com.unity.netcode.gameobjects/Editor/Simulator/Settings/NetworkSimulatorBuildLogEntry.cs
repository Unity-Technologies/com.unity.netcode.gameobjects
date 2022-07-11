using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    public class NetworkSimulatorBuildLogEntry : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1;

        public void OnPostprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;

            var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out var symbols);

            var isDevelopBuild = (report.summary.options & BuildOptions.Development) != 0;
            var isReleaseBuild = !isDevelopBuild;

            var enabledInDevelop = !symbols.Contains(NetworkSimulatorBuildSymbolStrings.k_DisableInDevelop);
            var enabledInRelease =  symbols.Contains(NetworkSimulatorBuildSymbolStrings.k_EnableInRelease);
            var overrideEnabled  =  symbols.Contains(NetworkSimulatorBuildSymbolStrings.k_OverrideEnabled);

            // This logic needs to match the preprocessor logic for the inclusion of the NetSim
            // implementation in the NetSim implementation source files
            var netSimImplementationEnabled = overrideEnabled ||
                (isDevelopBuild && enabledInDevelop) ||
                (isReleaseBuild && enabledInRelease);

            var buildType = isDevelopBuild ? "development" : "release";
            var enabled = netSimImplementationEnabled ? "enabled" : "disabled";

            Debug.Log($"Network Simulator implementation {enabled} in {buildType} build targeting {target}");
        }
    }
}
