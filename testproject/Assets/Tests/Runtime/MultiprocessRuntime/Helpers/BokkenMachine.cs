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
            rv.Name = name;
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
            ExecuteCommand(GenerateCreateCommand());
        }

        public void Setup()
        {
            // 1. Put built player file on remote machine

            // 2. Unzip the file on the remote machine

            // 3. Enable the firewall rules, etc. to allow to run
        }

        public void ExecuteCommand(string command)
        {
            
            MultiprocessLogger.Log("StartWorkerOnRemoteNodes");            
            
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
    }
}

