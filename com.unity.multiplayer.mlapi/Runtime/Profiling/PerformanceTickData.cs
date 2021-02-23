using System.Collections.Generic;

namespace MLAPI.Profiling
{
    public class PerformanceTickData
    {
        public int tickID;

        public readonly Dictionary<string, int> tickData = new Dictionary<string, int>();

        public void Increment(string fieldName, int count = 1)
        {
            if (!tickData.ContainsKey(fieldName))
            {
                tickData[fieldName] = 0;
            }

            tickData[fieldName] += count;
        }

        public int GetData(string fieldName)
        {
            return tickData.ContainsKey(fieldName) ? tickData[fieldName] : 0;
        }
    }
}
