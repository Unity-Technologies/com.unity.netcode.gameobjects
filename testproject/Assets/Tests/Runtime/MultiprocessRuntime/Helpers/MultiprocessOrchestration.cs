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
    public static bool IsPerformanceTest;
    public const string IsWorkerArg = "-isWorker";
    private static DirectoryInfo s_MultiprocessDirInfo;
    public static DirectoryInfo MultiprocessDirInfo
    {
        private set => s_MultiprocessDirInfo = value;
        get => s_MultiprocessDirInfo != null ? s_MultiprocessDirInfo : initMultiprocessDirinfo();
    }
    private static List<Process> s_Processes = new List<Process>();
    private static int s_TotalProcessCounter = 0;
    public static string UserProfile_Home;

    private static DirectoryInfo initMultiprocessDirinfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            UserProfile_Home = Environment.GetEnvironmentVariable("USERPROFILE");
        }
        else
        {
            UserProfile_Home = Environment.GetEnvironmentVariable("HOME");
        }
        s_MultiprocessDirInfo = new DirectoryInfo(Path.Combine(UserProfile_Home, ".multiprocess"));
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
    }

    /// <summary>
    /// Test to see if multimachine testing is enabled via command line switch.
    /// The result of this setting is so that any tests that want to run multimachine tests can
    /// decide if they are enabled or not.
    /// </summary>
    /// <returns></returns>
    public static bool ShouldRunMultiMachineTests()
    {
        return Environment.GetCommandLineArgs().Contains("-enableMultiMachineTesting");
    }

    public static BokkenMachine ProvisionWorkerNode(string platformString)
    {
        var bokkenMachine = BokkenMachine.Parse(platformString);
        bokkenMachine.PathToJson = Path.Combine(s_MultiprocessDirInfo.FullName, $"{bokkenMachine.Name}.json");
        var fi = new FileInfo(bokkenMachine.PathToJson);
        MultiprocessLogger.Log($"ProvisionWorkerNode - {bokkenMachine.PathToJson}: {fi.Exists}, parsed from {platformString}");
        if (!fi.Exists)
        {
            MultiprocessLogger.Log($"WARNING: Need to provision and set up a new machine named {bokkenMachine.Name} with path {bokkenMachine.PathToJson} because {fi.FullName} doesn't exist");
            bokkenMachine.Provision();
            bokkenMachine.Setup();
        }
        else
        {
            MultiprocessLogger.Log($"A machine named {bokkenMachine.Name} with path {bokkenMachine.PathToJson} already exists");
        }
        MultiprocessLogger.Log("ProvisionWorkerNode - Complete");
        return bokkenMachine;
    }

    public static void LogProcessList()
    {
        if (s_Processes != null)
        {
            foreach (var process in s_Processes)
            {
                if (!process.HasExited)
                {
                    MultiprocessLogger.Log($"Process item: {process.StartTime} {process.StartInfo.Arguments}");
                }
            }
        }
    }

    public static void ClearProcesslist()
    {
        if (s_Processes != null)
        {
            foreach (var process in s_Processes)
            {
                MultiprocessLogger.Log("About to call HasExited on a process");
                try
                {
                    if (!process.HasExited)
                    {
                        MultiprocessLogger.Log($"Teardown found an active process from MultiprocessOrchestration {process.ProcessName} {process.Id} {process.StartInfo.Arguments}");
                        process.CloseMainWindow();
                        process.Close();
                    }
                }
                catch (InvalidOperationException ioe)
                {
                    MultiprocessLogger.Log($"HasExited threw an exception {ioe.Message} {ioe.StackTrace}");
                }
            }
            s_Processes.Clear();
        }
    }

    public static string StartWorkerOnLocalNode()
    {
        MultiprocessLogger.Log($"Starting Worker on local node because: ShouldRunMultiMachineTests is {ShouldRunMultiMachineTests()}");
        var workerProcess = new Process();
        s_TotalProcessCounter++;
        if (s_Processes.Count > 0)
        {
            MultiprocessLogger.Log($"s_Processes.Count is {s_Processes.Count}");
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
                    throw new NotImplementedException($"Current platform is not supported");
            }
        }
        catch (FileNotFoundException)
        {
            Debug.LogError($"Could not find build info file. {buildInstructions}");
            throw;
        }

        MultiprocessLogger.Log($"extraArgs {extraArgs} workerProcess.StartInfo.FileName {workerProcess.StartInfo.FileName}");

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

    public static string[] GetRemotePlatformList()
    {
        // "default-win:test-win,default-mac:test-mac"
        string encodedPlatformList = Environment.GetEnvironmentVariable("MP_PLATFORM_LIST");
        if (encodedPlatformList == null)
        {
            MultiprocessLogger.Log($"MP_PLATFORM_LIST is null!");
            return null;
        }
        else
        {
            MultiprocessLogger.Log($"MP_PLATFORM_LIST is: {encodedPlatformList}");
        }
        string[] separated = encodedPlatformList.Split(',');
        return separated;

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
            }
            else
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
            // BokkenMachine.KillMultiprocessTestPlayer(f.FullName);
        }
    }

    public static void ShutdownAllProcesses(bool launchRemotely, int logCount)
    {

        MultiprocessLogger.Log($"{logCount + 0.1f} Shutting down all processes... by clearing the process from the stack");
        s_Processes.Clear();
        if (launchRemotely && ShouldRunMultiMachineTests())
        {
            MultiprocessLogger.Log($"{logCount + 0.2f} Shutting down all Bokken processes... by clearing the process from the stack");
            BokkenMachine.ProcessList.Clear();
        }
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
            if (!localhostname.Equals("Mac-mini.local"))
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
        }
        catch (Exception e)
        {
            MultiprocessLogger.LogError("Error: " + e.Message);
            MultiprocessLogger.LogError("Error Stack: " + e.StackTrace);
        }

        try
        {
            return GetLocalIPAddressFromNetworkInterface();
        }
        catch (Exception e)
        {
            MultiprocessLogger.LogError("Error: " + e.Message);
            MultiprocessLogger.LogError("Error Stack: " + e.StackTrace);
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    private static string GetLocalIPAddressFromNetworkInterface()
    {
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
        return "";
    }
}
