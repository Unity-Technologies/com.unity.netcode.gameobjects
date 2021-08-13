using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    /// <summary>
    /// Have to implement our own pool here for compatibility with Unity 2020LTS
    /// This shouldn't be needed if we were supporting only 2021 (and its new Pool)
    /// </summary>
    public class GameObjectPool<T> : IDisposable where T : NetworkBehaviour
    {
        private List<T> m_AllGameObject;
        private Stack<int> m_FreeIndexes;
        private Dictionary<T, int> m_ReverseLookup = new Dictionary<T, int>();

        public void Initialize(int originalCount, T prefabToSpawn)
        {
            m_AllGameObject = new List<T>(originalCount);
            m_FreeIndexes = new Stack<int>(originalCount);
            for (int i = 0; i < originalCount; i++)
            {
                var go = Object.Instantiate(prefabToSpawn);
                go.gameObject.SetActive(false);
                m_AllGameObject.Add(go);
                m_FreeIndexes.Push(i);
                m_ReverseLookup[go] = i;
            }
        }

        public void Dispose()
        {
            foreach (var gameObject in m_AllGameObject)
            {
                Object.Destroy(gameObject);
            }
            m_AllGameObject = null;
            m_FreeIndexes = null;
            m_ReverseLookup = null;
        }

        public T Get()
        {
            if (m_FreeIndexes.Count == 0)
            {
                throw new Exception("Pool full!");
            }
            var o = m_AllGameObject[m_FreeIndexes.Pop()];
            o.gameObject.SetActive(true);
            return o;
        }

        public void Release(T toRelease)
        {
            int index = m_ReverseLookup[toRelease];
            m_FreeIndexes.Push(index);
            toRelease.gameObject.SetActive(false);
        }
    }
}
