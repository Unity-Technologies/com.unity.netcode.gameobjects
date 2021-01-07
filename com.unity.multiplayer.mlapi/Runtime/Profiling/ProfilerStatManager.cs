using System.Collections.Generic;

namespace MLAPI.Profiling
{
    public static class ProfilerStatManager
    {
        public static List<ProfilerStat> allStats = new List<ProfilerStat>();
        public static ProfilerIncStat connections = new ProfilerIncStat("Connections");
        public static ProfilerStat bytesRcvd = new ProfilerStat("Bytes Rcvd");
        public static ProfilerStat bytesSent = new ProfilerStat("Bytes Sent");
        public static ProfilerStat rcvTickRate = new ProfilerStat("Rcv Tick Rate");
        public static ProfilerStat networkVarsRcvd = new ProfilerStat("Network Vars Rcvd");
        public static ProfilerStat namedMessage = new ProfilerStat("Named Message");
        public static ProfilerStat unnamedMessage = new ProfilerStat("UnNamed Message");

        public static ProfilerStat rpcsRcvd = new ProfilerStat("RPCs Rcvd");
        public static ProfilerStat rpcsSent = new ProfilerStat("RPCs Sent");
        public static ProfilerStat rpcBatchesRcvd = new ProfilerStat("RPC Batches Rcvd");
        public static ProfilerStat rpcBatchesSent = new ProfilerStat("RPC Batches Sent");
        public static ProfilerStat rpcsQueueProc = new ProfilerStat("RPCS-Processed");
        public static ProfilerStat rpcInQueueSize = new ProfilerStat("InQFrameSize");
        public static ProfilerStat rpcOutQueueSize = new ProfilerStat("OutQFrameSize");
        public static ProfilerIncStat NetTranforms = new ProfilerIncStat("NetTransforms");


        public static void Add(ProfilerStat s)
        {
            allStats.Add(s);
        }
    }
}
