using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Spawning;

namespace MLAPI.MultiprocessRuntimeTests
{
    public class CustomPrefabSpawnerForPerformanceTests : INetworkPrefabInstanceHandler
    {
        private GameObjectPool m_ObjectPool;
        private Func<GameObject, NetworkObject> m_SetupSpawnedObject;

        public CustomPrefabSpawnerForPerformanceTests(GameObject prefabToSpawn, int maxObjectsToSpawn, Func<GameObject, NetworkObject> setupSpawnedObject)
        {
            m_ObjectPool = new GameObjectPool();
            m_ObjectPool.Init(maxObjectsToSpawn, prefabToSpawn);
            m_SetupSpawnedObject = setupSpawnedObject;
        }

        public NetworkObject HandleNetworkPrefabSpawn(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            var networkObject = m_ObjectPool.Get().GetComponent<NetworkObject>(); // todo this is expensive
            Transform netTransform = networkObject.transform;
            netTransform.position = position;
            netTransform.rotation = rotation;
            m_SetupSpawnedObject(networkObject.gameObject); // adds custom component on spawn
            return networkObject;
        }

        public void HandleNetworkPrefabDestroy(NetworkObject networkObject)
        {
            Transform netTransform = networkObject.transform;
            netTransform.position = Vector3.zero;
            netTransform.rotation = Quaternion.identity;
            m_ObjectPool.Release(networkObject.gameObject);
        }
    }
}
