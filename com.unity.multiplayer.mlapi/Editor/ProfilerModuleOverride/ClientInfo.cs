using System.Runtime.InteropServices;
using MLAPI.Profiling;

namespace ProfilerModuleOverride
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ClientInfo
    {
        public fixed byte targetName[320];
        public fixed byte channelName[320];
        public TickType type;
        public uint bytesSent;
        public fixed byte messageName[320];
    }
}
