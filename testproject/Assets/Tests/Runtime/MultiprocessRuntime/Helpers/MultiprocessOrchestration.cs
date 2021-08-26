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
    public static List<Process> Processes = new List<Process>();
    private static DirectoryInfo m_MultiprocessDirInfo;

    public static void StartWorkerOnNodes()
    {
        if (Processes == null)
        {
            Processes = new List<Process>();
        }
        Debug.Log("Determine whether to start on local or remote nodes");
        string userprofile = "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            userprofile = Environment.GetEnvironmentVariable("USERPROFILE");
        }
        else
        {
            userprofile = Environment.GetEnvironmentVariable("HOME");
        }
        Debug.Log($"userprofile is {userprofile}");

        m_MultiprocessDirInfo = new DirectoryInfo(Path.Combine(userprofile, ".multiprocess"));
        var jobid_fileinfo = new FileInfo(Path.Combine(m_MultiprocessDirInfo.FullName, "jobid"));
        var resources_fileinfo = new FileInfo(Path.Combine(m_MultiprocessDirInfo.FullName, "resources"));
        var rootdir_fileinfo = new FileInfo(Path.Combine(m_MultiprocessDirInfo.FullName, "rootdir"));

        if (!m_MultiprocessDirInfo.Exists)
        {
            m_MultiprocessDirInfo.Create();
        }

        if (jobid_fileinfo.Exists && resources_fileinfo.Exists && rootdir_fileinfo.Exists)
        {
            Debug.Log("Run on remote nodes");
            StartWorkersOnRemoteNodes(rootdir_fileinfo);
        }
        else
        {
            Debug.Log($"Run on local nodes: current count is {Processes.Count}");
            StartWorkerNode();
        }
    }

    public static void StartWorkersOnRemoteNodes(FileInfo rootdir_fileinfo)
    {
        // That suggests sufficient information to determine that we can run remotely
        string rootdir = (File.ReadAllText(rootdir_fileinfo.FullName)).Trim();
        var fileName = Path.Combine(rootdir, "BokkenCore31", "bin", "Debug", "netcoreapp3.1", "BokkenCore31.dll");

        var workerProcess = new Process();
        // workerProcess.StartInfo.FileName = Path.Combine(rootdir, "BokkenCore31", "bin", "Debug", "netcoreapp3.1", "BokkenCore31.exe");
        workerProcess.StartInfo.FileName = Path.Combine("dotnet");
        workerProcess.StartInfo.UseShellExecute = false;
        workerProcess.StartInfo.RedirectStandardError = true;
        workerProcess.StartInfo.RedirectStandardOutput = true;
        workerProcess.StartInfo.Arguments = $"{fileName} launch";
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
            Debug.LogError($"Error starting bokken process, {e.Message} {e.Data} {e.ErrorCode}");
              throw;
        }    
    }

    public static void StartWorkerNode()
    {
        Debug.Log($"Run on local nodes: current count is {Processes.Count}");
        foreach (Process p in Processes)
        {
            Debug.Log($"Has the process exited? {p.HasExited} {p.Id} {p.ProcessName} is responding? {p.Responding}");
        }

        var workerProcess = new Process();
        Processes.Add(workerProcess);
        
        //TODO this should be replaced eventually by proper orchestration for all supported platforms
        // Starting new local processes is a solution to help run perf tests locally. CI should have multi machine orchestration to
        // run performance tests with more realistic conditions.
        string buildInstructions = $"You probably didn't generate your build. Please make sure you build a player using the '{BuildMultiprocessTestPlayer.BuildAndExecuteMenuName}' menu";
        try
        {

            var buildPath = BuildMultiprocessTestPlayer.ReadBuildInfo().BuildPath;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    workerProcess.StartInfo.FileName = $"{buildPath}.app/Contents/MacOS/testproject";
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    workerProcess.StartInfo.FileName = $"{buildPath}.exe";
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

        string logPath = Path.Combine(m_MultiprocessDirInfo.FullName, $"zlogfile{Processes.Count}");

        workerProcess.StartInfo.UseShellExecute = false;
        workerProcess.StartInfo.RedirectStandardError = true;
        workerProcess.StartInfo.RedirectStandardOutput = true;
        workerProcess.StartInfo.Arguments = $"{IsWorkerArg} -popupwindow -screen-width 100 -screen-height 100 -logFile {logPath}";
        // workerNode.StartInfo.Arguments += " -deepprofiling"; // enable for deep profiling
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
            Debug.LogError($"Error starting player, {buildInstructions}, {e.Message} {e.Data} {e.ErrorCode}");
            throw;
        }
    }

    public static bool IsMultiprocessTestPlayerAvailable()
    {
        bool answer = false;
        try
        {
            var buildInfoPath = Path.Combine(Application.streamingAssetsPath, BuildMultiprocessTestPlayer.BuildInfoFileName);
            var buildInfoFileInfo = new FileInfo(buildInfoPath);
            if (!buildInfoFileInfo.Exists)
            {
                return false;
            }
            var buildPath = BuildMultiprocessTestPlayer.ReadBuildInfo().BuildPath;
            FileInfo buildPathFileInfo = null;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    buildPathFileInfo = new FileInfo($"{buildPath}.app/Contents/MacOS/testproject");
                    break;
                case RuntimePlatform.WindowsEditor:
                    buildPathFileInfo = new FileInfo($"{buildPath}.exe");
                    break;
                case RuntimePlatform.LinuxEditor:
                    buildPathFileInfo = new FileInfo($"{buildPath}");
                    break;
            }

            if (buildPathFileInfo != null && buildPathFileInfo.Exists)
            {
                answer = true;
            }

        } catch (Exception e)
        {
            Debug.LogException(e);
            answer = false;
        }
        
        return answer;
    }
}
