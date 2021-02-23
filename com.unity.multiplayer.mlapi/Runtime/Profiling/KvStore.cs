using System.Collections.Generic;

namespace MLAPI.Profiling
{
    public class KvStore
    {
        readonly Dictionary<string, int> m_Dictionary = new Dictionary<string, int>();

        public void Add(string entryKey, int entryValue)
        {
            m_Dictionary.Add(entryKey, entryValue);
        }

        public void Increment(string fieldName, int count = 1)
        {
            if (!m_Dictionary.ContainsKey(fieldName))
            {
                m_Dictionary[fieldName] = 0;
            }

            m_Dictionary[fieldName] += count;
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
