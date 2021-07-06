using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;

namespace MLAPI.MultiprocessRuntimeTests
{
    /// <summary>
    /// This is needed as Unity throws "An abnormal situation has occurred: the PlayerLoop internal function has been called recursively. Please contact Customer Support with a sample project so that we can reproduce the problem and troubleshoot it."
    /// when trying to build from Setup() steps in tests.
    /// </summary>
    public static class BuildMultiprocessTestPlayer
    {
        public const string MultiprocessBaseMenuName = "MLAPI Multiprocess Test";
        public const string BuildAndExecuteMenuName = MultiprocessBaseMenuName + "/Build - Execute multiprocess tests #%t";
        public const string MainSceneName = "MultiprocessTestingScene";

        public const string BuildInfoFileName = "buildInfo.txt";

        public static string BuildPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds/MultiprocessTestBuild");


#if UNITY_EDITOR
        [MenuItem(MultiprocessBaseMenuName + "/Build Test Player #t")]
        public static void BuildNoDebug()
        {
            var success = Build();
            if (!success)
            {
                throw new Exception("Build failed!");
            }
        }

        [MenuItem(MultiprocessBaseMenuName + "/Build Test Player in debug mode")]
        public static void BuildDebug()
        {
            var success = Build(true);
            if (!success)
            {
                throw new Exception("Build failed!");
            }
        }

        [MenuItem(MultiprocessBaseMenuName + "/Delete Test Build")]
        public static void DeleteBuild()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    var exePath = $"{BuildPath}.exe";
                    if (File.Exists(exePath))
                    {
                        File.Delete(exePath);
                    }
                    else
                    {
                        Debug.Log($"exe {exePath} doesn't exist");
                    }

                    break;
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    var toDelete = BuildPath + ".app";
                    if (Directory.Exists(toDelete))
                    {
                        Directory.Delete(toDelete, recursive: true);
                    }
                    else
                    {
                        Debug.Log($"directory {toDelete} doesn't exist");
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }



        /// <summary>
        /// Needs a separate build than the standalone test builds since we don't want the player to try to connect to the editor to do test
        /// reporting. We only want to main node to do that, worker nodes should be dumb
        /// </summary>
        /// <returns></returns>
        public static bool Build(bool isDebug = false)
        {
            // Save standalone build path to file so we can read it from standalone tests (that are not running from editor)
            SaveBuildInfo(new BuildInfo() { buildPath = BuildPath, isDebug = isDebug });

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

            buildOptions &= ~BuildOptions.AutoRunPlayer;
            var buildReport = BuildPipeline.BuildPlayer(
                new[] { $"Assets/Scenes/{MainSceneName}.unity" },
                buildPathToUse,
                EditorUserBuildSettings.activeBuildTarget,
                buildOptions);

            return buildReport.summary.result == BuildResult.Succeeded;
        }
#endif

        [Serializable]
        public struct BuildInfo
        {
            public string buildPath;
            public bool isDebug;
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
