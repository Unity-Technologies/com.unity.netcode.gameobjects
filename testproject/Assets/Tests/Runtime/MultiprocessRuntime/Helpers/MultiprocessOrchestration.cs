using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Unity.Netcode.MultiprocessRuntimeTests;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MultiprocessOrchestration
{
    public const string IsWorkerArg = "-isWorker";

    public static void StartWorkersOnRemoteNodes()
    {
        string userprofile = Environment.GetEnvironmentVariable("USERPROFILE");
        var userprofile_di = new DirectoryInfo(userprofile);
        var multiprocess_di = new DirectoryInfo(Path.Combine(userprofile, ".multiprocess"));
        var jobid_fileinfo = new FileInfo(Path.Combine(multiprocess_di.FullName, "jobid"));
        var resources_fileinfo = new FileInfo(Path.Combine(multiprocess_di.FullName, "resources"));
        var rootdir_fileinfo = new FileInfo(Path.Combine(multiprocess_di.FullName, "rootdir"));
        
        if (jobid_fileinfo.Exists && resources_fileinfo.Exists && rootdir_fileinfo.Exists)
        {
            // That suggests sufficient information to determine that we can run remotely
            string rootdir = (File.ReadAllText(rootdir_fileinfo.FullName)).Trim();
            var workerProcess = new Process();
            // workerProcess.StartInfo.FileName = Path.Combine(rootdir, "BokkenCore31", "bin", "Debug", "netcoreapp3.1", "BokkenCore31.exe");
            workerProcess.StartInfo.FileName = Path.Combine("dotnet");
            workerProcess.StartInfo.UseShellExecute = false;
            workerProcess.StartInfo.RedirectStandardError = true;
            workerProcess.StartInfo.RedirectStandardOutput = true;
            workerProcess.StartInfo.Arguments = $"BokkenCore31/bin/Debug/netcoreapp3.1/BokkenCore31.dll launch";
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
        else
        {
            throw new FileNotFoundException($"multiprocess files not found: {jobid_fileinfo.FullName} {jobid_fileinfo.Exists} {resources_fileinfo.FullName} {resources_fileinfo.Exists} {rootdir_fileinfo.FullName} {rootdir_fileinfo.Exists}");
        }
        
    }

    public static void StartWorkerNode()
    {
        var workerProcess = new Process();

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

        workerProcess.StartInfo.UseShellExecute = false;
        workerProcess.StartInfo.RedirectStandardError = true;
        workerProcess.StartInfo.RedirectStandardOutput = true;
        workerProcess.StartInfo.Arguments = $"{IsWorkerArg} -popupwindow -screen-width 100 -screen-height 100";
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
}
