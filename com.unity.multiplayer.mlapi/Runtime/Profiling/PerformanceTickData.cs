using System.Collections.Generic;
using System.Linq;

namespace MLAPI.Profiling
{
    public class PerformanceTickData
    {
        public int TickId;

        private readonly ProfilingDataStore m_TickData = new ProfilingDataStore();

        public void Increment(string fieldName, int count = 1)
        {
            m_TickData.Increment(fieldName, count);
        }

        public void AddNonDuplicateData(IReadOnlyDictionary<string, int> transportProfilerData)
        {
            foreach (var entry in transportProfilerData)
            {
                if (m_TickData.HasData(entry.Key))
                {
                    continue;
                }
                m_TickData.Add(entry.Key, entry.Value);
            }
        }

        public int GetData(string fieldName)
        {
            return m_TickData.GetData(fieldName);
        }

        public bool HasData(string fieldName)
        {
            return m_TickData.HasData(fieldName);
        }

        public void Reset()
        {
            m_TickData.Clear();
        }
    }
}
