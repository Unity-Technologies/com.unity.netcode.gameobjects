using MLAPI.Data;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Core
{
    /// <summary>
    /// Main class for managing network pools
    /// </summary>
    public static class NetworkPoolManager
    {
        internal static Dictionary<ushort, NetworkPool> Pools;
        private static ushort PoolIndex = 0;
        internal static Dictionary<string, ushort> PoolNamesToIndexes;

        /// <summary>
        /// Creates a networked object pool. Can only be called from the server
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="spawnablePrefabIndex">The index of the prefab to use in the spawnablePrefabs array</param>
        /// <param name="size">The amount of objects in the pool</param>
        public static void CreatePool(string poolName, int spawnablePrefabIndex, uint size = 16)
        {
            if(!NetworkingManager.singleton.isServer)
            {
                Debug.LogWarning("MLAPI: Pools can only be created on the server");
                return;
            }
            NetworkPool pool = new NetworkPool(spawnablePrefabIndex, size, PoolIndex);
            PoolNamesToIndexes.Add(poolName, PoolIndex);
            PoolIndex++;
        }

        /// <summary>
        /// This destroys an object pool and all of it's objects. Can only be called from the server
        /// </summary>
        /// <param name="poolName">The name of the pool</param>
        public static void DestroyPool(string poolName)
        {
            if (!NetworkingManager.singleton.isServer)
            {
                Debug.LogWarning("MLAPI: Pools can only be destroyed on the server");
                return;
            }
            for (int i = 0; i < Pools[PoolNamesToIndexes[poolName]].objects.Length; i++)
            {
                MonoBehaviour.Destroy(Pools[PoolNamesToIndexes[poolName]].objects[i]);
            }
            Pools.Remove(PoolNamesToIndexes[poolName]);
        }

        /// <summary>
        /// Spawns a object from the pool at a given position and rotation. Can only be called from server.
        /// </summary>
        /// <param name="poolName">The name of the pool</param>
        /// <param name="position">The position to spawn the object at</param>
        /// <param name="rotation">The rotation to spawn the object at</param>
        /// <returns></returns>
        public static GameObject SpawnPoolObject(string poolName, Vector3 position, Quaternion rotation)
        {
            if (!NetworkingManager.singleton.isServer)
            {
                Debug.LogWarning("MLAPI: Object spawning can only occur on server");
                return null;
            }
            GameObject go = Pools[PoolNamesToIndexes[poolName]].SpawnObject(position, rotation);
            using (BitWriter writer = new BitWriter())
            {
                writer.WriteUInt(go.GetComponent<NetworkedObject>().NetworkId);

                writer.WriteFloat(position.x);
                writer.WriteFloat(position.y);
                writer.WriteFloat(position.z);

                writer.WriteFloat(rotation.eulerAngles.x);
                writer.WriteFloat(rotation.eulerAngles.y);
                writer.WriteFloat(rotation.eulerAngles.z);

                InternalMessageHandler.Send("MLAPI_SPAWN_POOL_OBJECT", "MLAPI_INTERNAL", writer, null);
            }
            return go;
        }

        /// <summary>
        /// Destroys a NetworkedObject if it's part of a pool. Use this instead of the MonoBehaviour Destroy method. Can only be called from Server.
        /// </summary>
        /// <param name="netObject">The NetworkedObject instance to destroy</param>
        public static void DestroyPoolObject(NetworkedObject netObject)
        {
            if (!NetworkingManager.singleton.isServer)
            {
                Debug.LogWarning("MLAPI: Objects can only be destroyed on the server");
                return;
            }
            netObject.gameObject.SetActive(false);
            using (BitWriter writer = new BitWriter())
            {
                writer.WriteUInt(netObject.NetworkId);

                InternalMessageHandler.Send("MLAPI_DESTROY_POOL_OBJECT", "MLAPI_INTERNAL", writer, null);
            }
        }
    }
}
