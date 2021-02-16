using System.Collections.Generic;

namespace MLAPI.Profiling
{
    public class PerformanceTickData
    {
        public int tickID;

        public readonly Dictionary<string, int> tickData = new Dictionary<string, int>();

        public void Increment(ProfilerConstants fieldName, int count = 1)
        {
            string fieldStr = fieldName.ToString();
            if (!tickData.ContainsKey(fieldStr))
            {
                tickData[fieldStr] = 0;
            }

            tickData[fieldStr] += count;
        }

        public int GetData(ProfilerConstants fieldName)
        {
            string fieldString = fieldName.ToString();
            return tickData.ContainsKey(fieldString) ? tickData[fieldString] : 0;
        }
    }
}
