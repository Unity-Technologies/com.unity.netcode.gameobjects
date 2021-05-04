using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI.MultiprocessRuntimeTests;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// This is needed as Unity throws "An abnormal situation has occurred: the PlayerLoop internal function has been called recursively. Please contact Customer Support with a sample project so that we can reproduce the problem and troubleshoot it."
/// when trying to build from Setup() steps in tests.
/// </summary>
public class BuildAndRunMultiprocessTests : MonoBehaviour
{
    [MenuItem("MLAPI Tests/Execute multiprocess tests no build %&t")]
    public static void ExecuteNoBuild()
    {
        Execute(build: false);
    }

    [MenuItem("MLAPI Tests/Build and execute multiprocess tests %t")]
    public static void BuildAndExecute()
    {
        Execute(build: true);
    }

    /// <summary>
    /// To run these from the command line, call
    /// runMultiplayerTests.sh
    ///
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static void Execute(bool build)
    {
        var shouldContinue = build ? Build(TestCoordinator.buildPath) : true;
        if (shouldContinue)
        {
            StartMainTestNodeInEditor();
            // todo this doesn't work from the command line. if -executeMethod is used, EditorApplication doesn't update
            // however, calling from the commandline -runTests with platform playmode does work. Will need to figure out
            // what's the difference between the two and how to get EditorApplication to run outside of -runTests
            // right now, can just run both executeMethod (which will launch the players) and -runTests one after the other to
            // get a successful test.
        }
        else
        {
            throw new Exception("Build failed to create!!");
        }
    }

    public static bool Build(string buildPath)
    {

        var buildOptions = BuildOptions.IncludeTestAssemblies;
        buildOptions |= BuildOptions.Development | BuildOptions.ConnectToHost | BuildOptions.IncludeTestAssemblies | BuildOptions.StrictMode;
        // buildOptions |= BuildOptions.AllowDebugging; // enable this if you want to debug your players. Your players
        // will have more connection permission popups when launching though

        buildOptions &= ~BuildOptions.AutoRunPlayer;
        var buildReport = BuildPipeline.BuildPlayer(
            new string[] { "Assets/Scenes/MultiprocessTestingScene.unity" },
            buildPath,
            BuildTarget.StandaloneOSX,
            buildOptions);

        return buildReport.summary.result == BuildResult.Succeeded;
    }

    private static void StartMainTestNodeInEditor()
    {
        var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();

        testRunnerApi.Execute(new ExecutionSettings()
            {
                filters = new Filter[]
                {
                    new Filter()
                    {
                        categoryNames = new [] {MultiprocessTests.categoryName},
                        testMode = TestMode.PlayMode
                    },
                },
            }
        );
    }
}
