using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    /// <summary>
    /// This is needed as Unity throws "An abnormal situation has occurred: the PlayerLoop internal function has been called recursively. Please contact Customer Support with a sample project so that we can reproduce the problem and troubleshoot it."
    /// when trying to build from Setup() steps in tests.
    /// </summary>
    public static class BuildMultiprocessTestPlayer
    {
        public const string MultiprocessBaseMenuName = "Netcode/Multiprocess Test";
        public const string BuildAndExecuteMenuName = MultiprocessBaseMenuName + "/Build Test Player #t";
        public const string MainSceneName = "MultiprocessTestScene";

        private static string BuildPathDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "MultiprocessTests");
        public static string BuildPath => Path.Combine(BuildPathDirectory, "MultiprocessTestPlayer");
        public const string BuildInfoFileName = "BuildInfo.json";

#if UNITY_EDITOR
        [MenuItem(BuildAndExecuteMenuName)]
        public static void BuildRelease()
        {
            var report = BuildPlayer();
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed! {report.summary.totalErrors} errors");
            }
        }

        [MenuItem(MultiprocessBaseMenuName + "/Build Test Player (Debug)")]
        public static void BuildDebug()
        {
            var report = BuildPlayer(true);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed! {report.summary.totalErrors} errors");
            }
        }

        [MenuItem(MultiprocessBaseMenuName + "/Delete Test Build")]
        public static void DeleteBuild()
        {
            if (Directory.Exists(BuildPathDirectory))
            {
                Directory.Delete(BuildPathDirectory, recursive: true);
            }
            else
            {
                Debug.Log($"[{nameof(BuildMultiprocessTestPlayer)}] build directory does not exist ({BuildPathDirectory}) not deleting anything");
            }
        }

        /// <summary>
        /// Needs a separate build than the standalone test builds since we don't want the player to try to connect to the editor to do test
        /// reporting. We only want to main node to do that, worker nodes should be dumb
        /// </summary>
        /// <returns></returns>
        private static BuildReport BuildPlayer(bool isDebug = false)
        {
            // Save standalone build path to file so we can read it from standalone tests (that are not running from editor)
            SaveBuildInfo(new BuildInfo() { BuildPath = BuildPath, IsDebug = isDebug });

            // deleting so we don't end up testing on outdated builds if there's a build failure
            DeleteBuild();

            var buildOptions = BuildOptions.None;
            buildOptions |= BuildOptions.IncludeTestAssemblies;
            buildOptions |= BuildOptions.StrictMode;
            if (isDebug)
            {
                buildOptions |= BuildOptions.Development;
                buildOptions |= BuildOptions.AllowDebugging; // enable this if you want to debug your players. Your players

                // will have more connection permission popups when launching though
            }

            var buildPathToUse = BuildPath;
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                buildPathToUse += ".exe";
            }

            Debug.Log($"Starting multiprocess player build using path {buildPathToUse}");

            buildOptions &= ~BuildOptions.AutoRunPlayer;
            var buildReport = BuildPipeline.BuildPlayer(
                new[] { $"Assets/Scenes/{MainSceneName}.unity" },
                buildPathToUse,
                EditorUserBuildSettings.activeBuildTarget,
                buildOptions);

            Debug.Log("Build finished");
            return buildReport;
        }
#endif

        [Serializable]
        public struct BuildInfo
        {
            public string BuildPath;
            public bool IsDebug;
        }

        public static BuildInfo ReadBuildInfo()
        {
            var jsonString = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, BuildInfoFileName));
            return JsonUtility.FromJson<BuildInfo>(jsonString);
        }

        public static void SaveBuildInfo(BuildInfo toSave)
        {
            var buildInfoJson = JsonUtility.ToJson(toSave);
            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, BuildInfoFileName), buildInfoJson);
        }
    }
}
