using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class BokkenMachine
    {
        public DateTimeOffset DateCreated { get; set; }
        public string Name { get; private set; }
        public string Type { get; set; }
        public string Image { get; set; }
        public string Flavor { get; set; }
        public string JobID { get; set; }
        public FileInfo JobFile { get; private set; }
        public string PathToJson { get; set; }
        public string LogPath { get; set; }

        public Dictionary<string, BokkenMachine> BokkenMachines;

        private static FileInfo s_FileInfo;
        private static string s_Rootdir;
        public static string PathToDll { get; private set; }

        private static List<Process> s_ProcessList = new List<Process>();

        static BokkenMachine()
        {
            s_FileInfo = new FileInfo(Path.Combine(MultiprocessOrchestration.MultiprocessDirInfo.FullName, "rootdir"));
            s_Rootdir = (File.ReadAllText(s_FileInfo.FullName)).Trim();
            PathToDll = Path.Combine(s_Rootdir, "BokkenForNetcode", "ProvisionBokkenMachines", "bin", "Debug", "netcoreapp3.1", "ProvisionBokkenMachines.dll");
            MultiprocessLogger.Log($"Bokken Machine vars are: \n\ts_FileInfo {s_FileInfo.FullName}, {s_FileInfo.Exists} \n\ts_Rootdir {s_Rootdir}\n\tPathToDll {PathToDll}");
        }

        public static void DumpProcessList()
        {
            foreach (Process process in s_ProcessList)
            {
                MultiprocessLogger.Log($"Process info: id: {process.Id} HasExited: {process.HasExited} {process.StartInfo.Arguments}");
            }
        }

        public static BokkenMachine GetDefaultMac(string name)
        {
            var defaultMac = new BokkenMachine();
            defaultMac.Type = "Unity::VM::osx";
            defaultMac.Image = "unity-ci/macos-10.15-dotnetcore:latest";
            defaultMac.Flavor = "b1.large";
            defaultMac.Name = name;
            return defaultMac;
        }

        public static BokkenMachine GetDefaultWin(string name)
        {
            var defaultWindows = new BokkenMachine();
            defaultWindows.Type = "Unity::VM";
            defaultWindows.Image = "package-ci/win10:stable";
            defaultWindows.Flavor = "b1.large";
            defaultWindows.Name = name;
            return defaultWindows;
        }

        public static BokkenMachine Parse(string shortcut)
        {
            BokkenMachine bokkenMachine = null;
            string[] parts = shortcut.Split(':');
            string name = parts[1];
            string type = parts[0];
            if (type.Equals("default-mac"))
            {
                bokkenMachine = GetDefaultMac(name);
            }
            else if (type.Equals("default-win"))
            {
                bokkenMachine = GetDefaultWin(name);
            }

            return bokkenMachine;
        }
        public BokkenMachine()
        {
        }

        /// <summary>
        /// Provision a new machine using the Bokken API, this command is async
        /// so we don't really need to wait for completion or set a specific
        /// timeout value
        /// </summary>
        public void Provision()
        {
            MultiprocessLogger.Log($"Provision start {Name}");
            ExecuteCommand(GenerateCreateCommand(), true);
            MultiprocessLogger.Log($"Provision end {Name}");
        }

        public static void DisposeResources()
        {
            MultiprocessLogger.Log("Disposing of resources");
            DirectoryInfo multiprocessAppDataDir = MultiprocessOrchestration.MultiprocessDirInfo;
            foreach (var f in multiprocessAppDataDir.GetFiles("*.json"))
            {
                MultiprocessLogger.Log($"Disposing of resource {f.FullName} and deleting file");
                ExecuteCommand($"--command destroy --input-path {f.FullName}");
                f.Delete();
            }
        }

        public static void FetchAllLogFiles()
        {
            DirectoryInfo multiprocessAppDataDir = MultiprocessOrchestration.MultiprocessDirInfo;
            MultiprocessLogger.Log($"FetchAllLogFiles: {multiprocessAppDataDir.FullName}");
            MultiprocessLogger.Log($"FetchAllLogFiles: {multiprocessAppDataDir.GetFiles("*.json").Length}");
            foreach (var f in multiprocessAppDataDir.GetFiles("*.json"))
            {
                MultiprocessLogger.Log($"Getting log files from {f.FullName}");
                ExecuteCommand($"--command GetMPLogFiles --input-path {f.FullName}", true);
            }
        }

        public static void KillMultiprocessTestPlayer(string pathToJson, bool waitForCompletion = true)
        {
            ExecuteCommand($" --command killmptplayer --input-path {pathToJson}", waitForCompletion);
        }

        // 1. Put built player file on remote machine
        // 2. Unzip the file on the remote machine
        // 3. Enable the firewall rules, etc. to allow to run
        public void Setup()
        {
            MultiprocessLogger.Log($"Setup start {Name}");
            ExecuteCommand(GenerateSetupMachineCommand(), true);
            MultiprocessLogger.Log($"Setup end {Name}");
        }

        public void CheckDirectoryStructure()
        {
            MultiprocessLogger.Log("Did the setup complete correctly?");
            Process p = null;
            string cmd = $" --command exec --input-path {PathToJson} --remote-command ";
            if (Image.Contains("win10"))
            {
                p = ExecuteCommand(cmd + "dir", true, true);
            }
            else
            {
                p = ExecuteCommand(cmd + "ls", true, true);
            }
            
        }

        public void Launch()
        {
            Process p = ExecuteCommand(GenerateLaunchCommand(MultiprocessOrchestration.GetLocalIPAddress()), false);
            MultiprocessLogger.Log($"Launch command ending with process exited state {p.HasExited}");
        }

        public void KillMptPlayer()
        {
            ExecuteCommand($" --command killmptplayer --input-path {PathToJson}", true);
        }

        public void PrintTaskListForMultiprocessTestPlayer()
        {
            try
            {
                string s = $" --command mpinfo " +
                    $"--input-path {PathToJson} ";
                ExecuteCommand(s, true, true, 20000);
            }
            catch (Exception e)
            {
                MultiprocessLogger.LogError("Error in PrintTaskList " + e.Message);
                MultiprocessLogger.LogError("Error in PrintTaskList " + e.StackTrace);
            }
        }

        /// <summary>
        /// Generalized for handling vast majority of uses cases for executing
        /// external process commands
        /// </summary>
        /// <param name="command"></param>
        /// <param name="waitForResult"></param>
        /// <param name="logStdOut"></param>
        /// <param name="timeToWait">timeout for any command is 30 seconds</param>
        /// <returns></returns>
        public static Process ExecuteCommand(string command, bool waitForResult = false, bool logStdOut = false, int timeToWait = 300000)
        {
            MultiprocessLogger.Log($"\"dotnet {PathToDll} {command}\"");

            var workerProcess = new Process();

            workerProcess.StartInfo.FileName = Path.Combine("dotnet");
            workerProcess.StartInfo.UseShellExecute = false;
            workerProcess.StartInfo.RedirectStandardError = true;
            workerProcess.StartInfo.RedirectStandardOutput = true;
            workerProcess.StartInfo.Arguments = $"{PathToDll} {command} ";
            try
            {
                MultiprocessLogger.Log($"Starting process : waitForResult is {waitForResult}");
                var newProcessStarted = workerProcess.Start();

                if (!newProcessStarted)
                {
                    throw new Exception("Failed to start worker process!");
                }
                else
                {
                    MultiprocessLogger.Log($"Started process : waitForResult is {waitForResult}");
                }
            }
            catch (Win32Exception e)
            {
                MultiprocessLogger.LogError($"Error starting bokken process, {e.Message} {e.Data} {e.ErrorCode}");
                throw;
            }

            if (waitForResult)
            {
                workerProcess.WaitForExit(timeToWait);
                if (logStdOut)
                {
                    string so = workerProcess.StandardOutput.ReadToEnd();
                    MultiprocessLogger.Log(so);
                }
            }
            else
            {
                s_ProcessList.Add(workerProcess);
            }
            MultiprocessLogger.Log($"Execute Command End");
            return workerProcess;
        }

        private string GenerateCreateCommand()
        {
            if (string.IsNullOrEmpty(PathToJson))
            {
                throw new Exception("PathToJson must not be null or empty");
            }

            string s = $" --command create --output-path {PathToJson} --type {Type} --image {Image} --flavor {Flavor} --name {Name}";
            return s;
        }

        private string GenerateSetupMachineCommand()
        {
            if (string.IsNullOrEmpty(PathToJson))
            {
                throw new Exception("PathToJson must not be null or empty");
            }

            string s = $" --command setupmachine --input-path {PathToJson}";
            MultiprocessLogger.Log(s);
            return s;
        }

        private string GenerateLaunchCommand(string ip)
        {
            if (string.IsNullOrEmpty(PathToJson))
            {
                throw new Exception("PathToJson must not be null or empty");
            }

            if (Image.Contains("win10"))
            {
                //TODO It is currently not possible to use environment variables in bokken commands and so explicit paths must be used for now
                // Open request to bokken team to enhance this
                // Possible workaround is to use a batch file or shell script, copy the script to the target, and then run the script
                //     which would have environment variables in it
                LogPath = @"C:\users\bokken\.multiprocess\" + $"logfile-mp-{DateTimeOffset.Now.ToUnixTimeSeconds()}.log";
                string s = $" --command exec " +
                    $"--input-path {PathToJson} " +
                    $"--remote-command \"com.unity.netcode.gameobjects\\testproject\\Builds\\MultiprocessTests\\MultiprocessTestPlayer.exe -isWorker -m client -logFile {LogPath} -popupwindow -screen-width 100 -screen-height 100 -p 3076 -ip {ip}\"";
                MultiprocessLogger.Log(s);
                return s;
            }
            else if (Type.Contains("osx"))
            {
                LogPath = Path.Combine(@"/Users/bokken/.multiprocess", $"logfile-mp-{DateTimeOffset.Now.ToUnixTimeSeconds()}.log");
                string s = $" --command exec " +
                    $"--input-path {PathToJson} " +
                    $"--remote-command \"./com.unity.netcode.gameobjects/testproject/Builds/MultiprocessTests/MultiprocessTestPlayer.app/Contents/MacOS/testproject -isWorker -m client -logFile {LogPath} -popupwindow -screen-width 100 -screen-height 100 -p 3076 -ip {ip}\"";
                MultiprocessLogger.Log(s);
                return s;
            }
            return "";
        }
    }

    public class BokkenMachineTestAttribute
    {
        public string Name { get; set; }
        public string SemanticTypeInfo { get; set; }

        public BokkenMachineTestAttribute(string name, string semanticinfo)
        {
            Name = name;
            SemanticTypeInfo = semanticinfo;
        }

        public BokkenMachineTestAttribute()
        {

        }
    }

}

