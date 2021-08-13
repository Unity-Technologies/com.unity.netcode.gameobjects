using System;
using UnityEngine;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class CustomPrefabSpawnerForPerformanceTests<T> : INetworkPrefabInstanceHandler, IDisposable where T : NetworkBehaviour
    {
        private GameObjectPool<T> m_ObjectPool;
        private Action<T> m_SetupSpawnedObject;
        private Action<T> m_OnRelease;

        public CustomPrefabSpawnerForPerformanceTests(T prefabToSpawn, int maxObjectsToSpawn, Action<T> setupSpawnedObject, Action<T> onRelease)
        {
            m_ObjectPool = new GameObjectPool<T>();
            m_ObjectPool.Initialize(maxObjectsToSpawn, prefabToSpawn);
            m_SetupSpawnedObject = setupSpawnedObject;
            m_OnRelease = onRelease;
        }

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            var netBehaviour = m_ObjectPool.Get();
            var networkObject = netBehaviour.NetworkObject;
            Transform netTransform = networkObject.transform;
            netTransform.position = position;
            netTransform.rotation = rotation;
            m_SetupSpawnedObject(netBehaviour);
            return networkObject;
        }

        public void Destroy(NetworkObject networkObject)
        {
            var behaviour = networkObject.gameObject.GetComponent<T>(); // todo expensive, only used in teardown for now, should optimize eventually
            m_OnRelease(behaviour);
            Transform netTransform = networkObject.transform;
            netTransform.position = Vector3.zero;
            netTransform.rotation = Quaternion.identity;
            m_ObjectPool.Release(behaviour);
        }

        public void Dispose()
        {
            m_ObjectPool.Dispose();
        }
    }
}
