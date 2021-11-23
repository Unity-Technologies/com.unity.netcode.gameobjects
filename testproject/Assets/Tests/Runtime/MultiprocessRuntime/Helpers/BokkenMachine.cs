using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;

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

        public static BokkenMachine GetDefaultMac(string name)
        {
            var rv = new BokkenMachine();
            rv.Type = "Unity::VM::osx";
            rv.Image = "unity-ci/macos-10.15-dotnetcore:latest";
            rv.Flavor = "b1.large";
            rv.Name = name;
            return rv;
        }

        public static BokkenMachine GetDefaultWin(string name)
        {
            var rv = new BokkenMachine();
            rv.Type = "Unity::VM";
            rv.Image = "package-ci/win10:stable";
            rv.Flavor = "b1.large";
            rv.Name = name;
            return rv;
        }

        public static BokkenMachine Parse(string shortcut)
        {
            BokkenMachine rv = null;
            string[] parts = shortcut.Split(":");
            string name = parts[1];
            string type = parts[0];
            if (type.Equals("default-mac"))
            {
                rv = GetDefaultMac(name);
            }
            else if (type.Equals("default-win"))
            {
                rv = GetDefaultWin(name);
            }            
            
            return rv;
        }
        public BokkenMachine()
        {
            
            
        }

        public void Provision()
        {
            ExecuteCommand(GenerateCreateCommand(), true, 5000);
        }

        public static void DisposeResources()
        {
            MultiprocessLogger.Log("Disposing of resources");
            DirectoryInfo mpDir = MultiprocessOrchestration.MultiprocessDirInfo;
            foreach (var f in mpDir.GetFiles("*.json"))
            {
                MultiprocessLogger.Log($"Disposing of resource {f.FullName} and deleting file");
                ExecuteCommand($"--command destroy --input-path {f.FullName}");
                f.Delete();                
            }            
        }

        public static void FetchAllLogFiles()
        {
            DirectoryInfo mpDir = MultiprocessOrchestration.MultiprocessDirInfo;
            MultiprocessLogger.Log($"FetchAllLogFiles: {mpDir.FullName}");
            MultiprocessLogger.Log($"FetchAllLogFiles: {mpDir.GetFiles("*.json").Length}");
            foreach (var f in mpDir.GetFiles("*.json"))
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
            ExecuteCommand(GenerateSetupMachineCommand(), true);
        }
        
        public void Launch()
        {
            ExecuteCommand(GenerateLaunchCommand(MultiprocessOrchestration.GetLocalIPAddress()), false);
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
                ExecuteCommand(s, true, 20000);
            }
            catch (Exception e)
            {
                MultiprocessLogger.LogError("Error in PrintTaskList " + e.Message);
                MultiprocessLogger.LogError("Error in PrintTaskList " + e.StackTrace);
            }
        }

        public void GetMPLog()
        {
            ExecuteCommand($"");
        }

        public static Process ExecuteCommand(string command, bool waitForResult = false, int timeToWait = 300000)
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
                string so = workerProcess.StandardOutput.ReadToEnd();
                MultiprocessLogger.Log(so);
                // string se = workerProcess.StandardError.ReadToEnd();
                // MultiprocessLogger.LogError(se);
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

                LogPath = @"C:\users\bokken\.multiprocess\" +$"logfile-mp-{DateTimeOffset.Now.ToUnixTimeSeconds()}.log";
                string s = $" --command exec " +
                    $"--input-path {PathToJson} " +
                    $"--remote-command \"com.unity.netcode.gameobjects\\testproject\\Builds\\MultiprocessTests\\MultiprocessTestPlayer.exe -isWorker -logFile {LogPath} -popupwindow -screen-width 100 -screen-height 100 -p 3076 -ip {ip}\"";
                return s;
            }
            else if (Type.Contains("osx"))
            {
                LogPath = Path.Combine(@"/Users/bokken/.multiprocess", $"logfile-mp-{DateTimeOffset.Now.ToUnixTimeSeconds()}.log");
                string s = $" --command exec " +
                    $"--input-path {PathToJson} " +
                    $"--remote-command \"./com.unity.netcode.gameobjects/testproject/Builds/MultiprocessTests/MultiprocessTestPlayer.app/Contents/MacOS/testproject -isWorker -logFile {LogPath} -popupwindow -screen-width 100 -screen-height 100 -p 3076 -ip {ip}\"";
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

