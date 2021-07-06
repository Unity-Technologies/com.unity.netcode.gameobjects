using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using MLAPI.MultiprocessRuntimeTests;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MultiprocessOrchestration
{
    public const string IsWorkerArg = "-isWorker";

    public static void StartWorkerNode()
    {
        var workerNode = new Process();

        //TODO this should be replaced eventually by proper orchestration for all supported platforms
        // Starting new local processes is a solution to help run perf tests locally. CI should have multi machine orchestration to
        // run performance tests with more realistic conditions.
        string buildInstructions = $"You probably didn't generate your build. Please make sure you build a player using the '{BuildMultiprocessTestPlayer.BuildAndExecuteMenuName}' menu";
        try
        {

            var buildPath = BuildMultiprocessTestPlayer.ReadBuildInfo().buildPath;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    workerNode.StartInfo.FileName = $"{buildPath}.app/Contents/MacOS/testproject";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    workerNode.StartInfo.FileName = $"{buildPath}.exe";
                    break;
                default:
                    throw new NotImplementedException($"{nameof(StartWorkerNode)}: Current platform is not supported");
            }
        }
        catch (FileNotFoundException)
        {
            Debug.LogError($"Couldn't find build info file. {buildInstructions}");
            throw;
        }

        workerNode.StartInfo.UseShellExecute = false;
        workerNode.StartInfo.RedirectStandardError = true;
        workerNode.StartInfo.RedirectStandardOutput = true;
        workerNode.StartInfo.Arguments = $"{IsWorkerArg} -popupwindow -screen-width 100 -screen-height 100";
        // workerNode.StartInfo.Arguments += " -deepprofiling"; // enable for deep profiling
        try
        {
            var newProcessStarted = workerNode.Start();
            if (!newProcessStarted)
            {
                throw new Exception("Failed to start process!");
            }
        }
        catch (Win32Exception e)
        {
            Debug.LogError($"Error starting player, {buildInstructions}, {e.Message} {e.Data} {e.ErrorCode}");
            throw;
        }
    }
}
