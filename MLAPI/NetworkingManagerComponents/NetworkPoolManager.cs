using MLAPI.Data;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents
{
    public static class NetworkPoolManager
    {
        internal static Dictionary<ushort, NetworkPool> Pools;
        private static ushort PoolIndex = 0;
        internal static Dictionary<string, ushort> PoolNamesToIndexes;

        //Server only
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

        public static GameObject SpawnPoolObject(string poolName, Vector3 position, Quaternion rotation)
        {
            if (!NetworkingManager.singleton.isServer)
            {
                Debug.LogWarning("MLAPI: Object spawning can only occur on server");
                return null;
            }
            GameObject go = Pools[PoolNamesToIndexes[poolName]].SpawnObject(position, rotation);
            using (MemoryStream stream = new MemoryStream(28))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(go.GetComponent<NetworkedObject>().NetworkId);
                    writer.Write(position.x);
                    writer.Write(position.y);
                    writer.Write(position.z);
                    writer.Write(rotation.eulerAngles.x);
                    writer.Write(rotation.eulerAngles.y);
                    writer.Write(rotation.eulerAngles.z);
                }
                NetworkingManager.singleton.Send("MLAPI_SPAWN_POOL_OBJECT", "MLAPI_INTERNAL", stream.GetBuffer());
            }
            return go;
        }

        public static void DestroyPoolObject(NetworkedObject netObject)
        {
            if (!NetworkingManager.singleton.isServer)
            {
                Debug.LogWarning("MLAPI: Objects can only be destroyed on the server");
                return;
            }
            netObject.gameObject.SetActive(false);
            using (MemoryStream stream = new MemoryStream(4))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(netObject.NetworkId);
                }
                NetworkingManager.singleton.Send("MLAPI_DESTROY_POOL_OBJECT", "MLAPI_INTERNAL", stream.GetBuffer());
            }
        }
    }
}
