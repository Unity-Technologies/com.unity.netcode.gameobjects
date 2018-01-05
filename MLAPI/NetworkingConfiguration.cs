using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace MLAPI
{
    public class NetworkingConfiguration
    {
        public ushort ProtocolVersion = 0;
        public Dictionary<string, QosType> Channels = new Dictionary<string, QosType>();
        public List<string> MessageTypes = new List<string>();
        public int MessageBufferSize = 65536;
        public int MaxMessagesPerFrame = 150;
        public int MaxConnections = 100;
        public int Port = 7777;
        public int ClientConnectionBufferTimeout = 10;
        public bool ConnectionApproval = false;
        public Action<byte[], int, Action<int, bool>> ConnectionApprovalCallback;
        public byte[] ConnectionData;
    }
}
