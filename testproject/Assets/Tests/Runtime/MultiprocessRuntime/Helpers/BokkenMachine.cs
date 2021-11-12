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

        public Dictionary<string, BokkenMachine> BokkenMachines;

        private FileInfo m_FileInfo;
        private string m_Rootdir;
        private string m_PathToDll;
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
            m_FileInfo = new FileInfo(Path.Combine(MultiprocessOrchestration.MultiprocessDirInfo.FullName, "rootdir"));
            m_Rootdir = (File.ReadAllText(m_FileInfo.FullName)).Trim();
            m_PathToDll = Path.Combine(m_Rootdir, "BokkenForNetcode", "ProvisionBokkenMachines", "bin", "Debug", "netcoreapp3.1", "ProvisionBokkenMachines.dll");
        }

        public void Provision()
        {
            ExecuteCommand(GenerateCreateCommand(), true, 5000);
        }

        // 1. Put built player file on remote machine
        // 2. Unzip the file on the remote machine
        // 3. Enable the firewall rules, etc. to allow to run
        public void Setup()
        {
            ExecuteCommand(GenerateSetupMachineCommand(), true);
        }

        public void Launch(string ip)
        {
            ExecuteCommand(GenerateLaunchCommand(ip));
        }

        public void ExecuteCommand(string command, bool waitForResult = false, int timeToWait = 300000)
        {
            
            MultiprocessLogger.Log($"Execute Command {command}");
            
            var workerProcess = new Process();

            workerProcess.StartInfo.FileName = Path.Combine("dotnet");
            workerProcess.StartInfo.UseShellExecute = false;
            workerProcess.StartInfo.RedirectStandardError = true;
            workerProcess.StartInfo.RedirectStandardOutput = true;
            workerProcess.StartInfo.Arguments = $"{m_PathToDll} {command} ";
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

            if (waitForResult)
            {
                MultiprocessLogger.Log("Starting to wait");
                workerProcess.WaitForExit(timeToWait);
                MultiprocessLogger.Log("Done waiting");
                string so = workerProcess.StandardOutput.ReadToEnd();
                MultiprocessLogger.Log(so);
            }
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
            string logPath = Path.Combine(@"C:\users\bokken\.multiprocess", $"logfile-mp-{DateTimeOffset.Now.ToUnixTimeSeconds()}.log");
            string s = $" --command exec " +
                $"--input-path {PathToJson} "+
                $"--remote-command \"com.unity.netcode.gameobjects\\testproject\\Builds\\MultiprocessTests\\MultiprocessTestPlayer.exe -isWorker -logFile {logPath} -popupwindow -screen-width 100 -screen-height 100 -p 3076 -ip {ip}\"";
            return s;
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

