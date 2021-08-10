using System.Collections.Generic;

namespace Unity.Netcode
{
    internal class ProfilingDataStore
    {
        private readonly Dictionary<string, int> m_Dictionary = new Dictionary<string, int>();

        public void Add(string fieldName, int value)
        {
            m_Dictionary.Add(fieldName, value);
        }

        public void Increment(string fieldName, int count = 1)
        {
            if (m_Dictionary.TryGetValue(fieldName, out int value))
            {
                m_Dictionary[fieldName] = value + count;
            }
            else
            {
                m_Dictionary[fieldName] = count;
            }
        }

        public bool HasData(string fieldName)
        {
            return m_Dictionary.ContainsKey(fieldName);
        }

        public int GetData(string fieldName)
        {
            return m_Dictionary.TryGetValue(fieldName, out int value) ? value : 0;
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
