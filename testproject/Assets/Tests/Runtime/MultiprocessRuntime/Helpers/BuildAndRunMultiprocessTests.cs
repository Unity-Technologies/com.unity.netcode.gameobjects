using System;
using System.IO;
using MLAPI;
using MLAPI.MultiprocessRuntimeTests;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;
#endif
using UnityEngine;

/// <summary>
/// This is needed as Unity throws "An abnormal situation has occurred: the PlayerLoop internal function has been called recursively. Please contact Customer Support with a sample project so that we can reproduce the problem and troubleshoot it."
/// when trying to build from Setup() steps in tests.
/// </summary>
public class BuildAndRunMultiprocessTests : MonoBehaviour
{
    public const string BuildAndExecuteMenuName = "MLAPI Tests/Build - Execute multiprocess tests #%t";
    public static string buildPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds/MultiprocessTestBuild");
    public const string mainSceneName = "MultiprocessTestingScene";


#if UNITY_EDITOR
    [MenuItem("MLAPI Tests/Build Test Player #t")]
    public static void BuildNoExecute()
    {
        var success = Build();
        if (!success)
        {
            throw new Exception("Build failed!");
        }
    }

    [MenuItem("MLAPI Tests/Build Test Player in debug mode")]
    public static void BuildDebug()
    {
        var success = Build(true);
        if (!success)
        {
            throw new Exception("Build failed!");
        }
    }


    [MenuItem("MLAPI Tests/Delete Test Build")]
    public static void DeleteBuild()
    {
#if UNITY_EDITOR_OSX
        var toDelete = buildPath + ".app";
        if (Directory.Exists(toDelete))
        {
            Directory.Delete(toDelete, recursive: true);
        }
        else
        {
            Debug.Log($"directory {toDelete} doesn't exists");
        }
#elif UNITY_EDITOR_WIN
        var exePath = Path.Combine(buildPath, $"{PlayerSettings.productName}.exe");
        if (File.Exists(exePath))
        {
            File.Delete(exePath);
        }
        else
        {
            Debug.Log($"exe {exePath} doesn't exists");
        }
#else
        throw new NotImplementedException();
#endif
    }

    /// <summary>
    /// Needs a separate build than the standalone test builds since we don't want the player to try to connect to the editor to do test
    /// reporting. We only want to main node to do that, worker nodes should be dumb
    /// </summary>
    /// <returns></returns>
    public static bool Build(bool isDebug=false)
    {
        // Save standalone build path to file
        var f = File.CreateText(Path.Combine(Application.streamingAssetsPath, MultiprocessOrchestration.buildInfoFileName));
        f.Write(buildPath);
        f.Close();

        // var buildPath = Application.streamingAssetsPath;
        // deleting so we don't endup testing on outdated builds
        DeleteBuild();
#if UNITY_EDITOR_OSX
        var buildTarget = BuildTarget.StandaloneOSX;
#elif UNITY_EDITOR_WIN
        // todo test on windows
        var buildTarget = BuildTarget.StandaloneWindows;
#else
        throw new NotImplementedException("Building for this platform is not supported");
#endif
        var buildOptions = BuildOptions.None;
        buildOptions |= BuildOptions.IncludeTestAssemblies;
        buildOptions |= BuildOptions.StrictMode;
        if (isDebug)
        {
            buildOptions |= BuildOptions.Development;
            buildOptions |= BuildOptions.AllowDebugging; // enable this if you want to debug your players. Your players
            // will have more connection permission popups when launching though
        }

        buildOptions &= ~BuildOptions.AutoRunPlayer;
        var buildReport = BuildPipeline.BuildPlayer(
            new string[] { $"Assets/Scenes/{mainSceneName}.unity" },
            buildPath,
            buildTarget,
            buildOptions);

        return buildReport.summary.result == BuildResult.Succeeded;
    }
#endif

}
