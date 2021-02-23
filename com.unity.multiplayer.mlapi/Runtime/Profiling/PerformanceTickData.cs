using System.Collections.Generic;
using System.Linq;

namespace MLAPI.Profiling
{
    public class PerformanceTickData
    {
        public int tickID;

        readonly ProfilingDataStore m_TickData = new ProfilingDataStore();

        public void Increment(string fieldName, int count = 1)
        {
            m_TickData.Increment(fieldName, count);
        }

        public void AddNonDuplicateData(IReadOnlyDictionary<string, int> transportProfilerData)
        {
            IEnumerable<KeyValuePair<string, int>> nonDuplicates = transportProfilerData.Where(entry => !m_TickData.HasData(entry.Key));
            foreach (KeyValuePair<string, int> entry in nonDuplicates)
            {
                m_TickData.Add(entry.Key, entry.Value);
            }
        }

        public int GetData(string fieldName)
        {
            return m_TickData.GetData(fieldName);
        }
    }
}
