using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;

/// <summary>
/// This is needed as Unity throws "An abnormal situation has occurred: the PlayerLoop internal function has been called recursively. Please contact Customer Support with a sample project so that we can reproduce the problem and troubleshoot it."
/// when trying to build from Setup() steps in tests.
/// </summary>
public class BuildMultiprocessTestPlayer : MonoBehaviour
{
    public const string multiprocessBaseMenuName = "MLAPI Multiprocess Test";
    public const string BuildAndExecuteMenuName = multiprocessBaseMenuName + "/Build - Execute multiprocess tests #%t";
    public static string buildPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds/MultiprocessTestBuild");
    public const string mainSceneName = "MultiprocessTestingScene";


#if UNITY_EDITOR
    [MenuItem(multiprocessBaseMenuName+"/Build Test Player #t")]
    public static void BuildNoExecute()
    {
        var success = Build();
        if (!success)
        {
            throw new Exception("Build failed!");
        }
    }

    [MenuItem(multiprocessBaseMenuName+"/Build Test Player in debug mode")]
    public static void BuildDebug()
    {
        var success = Build(true);
        if (!success)
        {
            throw new Exception("Build failed!");
        }
    }

    [MenuItem(multiprocessBaseMenuName+"/Delete Test Build")]
    public static void DeleteBuild()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                var exePath = $"{buildPath}.exe";
                if (File.Exists(exePath))
                {
                    File.Delete(exePath);
                }
                else
                {
                    Debug.Log($"exe {exePath} doesn't exists");
                }
                break;
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                var toDelete = buildPath + ".app";
                if (Directory.Exists(toDelete))
                {
                    Directory.Delete(toDelete, recursive: true);
                }
                else
                {
                    Debug.Log($"directory {toDelete} doesn't exists");
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
        var f = File.CreateText(Path.Combine(Application.streamingAssetsPath, MultiprocessOrchestration.buildInfoFileName));
        f.Write(buildPath);
        f.Close();

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

        var buildPathToUse = buildPath;
        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            buildPathToUse += ".exe";
        }

        buildOptions &= ~BuildOptions.AutoRunPlayer;
        var buildReport = BuildPipeline.BuildPlayer(
            new[] { $"Assets/Scenes/{mainSceneName}.unity" },
            buildPathToUse,
            EditorUserBuildSettings.activeBuildTarget,
            buildOptions);

        return buildReport.summary.result == BuildResult.Succeeded;
    }
#endif
}
