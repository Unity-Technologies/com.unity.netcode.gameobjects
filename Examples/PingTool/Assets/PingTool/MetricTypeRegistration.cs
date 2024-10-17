#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools.NetStats;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.Netcode.Examples.PingTool
{
    [Preserve]
    internal static class NetStatsTypeRegistration
    {
        internal static void Run()
        {
            MetricIdTypeLibrary.RegisterType<PingTool.PingToolMetrics>();
        }
    }
}
#endif
