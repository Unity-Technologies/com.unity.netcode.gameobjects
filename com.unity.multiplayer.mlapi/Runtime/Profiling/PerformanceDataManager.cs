using System;
using System.Collections.Generic;
using System.Linq;

namespace MLAPI.Profiling
{
    static class PerformanceDataManager
    {
        static PerformanceTickData s_ProfilerData;
        static int s_TickID;

        internal static void BeginNewTick()
        {
            s_TickID = Math.Max(s_TickID, 0);
            s_ProfilerData = new PerformanceTickData
            {
                tickID = s_TickID++,
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
