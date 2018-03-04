using MLAPI.Data;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents
{
    public static class NetworkPoolManager
    {
        internal static Dictionary<string, NetworkPool> Pools;
        //We want to keep the pool indexes incrementing, this is to prevent new pools getting old names and the wrong objects being spawned. 
        private static ushort PoolIndex = 0;

        internal static Dictionary<ushort, string> PoolIndexToPoolName = new Dictionary<ushort, string>();
        internal static Dictionary<string, ushort> PoolNamesToIndexes = new Dictionary<string, ushort>();

        public static void CreatePool(string poolName, GameObject poolPrefab, uint size = 16)
        {
            if(Pools.ContainsKey(poolName))
            {
                Debug.LogWarning("MLAPI: A pool with the name " + poolName + " already exists");
                return;
            }
            else if(poolPrefab == null)
            {
                Debug.LogWarning("MLAPI: A pool prefab is required");
            }
            PoolIndexToPoolName.Add(PoolIndex, poolName);
            PoolNamesToIndexes.Add(poolName, PoolIndex);
            PoolIndex++;
            Pools.Add(poolName, new NetworkPool(poolPrefab, size, poolName));
        }

        public static GameObject SpawnPoolObject(string poolName, Vector3 position, Quaternion rotation)
        {
            if(NetworkingManager.singleton.isServer)
            {
                using(MemoryStream stream = new MemoryStream(26))
                {
                    using(BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(PoolNamesToIndexes[poolName]);
                        writer.Write(position.x);
                        writer.Write(position.y);
                        writer.Write(position.z);
                        writer.Write(rotation.eulerAngles.x);
                        writer.Write(rotation.eulerAngles.y);
                        writer.Write(rotation.eulerAngles.z);
                    }
                    NetworkingManager.singleton.Send("MLAPI_SPAWN_POOL_OBJECT", "MLAPI_RELIABLE_FRAGMENTED_SEQUENCED", stream.GetBuffer());
                }
            }
            return Pools[poolName].SpawnObject(position, rotation);
        }

        public static void DestroyPoolObject(GameObject gameObject)
        {
            gameObject.SetActive(false);
        }
    }
}
