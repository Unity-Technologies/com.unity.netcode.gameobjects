using System;
using System.Collections.Generic;

namespace MLAPI.Profiling
{
    internal static class PerformanceDataManager
    {
        private static PerformanceTickData s_ProfilerData;
        private static int s_TickId;

        internal static void BeginNewTick()
        {
            s_TickId = Math.Max(s_TickId, 0);
            s_ProfilerData = new PerformanceTickData
            {
                TickId = s_TickId++,
            };
        }

        internal static void Increment(string fieldName, int count = 1)
        {
            s_ProfilerData?.Increment(fieldName, count);
        }

        internal static void AddTransportData(IReadOnlyDictionary<string, int> transportProfilerData)
        {
            s_ProfilerData?.AddNonDuplicateData(transportProfilerData);
        }

        internal static PerformanceTickData GetData()
        {
            return s_ProfilerData;
        }
    }
}