using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MultiprocessOrchestration
{
    public const string buildInfoFileName = "buildInfo.txt";
    public const string isWorkerArg = "-isWorker";

    public static void StartWorkerNode()
    {
        var workerNode = new Process();

        //TODO this should be replaced eventually by proper orchestration for all supported platforms
        string buildInstructions = $"You probably didn't generate your build. Please make sure you build a player using the '{BuildMultiprocessTestPlayer.BuildAndExecuteMenuName}' menu";
        try
        {
            var buildInfo = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, buildInfoFileName));
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    workerNode.StartInfo.FileName = $"{buildInfo}.app/Contents/MacOS/testproject";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    workerNode.StartInfo.FileName = $"{buildInfo}.exe";
                    break;
                default:
                    throw new NotImplementedException("StartWorkerNode: Current platform not supported");
            }
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
