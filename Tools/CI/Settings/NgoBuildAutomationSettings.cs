using System.Collections.Generic;
using RecipeEngine.Modules.BuildAutomation.Models;
using RecipeEngine.Modules.BuildAutomation.Settings;

namespace NGO.Cookbook.Settings
{
    public class NgoBuildAutomationSettings
    {
        public NgoBuildAutomationSettings()
        {
            BuildAutomation = new BuildAutomationSettings(
                projects: new Dictionary<string, BuildProject>
                {
                    ["BossRoom"] = new(
                        GithubRepo: "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop.git",
                        ProjectPath: ".",
                        DefaultSampleBranch: "ngo-playtest-update",
                        MinSupportedEditorBySample: "6000.7",
                        LocalTestedPackagesNames: new[] { "com.unity.netcode.gameobjects" },
                        LocalTestedPackagesPath: "."),
                    ["Asteroids"] = new(
                        GithubRepo: "https://github.cds.internal.unity3d.com/unity/Asteroids-CMB-NGO-Sample.git",
                        ProjectPath: ".",
                        DefaultSampleBranch: "main",
                        MinSupportedEditorBySample: "6000.7",
                        LocalTestedPackagesNames: new[] { "com.unity.netcode.gameobjects" },
                        LocalTestedPackagesPath: "."),
                    ["SocialHub"] = new(
                        GithubRepo: "https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize.git",
                        ProjectPath: "Basic/DistributedAuthoritySocialHub",
                        DefaultSampleBranch: "main",
                        MinSupportedEditorBySample: "6000.7",
                        LocalTestedPackagesNames: new[] { "com.unity.netcode.gameobjects" },
                        LocalTestedPackagesPath: "."),
                });
        }

        public BuildAutomationSettings BuildAutomation { get; }
    }
}
