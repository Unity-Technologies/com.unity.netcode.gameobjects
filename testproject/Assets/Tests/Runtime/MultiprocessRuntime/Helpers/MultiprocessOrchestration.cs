using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Netcode.MultiprocessRuntimeTests;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MultiprocessOrchestration
{
    public const string IsWorkerArg = "-isWorker";
    private static DirectoryInfo s_MultiprocessDirInfo;
    public static List<Process> Processes = new List<Process>();

    /// <summary>
    /// This is to detect if we should ignore Multiprocess tests
    /// For testing, include the -bypassIgnoreUTR command line parameter when running UTR.
    /// </summary>
    public static bool ShouldIgnoreUTRTests()
    {
        return Environment.GetCommandLineArgs().Contains("-automated") && !Environment.GetCommandLineArgs().Contains("-bypassIgnoreUTR");
    }

    public static void StartWorkerNode()
    {
        if (Processes == null)
        {
            Processes = new List<Process>();
        }

        string userprofile = "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            userprofile = Environment.GetEnvironmentVariable("USERPROFILE");
        }
        else
        {
            userprofile = Environment.GetEnvironmentVariable("HOME");
        }
        // Debug.Log($"userprofile is {userprofile}");
        s_MultiprocessDirInfo = new DirectoryInfo(Path.Combine(userprofile, ".multiprocess"));

        var workerProcess = new Process();
        if (Processes.Count > 0)
        {
            string message = "";
            foreach (var p in Processes)
            {
                message += $" {p.Id} {p.HasExited} {p.StartTime} ";
            }
            Debug.Log($"Current process count {Processes.Count} with data {message}");
        }
        Processes.Add(workerProcess);

        //TODO this should be replaced eventually by proper orchestration for all supported platforms
        // Starting new local processes is a solution to help run perf tests locally. CI should have multi machine orchestration to
        // run performance tests with more realistic conditions.
        string buildInstructions = $"You probably didn't generate your build. Please make sure you build a player using the '{BuildMultiprocessTestPlayer.BuildAndExecuteMenuName}' menu";
        string extraArgs = "";
        try
        {
            var buildPath = BuildMultiprocessTestPlayer.ReadBuildInfo().BuildPath;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    workerProcess.StartInfo.FileName = $"{buildPath}.app/Contents/MacOS/testproject";
                    extraArgs += "-popupwindow -screen-width 100 -screen-height 100";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    workerProcess.StartInfo.FileName = $"{buildPath}.exe";
                    extraArgs += "-popupwindow -screen-width 100 -screen-height 100";
                    break;
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    workerProcess.StartInfo.FileName = $"{buildPath}";
                    extraArgs += "-nographics";
                    break;
                default:
                    throw new NotImplementedException($"{nameof(StartWorkerNode)}: Current platform is not supported");
            }
        }
        catch (FileNotFoundException)
        {
            Debug.LogError($"Could not find build info file. {buildInstructions}");
            throw;
        }

        string logPath = Path.Combine(s_MultiprocessDirInfo.FullName, $"logfile-mp{Processes.Count}");


        workerProcess.StartInfo.UseShellExecute = false;
        workerProcess.StartInfo.RedirectStandardError = true;
        workerProcess.StartInfo.RedirectStandardOutput = true;

        workerProcess.StartInfo.Arguments = $"{IsWorkerArg} {extraArgs} -logFile {logPath} -s {BuildMultiprocessTestPlayer.MainSceneName}";

        try
        {
            Debug.Log($"Attempting to start new process, current process count: {Processes.Count}");
            var newProcessStarted = workerProcess.Start();
            if (!newProcessStarted)
            {
                throw new Exception("Failed to start worker process!");
            }
        }
        catch (Win32Exception e)
        {
            Debug.LogError($"Error starting player, {buildInstructions}, {e.Message} {e.Data} {e.ErrorCode}");
            throw;
        }
    }

    public static void ShutdownAllProcesses()
    {
        Debug.Log("Shutting down all processes..");
        foreach (var process in Processes)
        {
            Debug.Log($"Shutting down process {process.Id} with state {process.HasExited}");
            try
            {
                if (!process.HasExited)
                {
                    // Close process by sending a close message to its main window.
                    process.CloseMainWindow();

                    // Free resources associated with process.
                    process.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        Processes.Clear();
    }
}
