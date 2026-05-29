using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Netcode
{
    /// <summary>
    /// Helper class to handle the variations of FindObjectsByType.
    /// </summary>
    /// <remarks>
    /// It is intentional that we do not include the UnityEngine namespace in order to avoid
    /// over-complicated define wrapping between versions that do or don't support FindObjectsSortMode.
    /// </remarks>
    internal static class FindObjects
    {
        /// <summary>
        /// Replaces <see cref="Object.FindObjectsByType"/> to have one place where these changes are applied.
        /// </summary>
        /// <typeparam name="T">The type of object to find. Must be a reference type derived from <see cref="Object"/></typeparam>
        /// <param name="includeInactive">When true, inactive objects will be included.</param>
        /// <param name="orderByIdentifier">When true, the array returned will be sorted by identifier.</param>
        /// <returns>Results as an <see cref="Array"/> of type T</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ByType<T>(bool includeInactive = false, bool orderByIdentifier = false) where T : Object
        {
            var inactive = includeInactive ? UnityEngine.FindObjectsInactive.Include : UnityEngine.FindObjectsInactive.Exclude;
#if NGO_FINDOBJECTS_NOSORTING
            var results = Object.FindObjectsByType<T>(inactive);
#if !NGO_FINDOBJECTS_UNORDERED_IDS
            if (orderByIdentifier)
            {
                Array.Sort(results, (a, b) => a.GetEntityId().CompareTo(b.GetEntityId()));
            }
#endif
#else
            var results = Object.FindObjectsByType<T>(inactive, orderByIdentifier ? UnityEngine.FindObjectsSortMode.InstanceID : UnityEngine.FindObjectsSortMode.None);
#endif
            return results;
        }

        /// <summary>
        /// Returns an enumerator that enumerates over all the components of a given type in a scene.
        /// </summary>
        /// <param name="scene">The scene to use for searching</param>
        /// <param name="includeInactive">When true, inactive objects will be included.</param>
        /// <typeparam name="T">Type of <see cref="Component"/> to get from the scene</typeparam>
        /// <returns>a generator that yields successive NetworkObjects in the current scene</returns>
        public static IEnumerable<T> FromSceneByType<T>(Scene scene, bool includeInactive) where T : UnityEngine.Component
        {
            return new ObjectsInSceneEnumerator<T>(scene, includeInactive);
        }

        /// <summary>
        /// An Enumerator that enumerates over each component of type <see cref="T"/> in the given scene.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="Component"/> to get from the scene</typeparam>
        private struct ObjectsInSceneEnumerator<T> : IEnumerable<T>, IEnumerator<T> where T : UnityEngine.Component
        {
            private readonly UnityEngine.GameObject[] m_RootObjects;
            private int m_RootIndex;
            private T[] m_CurrentChildObjects;
            private int m_CurrentChildIndex;

            private readonly bool m_IncludeInactive;

            internal ObjectsInSceneEnumerator(Scene scene, bool includeInactive)
            {
                m_IncludeInactive = includeInactive;

                m_RootObjects = scene.GetRootGameObjects();
                m_RootIndex = 0;
                m_CurrentChildObjects = null;
                m_CurrentChildIndex = 0;
                Current = null;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                while (m_CurrentChildObjects == null && m_RootIndex < m_RootObjects.Length)
                {
                    m_CurrentChildObjects = m_RootObjects[m_RootIndex].GetComponentsInChildren<T>(m_IncludeInactive);
                    m_RootIndex++;

                    if (m_CurrentChildObjects.Length == 0)
                    {
                        m_CurrentChildObjects = null;
                    }
                }

                if (m_CurrentChildObjects != null && m_CurrentChildIndex < m_CurrentChildObjects.Length)
                {
                    Current = m_CurrentChildObjects[m_CurrentChildIndex];
                    m_CurrentChildIndex++;

                    if (m_CurrentChildIndex >= m_CurrentChildObjects.Length)
                    {
                        m_CurrentChildIndex = 0;
                        m_CurrentChildObjects = null;
                    }
                    return true;
                }

                Current = null;
                return false;
            }

            public void Reset()
            {
                m_RootIndex = 0;
                m_CurrentChildObjects = null;
                m_CurrentChildIndex = 0;
                Current = null;
            }

            object IEnumerator.Current => Current;

            public T Current { get; private set; }

            public IEnumerator<T> GetEnumerator() => this;

            IEnumerator IEnumerable.GetEnumerator() => this;
        }
    }
}
