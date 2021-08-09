using System.Collections.Generic;

namespace Unity.Netcode
{
    internal static class ProfilerStatManager
    {
        public static List<ProfilerStat> AllStats = new List<ProfilerStat>();

        public static readonly ProfilerIncStat Connections = new ProfilerIncStat("Connections");
        public static readonly ProfilerStat BytesRcvd = new ProfilerStat("Bytes Rcvd");
        public static readonly ProfilerStat BytesSent = new ProfilerStat("Bytes Sent");
        public static readonly ProfilerStat RcvTickRate = new ProfilerStat("Rcv Tick Rate");
        public static readonly ProfilerStat NetworkVarsRcvd = new ProfilerStat("Network Vars Rcvd");
        public static readonly ProfilerStat NamedMessage = new ProfilerStat("Named Message");
        public static readonly ProfilerStat UnnamedMessage = new ProfilerStat("UnNamed Message");

        public static readonly ProfilerStat RpcsRcvd = new ProfilerStat("RPCs Rcvd");
        public static readonly ProfilerStat RpcsSent = new ProfilerStat("RPCs Sent");
        public static readonly ProfilerIncStat NetTranforms = new ProfilerIncStat("NetTransforms");


        public static void Add(ProfilerStat s)
        {
            AllStats.Add(s);
        }
    }
}
