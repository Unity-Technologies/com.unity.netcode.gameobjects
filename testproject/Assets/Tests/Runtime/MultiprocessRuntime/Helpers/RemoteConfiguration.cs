using System;
using UnityEngine;
namespace Unity.Netcode.MultiprocessRuntimeTests
{
    [Serializable]
    public class RemoteConfiguration
    {
        public string IpAddressOfHost;
        public string OperationMode;
        public string LogFile;
        public string JobId;
        public string Port;
    }
}