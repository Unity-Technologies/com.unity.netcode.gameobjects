using System.Collections.Generic;
using MLAPI.Data;
using MLAPI.Internal;
using MLAPI.Logging;
using MLAPI.Serialization;
using UnityEngine;

namespace MLAPI.Components
{
    /// <summary>
    /// Main class for managing network pools
    /// </summary>
    public static class NetworkPoolManager
    {
        internal static readonly Dictionary<ushort, NetworkPool> Pools = new Dictionary<ushort, NetworkPool>();
        private static ushort PoolIndex = 0;
        internal static readonly Dictionary<string, ushort> PoolNamesToIndexes = new Dictionary<string, ushort>();

        /// <summary>
        /// Creates a networked object pool. Can only be called from the server
        /// </summary>
        /// <param name="poolName">Name of the pool</param>
        /// <param name="spawnablePrefabIndex">The index of the prefab to use in the spawnablePrefabs array</param>
        /// <param name="size">The amount of objects in the pool</param>
        public static void CreatePool(string poolName, int spawnablePrefabIndex, uint size = 16)
        {
            if(!NetworkingManager.Singleton.IsServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Pools can only be created on the server");
                return;
            }
            NetworkPool pool = new NetworkPool(spawnablePrefabIndex, size, PoolIndex);
            Pools.Add(PoolIndex, pool);
            PoolNamesToIndexes.Add(poolName, PoolIndex);
            PoolIndex++;
        }

        /// <summary>
        /// This destroys an object pool and all of it's objects. Can only be called from the server
        /// </summary>
        /// <param name="poolName">The name of the pool</param>
        public static void DestroyPool(string poolName)
        {
            if (!NetworkingManager.Singleton.IsServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Pools can only be destroyed on the server");
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
        public static NetworkedObject SpawnPoolObject(string poolName, Vector3 position, Quaternion rotation)
        {
            if (!NetworkingManager.Singleton.IsServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Object spawning can only occur on server");
                return null;
            }
            NetworkedObject netObject = Pools[PoolNamesToIndexes[poolName]].SpawnObject(position, rotation);
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(netObject.NetworkId);

                    writer.WriteSinglePacked(position.x);
                    writer.WriteSinglePacked(position.y);
                    writer.WriteSinglePacked(position.z);

                    writer.WriteSinglePacked(rotation.eulerAngles.x);
                    writer.WriteSinglePacked(rotation.eulerAngles.y);
                    writer.WriteSinglePacked(rotation.eulerAngles.z);

                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_SPAWN_POOL_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None);
                }
            }
            return netObject;
        }

        /// <summary>
        /// Destroys a NetworkedObject if it's part of a pool. Use this instead of the MonoBehaviour Destroy method. Can only be called from Server.
        /// </summary>
        /// <param name="netObject">The NetworkedObject instance to destroy</param>
        public static void DestroyPoolObject(NetworkedObject netObject)
        {
            if (!NetworkingManager.Singleton.IsServer)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Objects can only be destroyed on the server");
                return;
            }
            netObject.gameObject.SetActive(false);
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt32Packed(netObject.NetworkId);

                    InternalMessageHandler.Send(MLAPIConstants.MLAPI_DESTROY_POOL_OBJECT, "MLAPI_INTERNAL", stream, SecuritySendFlags.None);
                }
            }
        }
    }
}
