using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MultiprocessOrchestration
{
    public const string buildInfoFileName = "buildInfo.txt";
    public const string isWorkerArg = "-isWorker";

    public static void StartWorkerNode()
    {
        var workerNode = new Process();

        //TODO this should be replaced eventually by proper orchestration
        // TODO test on windows
        const string exeName = "testproject";
        string buildInstructions = $"You probably didn't generate your build. Please make sure you build a player using the '{BuildMultiprocessTestPlayer.BuildAndExecuteMenuName}' menu";
        try
        {
            var buildInfo = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, buildInfoFileName));
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            workerNode.StartInfo.FileName = $"{buildInfo}.app/Contents/MacOS/{exeName}";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            workerNode.StartInfo.FileName = $"{buildInfo}/{exeName}.exe";
#else
            throw new NotImplementedException("StartWorkerNode: Current platform not supported");
#endif
        }
        catch (FileNotFoundException)
        {
            throw new Exception($"Couldn't find build info file. {buildInstructions}");
        }

        workerNode.StartInfo.UseShellExecute = false;
        workerNode.StartInfo.RedirectStandardError = true;
        workerNode.StartInfo.RedirectStandardOutput = true;
        workerNode.StartInfo.Arguments = $"{isWorkerArg} -popupwindow -screen-width 100 -screen-height 100";
        try
        {
            var newProcessStarted = workerNode.Start();
            Debug.Log($"new process started? {newProcessStarted}");
            if (!newProcessStarted)
            {
                throw new Exception("Process not started!");
            }
        }
        catch (Win32Exception e)
        {
            Debug.LogError($"Error starting player, {buildInstructions}, {e.Message} {e.Data} {e.ErrorCode}");
            throw;
        }
    }
}
