using System.Collections.Generic;

namespace MLAPI.Profiling
{
    public static class ProfilerStatManager
    {
        public static List<ProfilerStat> AllStats = new List<ProfilerStat>();
        public static ProfilerIncStat Connections = new ProfilerIncStat("Connections");
        public static ProfilerStat BytesRcvd = new ProfilerStat("Bytes Rcvd");
        public static ProfilerStat BytesSent = new ProfilerStat("Bytes Sent");
        public static ProfilerStat RcvTickRate = new ProfilerStat("Rcv Tick Rate");
        public static ProfilerStat NetworkVarsRcvd = new ProfilerStat("Network Vars Rcvd");
        public static ProfilerStat NamedMessage = new ProfilerStat("Named Message");
        public static ProfilerStat UnnamedMessage = new ProfilerStat("UnNamed Message");

        public static ProfilerStat RpcsRcvd = new ProfilerStat("RPCs Rcvd");
        public static ProfilerStat RpcsSent = new ProfilerStat("RPCs Sent");
        public static ProfilerStat RpcBatchesRcvd = new ProfilerStat("RPC Batches Rcvd");
        public static ProfilerStat RpcBatchesSent = new ProfilerStat("RPC Batches Sent");
        public static ProfilerStat RpcsQueueProc = new ProfilerStat("RPCS-Processed");
        public static ProfilerStat RpcInQueueSize = new ProfilerStat("InQFrameSize");
        public static ProfilerStat RpcOutQueueSize = new ProfilerStat("OutQFrameSize");
        public static ProfilerIncStat NetTranforms = new ProfilerIncStat("NetTransforms");


        public static void Add(ProfilerStat s)
        {
            AllStats.Add(s);
        }
    }
}