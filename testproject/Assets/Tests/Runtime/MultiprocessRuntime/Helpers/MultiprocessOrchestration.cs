using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
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
    private static FileInfo s_Localip_fileinfo;
    public const string IsWorkerArg = "-isWorker";
    private static DirectoryInfo s_MultiprocessDirInfo;
    public static DirectoryInfo MultiprocessDirInfo
    {
        private set => s_MultiprocessDirInfo = value;
        get => s_MultiprocessDirInfo != null ? s_MultiprocessDirInfo : initMultiprocessDirinfo();
    }
    private static List<Process> s_Processes = new List<Process>();
    private static int s_TotalProcessCounter = 0;

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
        s_Localip_fileinfo = new FileInfo(Path.Join(s_MultiprocessDirInfo.FullName, "localip"));
        return s_MultiprocessDirInfo;
    }

    static MultiprocessOrchestration()
    {
        initMultiprocessDirinfo();
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
            MultiprocessLogger.Log($"s_Processes.Count is {s_Processes.Count}");
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

    public static BokkenMachine ProvisionWorkerNode(string platformString)
    {
        var bokkenMachine = BokkenMachine.Parse(platformString);
        bokkenMachine.PathToJson = Path.Combine(s_MultiprocessDirInfo.FullName, $"{bokkenMachine.Name}.json");
        var fi = new FileInfo(bokkenMachine.PathToJson);
        if (!fi.Exists)
        {
            MultiprocessLogger.Log($"Need to provision and set up a new machine named {bokkenMachine.Name} with path {bokkenMachine.PathToJson}");
            bokkenMachine.Provision();
            bokkenMachine.Setup();
        }
        else
        {
            MultiprocessLogger.Log($"A machine named {bokkenMachine.Name} with path {bokkenMachine.PathToJson} already exists, just kill any old processes");
            bokkenMachine.KillMptPlayer();
        }    
        return bokkenMachine;
    }

    public static string StartWorkerNode()
    {
        if (s_Processes == null)
        {
            s_Processes = new List<Process>();
        }

        var jobid_fileinfo = new FileInfo(Path.Combine(s_MultiprocessDirInfo.FullName, "jobid"));
        var resources_fileinfo = new FileInfo(Path.Combine(s_MultiprocessDirInfo.FullName, "resources"));
        var rootdir_fileinfo = new FileInfo(Path.Combine(s_MultiprocessDirInfo.FullName, "rootdir"));

        if (jobid_fileinfo.Exists && resources_fileinfo.Exists && rootdir_fileinfo.Exists)
        {
            MultiprocessLogger.Log("Run on remote nodes because jobid, resource and rootdir files exist");
            StartWorkersOnRemoteNodes(rootdir_fileinfo);
            return "";
        }        
        else
        {
            MultiprocessLogger.Log($"Run on local nodes: current count is {s_Processes.Count}");
            return StartWorkerOnLocalNode();
        }
    }

    public static string StartWorkerOnLocalNode()
    {
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
                    extraArgs += "-batchmode -nographics";
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
        workerProcess.StartInfo.Arguments = $"{IsWorkerArg} {extraArgs} -logFile {logPath} -s {BuildMultiprocessTestPlayer.MainSceneName}";

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
    

    public static void StartWorkersOnRemoteNodes(FileInfo rootdir_fileinfo)
    {
        string launch_platform = Environment.GetEnvironmentVariable("LAUNCH_PLATFORM");
        MultiprocessLogger.Log("StartWorkerOnRemoteNodes");
        // That suggests sufficient information to determine that we can run remotely
        string rootdir = (File.ReadAllText(rootdir_fileinfo.FullName)).Trim();
        var fileName = Path.Combine(rootdir, "BokkenCore31", "bin", "Debug", "netcoreapp3.1", "BokkenCore31.dll");
        var fileNameInfo = new FileInfo(fileName);

        MultiprocessLogger.Log($"launching {fileName} does it exist {fileNameInfo.Exists} ");

        var workerProcess = new Process();
        // workerProcess.StartInfo.FileName = Path.Combine(rootdir, "BokkenCore31", "bin", "Debug", "netcoreapp3.1", "BokkenCore31.exe");
        workerProcess.StartInfo.FileName = Path.Combine("dotnet");
        workerProcess.StartInfo.UseShellExecute = false;
        workerProcess.StartInfo.RedirectStandardError = true;
        workerProcess.StartInfo.RedirectStandardOutput = true;
        workerProcess.StartInfo.Arguments = $"{fileName} launch {launch_platform}";
        try
        {
            MultiprocessLogger.Log($"{workerProcess.StartInfo.Arguments}");
            var newProcessStarted = workerProcess.Start();
            if (!newProcessStarted)
            {
                throw new Exception("Failed to start worker process!");
            } else
            {
                MultiprocessLogger.Log($" {workerProcess.HasExited} ");
            }
        }
        catch (Win32Exception e)
        {
            MultiprocessLogger.LogError($"Error starting bokken process, {e.Message} {e.Data} {e.ErrorCode}");
            throw;
        }
    }

    public static void KillAllTestPlayersOnRemoteMachines()
    {
        
        foreach (var f in MultiprocessDirInfo.GetFiles("*.json"))
        {
            BokkenMachine.KillMultiprocessTestPlayer(f.FullName);
        }
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

    private static void WriteLocalIP(string localip)
    {
        using StreamWriter sw = File.CreateText(s_Localip_fileinfo.FullName);
        sw.WriteLine(localip);
    }

    public static string GetLocalIPAddress()
    {
        
        string bOKKEN_HOST_IP = Environment.GetEnvironmentVariable("BOKKEN_HOST_IP");
        if (!string.IsNullOrEmpty(bOKKEN_HOST_IP) && bOKKEN_HOST_IP.Contains("."))
        {

            MultiprocessLogger.Log($"BOKKEN_HOST_IP was found as {bOKKEN_HOST_IP}");
            return bOKKEN_HOST_IP;
        }

        if (s_Localip_fileinfo.Exists)
        {            
            string alllines = File.ReadAllText(s_Localip_fileinfo.FullName).Trim();
            MultiprocessLogger.Log($"localIP file was found as {alllines}");
            return alllines;
        }

        
        string localhostname = Dns.GetHostName();
        

        try
        {
            var host = Dns.GetHostEntry(localhostname);
        
            foreach (var ip in host.AddressList)
            {
        
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    string localIPAddress = ip.ToString();
        
                    WriteLocalIP(localIPAddress);
                    return localIPAddress;
                }
            }
        }
        catch (Exception e)
        {
            MultiprocessLogger.LogError(e.Message);
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                 
                    if (ip.Address.ToString().Contains(".") && !ip.Address.ToString().Equals("127.0.0.1"))
                    {
                        // TODO: Write this to a file so we don't have to keep getting this IP over and over
                        WriteLocalIP(ip.Address.ToString());
                        return ip.Address.ToString();
                    }
                }
            }
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}
