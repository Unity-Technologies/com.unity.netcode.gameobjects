using System.Collections.Generic;

namespace MLAPI.Profiling
{
    public class ProfilingDataStore
    {
        private readonly Dictionary<string, int> m_Dictionary = new Dictionary<string, int>();

        public void Add(string fieldName, int value)
        {
            m_Dictionary.Add(fieldName, value);
        }

        public void Increment(string fieldName, int count = 1)
        {
            m_Dictionary[fieldName] = m_Dictionary.ContainsKey(fieldName) ? m_Dictionary[fieldName] + count : count;
        }

        public bool HasData(string fieldName)
        {
            return m_Dictionary.ContainsKey(fieldName);
        }

        public int GetData(string fieldName)
        {
            return m_Dictionary.ContainsKey(fieldName) ? m_Dictionary[fieldName] : 0;
        }

        public void Clear()
        {
            m_Dictionary.Clear();
        }

        public IReadOnlyDictionary<string, int> GetReadonly()
        {
            return m_Dictionary;
        }
    }
}