using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public static class BuildMultiNodePlayer
    {
        private static string BuildPathDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "MultiprocessTests");
        public static string BuildPath => Path.Combine(BuildPathDirectory, "MultiNodeTestPlayer");
#if UNITY_EDITOR
        [MenuItem("Netcode/Multinode Standalone/Windows")]
        public static void BuildRelease()
        {
            var buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/RemoteConfigScene.unity", "Assets/Scenes/MultiprocessTestScene.unity" };
            buildPlayerOptions.locationPathName = BuildPath + ".exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            var buildOptions = BuildOptions.None;
            buildOptions |= BuildOptions.IncludeTestAssemblies;
            buildOptions |= BuildOptions.StrictMode;
            buildPlayerOptions.options = buildOptions;
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
#endif
    }

}
