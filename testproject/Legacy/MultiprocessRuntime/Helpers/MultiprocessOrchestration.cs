using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Netcode.MultiprocessRuntimeTests;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MultiprocessOrchestration
{
    public static bool IsPerformanceTest;
    public const string IsWorkerArg = "-isWorker";
    private static DirectoryInfo s_MultiprocessDirInfo;
    public static DirectoryInfo MultiprocessDirInfo
    {
        private set => s_MultiprocessDirInfo = value;
        get => s_MultiprocessDirInfo ?? initMultiprocessDirinfo();
    }
    private static List<Process> s_Processes = new List<Process>();
    private static int s_TotalProcessCounter = 0;
    public static string PathToDll { get; private set; }
    public static List<Process> ProcessList = new List<Process>();
    private static FileInfo s_Localip_fileinfo;

    private static DirectoryInfo initMultiprocessDirinfo()
    {
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
        if (!MultiprocessDirInfo.Exists)
        {
            MultiprocessDirInfo.Create();
        }
        s_Localip_fileinfo = new FileInfo(Path.Combine(s_MultiprocessDirInfo.FullName, "localip"));

        return s_MultiprocessDirInfo;
    }

    static MultiprocessOrchestration()
    {
        initMultiprocessDirinfo();
        MultiprocessLogger.Log($" userprofile: {s_MultiprocessDirInfo.FullName} localipfile: {s_Localip_fileinfo}");
        var rootdir_FileInfo = new FileInfo(Path.Combine(MultiprocessDirInfo.FullName, "rootdir"));
        MultiprocessLogger.Log($"Checking for the existence of {rootdir_FileInfo.FullName}");
        if (rootdir_FileInfo.Exists)
        {
            var rootDirText = (File.ReadAllText(rootdir_FileInfo.FullName)).Trim();
            PathToDll = Path.Combine(rootDirText, "multiplayer-multiprocess-test-tools/BokkenForNetcode/ProvisionBokkenMachines/bin/Debug/netcoreapp3.1/osx-x64", "ProvisionBokkenMachines.dll");
        }
        else
        {
            MultiprocessLogger.Log("PathToDll cannot be set as rootDir doesn't exist");
            PathToDll = "unknown";
        }
    }

    /// <summary>
    /// This is to detect if we should ignore Multiprocess tests
    /// For testing, include the -bypassIgnoreUTR command line parameter when running UTR.
    /// </summary>
    public static bool ShouldIgnoreUTRTests()
    {
        return Environment.GetCommandLineArgs().Contains("-automated") && !Environment.GetCommandLineArgs().Contains("-bypassIgnoreUTR");
    }

    public static int ActiveWorkerCount()
    {
        int activeWorkerCount = 0;
        if (s_Processes == null)
        {
            return activeWorkerCount;
        }

        if (s_Processes.Count > 0)
        {
            foreach (var p in s_Processes)
            {
                if ((p != null) && (!p.HasExited))
                {
                    activeWorkerCount++;
                }
            }
        }
        return activeWorkerCount;
    }

    public static string StartWorkerNode()
    {
        if (s_Processes == null)
        {
            s_Processes = new List<Process>();
        }

        var workerProcess = new Process();
        s_TotalProcessCounter++;
        if (s_Processes.Count > 0)
        {
            string message = "";
            foreach (var p in s_Processes)
            {
                message += $" {p.Id} {p.HasExited} {p.StartTime} ";
            }
            MultiprocessLogger.Log($"Current process count {s_Processes.Count} with data {message}");
        }

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
                    // extraArgs += "-popupwindow -screen-width 100 -screen-height 100";
                    extraArgs += "-batchmode -nographics";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    workerProcess.StartInfo.FileName = $"{buildPath}.exe";
                    //extraArgs += "-popupwindow -screen-width 100 -screen-height 100";
                    extraArgs += "-popupwindow";
                    break;
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    workerProcess.StartInfo.FileName = $"{buildPath}";
                    // extraArgs += "-popupwindow -screen-width 100 -screen-height 100";
                    extraArgs += "-batchmode -nographics";
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

        string logPath = Path.Combine(MultiprocessDirInfo.FullName, $"logfile-mp{s_TotalProcessCounter}.log");

        workerProcess.StartInfo.UseShellExecute = false;
        workerProcess.StartInfo.RedirectStandardError = true;
        workerProcess.StartInfo.RedirectStandardOutput = true;
        workerProcess.StartInfo.Arguments = $"{IsWorkerArg} {extraArgs} -logFile {logPath}";

        try
        {
            MultiprocessLogger.Log($"Attempting to start new process, current process count: {s_Processes.Count} with arguments {workerProcess.StartInfo.Arguments}");
            var newProcessStarted = workerProcess.Start();
            if (!newProcessStarted)
            {
                throw new Exception("Failed to start worker process!");
            }
            s_Processes.Add(workerProcess);
        }
        catch (Win32Exception e)
        {
            MultiprocessLogger.LogError($"Error starting player, {buildInstructions}, {e}");
            throw;
        }
        return logPath;
    }

    public static void ShutdownAllProcesses()
    {
        MultiprocessLogger.Log("Shutting down all processes..");
        foreach (var process in s_Processes)
        {
            MultiprocessLogger.Log($"Shutting down process {process.Id} with state {process.HasExited}");
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

        s_Processes.Clear();
    }

    public static bool IsRemoteOperationEnabled()
    {
        string encodedPlatformList = Environment.GetEnvironmentVariable("MP_PLATFORM_LIST");
        if (encodedPlatformList != null && encodedPlatformList.Split(',').Length > 1)
        {
            return true;
        }
        return false;
    }

    public static string[] GetRemotePlatformList()
    {
        // "default-win:test-win,default-mac:test-mac"
        if (!IsRemoteOperationEnabled())
        {
            return null;
        }
        string encodedPlatformList = Environment.GetEnvironmentVariable("MP_PLATFORM_LIST");
        string[] separated = encodedPlatformList.Split(',');
        return separated;
    }

    public static List<FileInfo> GetRemoteMachineList()
    {
        var machineJson = new List<FileInfo>();
        foreach (var f in MultiprocessDirInfo.GetFiles("*.json"))
        {
            if (f.Name.Equals("remoteConfig.json"))
            {
                continue;
            }
            else
            {
                machineJson.Add(f);
            }
        }
        return machineJson;
    }

    public static Process StartWorkersOnRemoteNodes(FileInfo machine)
    {
        string command = $" --command launch " +
                $"--input-path {machine.FullName} ";

        var workerProcess = new Process();

        workerProcess.StartInfo.FileName = Path.Combine("dotnet");
        workerProcess.StartInfo.UseShellExecute = false;
        workerProcess.StartInfo.RedirectStandardError = true;
        workerProcess.StartInfo.RedirectStandardOutput = true;
        workerProcess.StartInfo.Arguments = $"{PathToDll} {command} ";
        try
        {
            var newProcessStarted = workerProcess.Start();

            if (!newProcessStarted)
            {
                throw new Exception("Failed to start worker process!");
            }
        }
        catch (Win32Exception e)
        {
            MultiprocessLogger.LogError($"Error starting bokken process, {e.Message} {e.Data} {e.ErrorCode}");
            throw;
        }


        ProcessList.Add(workerProcess);

        MultiprocessLogger.Log($"Execute Command: {PathToDll} {command} End");
        return workerProcess;
    }
}
