using System.Collections.Generic;
using System.Linq;

namespace MLAPI.Profiling
{
    public static class PerformanceDataManager
    {
        static PerformanceTickData s_ProfilerData;
        static int s_TickID;

        public static void BeginNewTick()
        {
            s_ProfilerData = new PerformanceTickData
            {
                tickID = s_TickID++,
            };
        }

        public static void Increment(ProfilerConstants fieldName, int count = 1)
        {
            s_ProfilerData.Increment(fieldName, count);
        }

        public static void AddTransportData(Dictionary<string, int> transportProfilerData)
        {
            IEnumerable<KeyValuePair<string, int>> nonDuplicates = transportProfilerData.Where(entry => !s_ProfilerData.tickData.ContainsKey(entry.Key));
            foreach (KeyValuePair<string, int> entry in nonDuplicates)
            {
                s_ProfilerData.tickData.Add(entry.Key, entry.Value);
            }
        }

        public static PerformanceTickData GetData()
        {
            return s_ProfilerData;
        }
    }
}
