using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MLAPI.MultiprocessRuntimeTests
{
    /// <summary>
    /// Have to implement our own pool here for compatibility with Unity 2020LTS
    /// This shouldn't be needed if we were supporting only 2021 (and its new Pool)
    /// </summary>
    public class GameObjectPool
    {
        private List<GameObject> m_GameObjectPool;
        private Stack<int> m_FreeIndexes;
        private Dictionary<GameObject, int> m_ReverseLookup = new Dictionary<GameObject, int>();

        public void Init(int originalCount, GameObject prefabToSpawn)
        {
            m_GameObjectPool = new List<GameObject>(originalCount);
            m_FreeIndexes = new Stack<int>(originalCount);
            for (int i = 0; i < originalCount; i++)
            {
                var go = Object.Instantiate(prefabToSpawn);
                go.SetActive(false);
                m_GameObjectPool.Add(go);
                m_FreeIndexes.Push(i);
                m_ReverseLookup[go] = i;
            }
        }

        public GameObject Get()
        {
            if (m_FreeIndexes.Count == 0)
            {
                throw new Exception("Pool full!");
            }
            var o = m_GameObjectPool[m_FreeIndexes.Pop()];
            o.SetActive(true);
            return o;
        }

        public void Release(GameObject toRelease)
        {
            int index = m_ReverseLookup[toRelease];
            m_FreeIndexes.Push(index);
            toRelease.SetActive(false);
        }
    }
}
