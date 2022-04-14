using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using NUnit.Framework;

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

        public static List<Process> ProcessList = new List<Process>();

        static BokkenMachine()
        {
            s_FileInfo = new FileInfo(Path.Combine(MultiprocessOrchestration.MultiprocessDirInfo.FullName, "rootdir"));
            if (s_FileInfo.Exists)
            {
                s_Rootdir = (File.ReadAllText(s_FileInfo.FullName)).Trim();
            }
            PathToDll = Path.Combine(s_Rootdir, "BokkenForNetcode", "ProvisionBokkenMachines", "bin", "Debug", "netcoreapp3.1", "ProvisionBokkenMachines.dll");
        }

        public bool IsValid()
        {
            bool isvalid = true;
            if (Name == null)
            {
                isvalid = false;
                MultiprocessLogger.Log("Machine name is null");
            }

            if (Image == null)
            {
                isvalid = false;
                MultiprocessLogger.Log("Machine Image is null");
            }

            if (Type == null)
            {
                isvalid = false;
                MultiprocessLogger.Log("Machine Type is null");
            }
            return isvalid;
        }

        public static void DumpProcessList()
        {
            foreach (Process process in ProcessList)
            {
                MultiprocessLogger.Log($"Process info: id: {process.Id} HasExited: {process.HasExited}");
                if (!process.HasExited)
                {
                    MultiprocessLogger.Log($"Process arguments: {process.StartInfo.Arguments}");
                }
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

        //GetDefaultLinux
        public static BokkenMachine GetDefaultLinux(string name)
        {
            var defaultLinux = new BokkenMachine();
            defaultLinux.Type = "Unity::VM";
            defaultLinux.Image = "package-ci/ubuntu:stable";
            defaultLinux.Flavor = "b1.large";
            defaultLinux.Name = name;
            return defaultLinux;
        }

        public static BokkenMachine GetDefaultAndroid(string name)
        {
            var defaultAndroid = new BokkenMachine();
            defaultAndroid.Type = "Unity::mobile::shield";
            defaultAndroid.Image = "multiplayer/android-execution-r19-dotnet:latest";
            defaultAndroid.Flavor = "b1.large";
            defaultAndroid.Name = name;
            return defaultAndroid;
        }

        public static BokkenMachine GetDefaultXboxOne(string name)
        {
            var defaultXboxOne = new BokkenMachine();
            defaultXboxOne.Type = "Unity::console::xbox";
            defaultXboxOne.Image = "gamecore/gamecore-ci:latest";
            defaultXboxOne.Flavor = "b1.xlarge";
            defaultXboxOne.Name = name;
            return defaultXboxOne;
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
            else if (type.Equals("default-linux"))
            {
                bokkenMachine = GetDefaultLinux(name);
            }
            else if (type.Equals("default-android"))
            {
                bokkenMachine = GetDefaultAndroid(name);
            }
            else if (type.Equals("default-xboxone"))
            {
                bokkenMachine = GetDefaultXboxOne(name);
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
            ExecuteCommand($" --command killmptplayer --input-path {pathToJson}", waitForCompletion, true);
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

        public static void LogProcessListStatus()
        {
            int counter = 0;
            var deletionList = new List<Process>();
            foreach (var process in ProcessList)
            {
                counter++;
                if (!process.HasExited)
                {
                    MultiprocessLogger.Log($"BokkenMachine process list item {counter} of {ProcessList.Count}");
                }
                else
                {
                    deletionList.Add(process);
                    MultiprocessLogger.Log($" Deletion list for BokkenMachine process is now {deletionList.Count}");
                }
            }

            foreach (var processToDelete in deletionList)
            {
                ProcessList.Remove(processToDelete);
            }
        }

        public void Launch()
        {
            MultiprocessLogger.Log($"Launch command - BokkenMachine process status: {ProcessList.Count}");
            MultiprocessLogger.Log($"Launch command - {Name} {Type} {Image}");
            Process p = ExecuteCommand(GenerateLaunchCommand(MultiprocessOrchestration.GetLocalIPAddress()), false);
            MultiprocessLogger.Log($"Launch command ending with process exited state {p.HasExited}");
            if (!ProcessList.Contains(p))
            {
                ProcessList.Add(p);
            }
            MultiprocessLogger.Log($"Launch command - BokkenMachine process status: {ProcessList.Count}");
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
                ProcessList.Add(workerProcess);
            }
            MultiprocessLogger.Log($"Execute Command: {command} End");
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

            string testName = TestContext.CurrentContext.Test.Name;
            testName = testName.Replace('(', '_').Replace(')', '_');

            if (Name.Contains("android"))
            {
                LogPath = "-";
                string s = $" --command exec " +
                    $"--input-path {PathToJson} " +
                    "--remote-command " +
                    "\"c:\\Users\\bokken\\android-sdk_auto\\platform-tools\\adb.exe shell am start -n com.UnityTechnologies.testproject/com.unity3d.player.UnityPlayerActivity\"";
                return s;
            }
            else if (Image.Contains("win10"))
            {
                //TODO It is currently not possible to use environment variables in bokken commands and so explicit paths must be used for now
                // Open request to bokken team to enhance this
                // Possible workaround is to use a batch file or shell script, copy the script to the target, and then run the script
                //     which would have environment variables in it
                LogPath = @"C:\users\bokken\.multiprocess\" + $"logfile-mp-{DateTimeOffset.Now.ToUnixTimeSeconds()}.log";
                string s = $" --command exec " +
                    $"--input-path {PathToJson} " +
                    $"--remote-command \"com.unity.netcode.gameobjects\\testproject\\Builds\\MultiprocessTests\\MultiprocessTestPlayer.exe -isWorker -m client -logFile {LogPath} -jobid {MultiprocessLogHandler.JobId} -testname {testName} -popupwindow -screen-width 100 -screen-height 100 -p 3076 -ip {ip}\"";
                MultiprocessLogger.Log(s);
                return s;
            }
            else if (Type.Contains("osx"))
            {
                LogPath = Path.Combine(@"/Users/bokken/.multiprocess", $"logfile-mp-{DateTimeOffset.Now.ToUnixTimeSeconds()}.log");
                string s = $" --command exec " +
                    $"--input-path {PathToJson} " +
                    $"--remote-command \"./com.unity.netcode.gameobjects/testproject/Builds/MultiprocessTests/MultiprocessTestPlayer.app/Contents/MacOS/testproject -isWorker -m client -logFile {LogPath} -jobid {MultiprocessLogHandler.JobId} -testname {testName} -popupwindow -screen-width 100 -screen-height 100 -p 3076 -ip {ip}\"";
                MultiprocessLogger.Log(s);
                return s;
            }
            else if (Image.Contains("ubuntu"))
            {
                LogPath = Path.Combine(@"/home/bokken/.multiprocess", $"logfile-mp-{DateTimeOffset.Now.ToUnixTimeSeconds()}.log");
                string s = $" --command exec " +
                    $"--input-path {PathToJson} " +
                    $"--remote-command \"./com.unity.netcode.gameobjects/testproject/Builds/MultiprocessTests/MultiprocessTestPlayer -isWorker -m client -logFile {LogPath} -jobid {MultiprocessLogHandler.JobId} -testname {testName} -nographics -batchmode -p 3076 -ip {ip}\"";
                MultiprocessLogger.Log(s);
                return s;
            }
            else
            {
                throw new NotImplementedException("Unknown Image or Type, no launch command available");
            }
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

