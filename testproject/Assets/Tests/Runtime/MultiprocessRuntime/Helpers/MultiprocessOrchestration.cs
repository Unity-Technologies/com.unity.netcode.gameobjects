using System;
using System.Linq;
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
    private static List<Process> s_Processes = new List<Process>();
    private static int s_TotalProcessCounter = 0;

    static MultiprocessOrchestration()
    {
        BaseMultiprocessTests.MultiProcessLog("Class initialized for MultiprocessOrchestration");
        string path = PathToLogFile();
        if (path != null)
        {
            BaseMultiprocessTests.MultiProcessLog($"PathToLogFile {path}");
            string tmpPath = Path.GetTempPath();
            BaseMultiprocessTests.MultiProcessLog($"tmpPath {tmpPath}");
            using var outputFile = new StreamWriter(Path.Combine(tmpPath, "WriteLines.txt"));
            outputFile.WriteLine(path);
        }

        BaseMultiprocessTests.MultiProcessLog($"The end");
    }

    /// <summary>
    /// This is to detect if we should ignore Multiprocess tests
    /// For testing, include the -bypassIgnoreUTR command line parameter when running UTR.
    /// </summary>
    public static bool ShouldIgnoreUTRTests()
    {
        return Environment.GetCommandLineArgs().Contains("-automated") && !Environment.GetCommandLineArgs().Contains("-bypassIgnoreUTR");
    }

    public static string PathToLogFile()
    {
        string[] allArgs = Environment.GetCommandLineArgs();
        foreach (var arg in allArgs)
        {
            if (arg.Contains("UnityLog.txt"))
            {
                return arg;
            }
        }
        return null;
    }

    public static int ActiveWorkerCount()
    {
        int activeWorkerCount = 0;
        if (s_Processes == null)
        {
            return activeWorkerCount;
        }
        foreach (var p in s_Processes)
        {
            if (!p.HasExited)
            {
                activeWorkerCount++;
            }
        }
        return activeWorkerCount;
    }

    public static void StartWorkerNode()
    {
        if (s_Processes == null)
        {
            s_Processes = new List<Process>();
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
        s_MultiprocessDirInfo = new DirectoryInfo(Path.Combine(userprofile, ".multiprocess"));

        var workerProcess = new Process();
        s_TotalProcessCounter++;
        if (s_Processes.Count > 0)
        {
            string message = "";
            foreach (var p in s_Processes)
            {
                message += $" {p.Id} {p.HasExited} {p.StartTime} ";
            }
            BaseMultiprocessTests.MultiProcessLog($"Current process count {s_Processes.Count} with data {message}");
        }
        s_Processes.Add(workerProcess);

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
                    extraArgs += "-popupwindow -screen-width 100 -screen-height 100";
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

        string logPath = Path.Combine(s_MultiprocessDirInfo.FullName, $"logfile-mp{s_TotalProcessCounter}.log");

        workerProcess.StartInfo.UseShellExecute = false;
        workerProcess.StartInfo.RedirectStandardError = true;
        workerProcess.StartInfo.RedirectStandardOutput = true;
        workerProcess.StartInfo.Arguments = $"{IsWorkerArg} {extraArgs} -logFile {logPath} -s {BuildMultiprocessTestPlayer.MainSceneName}";

        try
        {
            BaseMultiprocessTests.MultiProcessLog($"Attempting to start new process with log {logPath}, current process count: {s_Processes.Count}");
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
        BaseMultiprocessTests.MultiProcessLog("Shutting down all processes..");
        foreach (var process in s_Processes)
        {
            BaseMultiprocessTests.MultiProcessLog($"Shutting down process {process.Id} with state {process.HasExited}");
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

        BaseMultiprocessTests.MultiProcessLog($"Double check that all processes are shut down");
        foreach (var process in s_Processes)
        {
            BaseMultiprocessTests.MultiProcessLog($"Checking process {process.Id} with HasExited: {process.HasExited}");
            if (!process.HasExited)
            {
                BaseMultiprocessTests.MultiProcessLog($" {process.Id} has not exited ");
            }
        }
    }
}
