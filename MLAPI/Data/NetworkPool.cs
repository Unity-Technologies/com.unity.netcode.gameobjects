using MLAPI.Logging;
using UnityEngine;

namespace MLAPI.Internal
{
    internal class NetworkPool
    {
        internal NetworkedObject[] objects;
        internal ushort poolId;

        internal NetworkPool(int prefabId, uint size, ushort poolIndex)
        {
            objects = new NetworkedObject[size];
            poolId = poolIndex;
            for (int i = 0; i < size; i++)
            {
                GameObject go = MonoBehaviour.Instantiate(NetworkingManager.Singleton.NetworkConfig.NetworkedPrefabs[prefabId].prefab, Vector3.zero, Quaternion.identity) as GameObject;
                objects[i] = go.GetComponent<NetworkedObject>();
                objects[i].IsPooledObject = true;
                objects[i].PoolId = poolId;
                objects[i].Spawn();
                go.name = "Pool Id: " + poolId + " #" + i;
                go.SetActive(false);
            }
        }

        internal NetworkedObject SpawnObject(Vector3 position, Quaternion rotation)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (!objects[i].gameObject.activeInHierarchy)
                {
                    GameObject go = objects[i].gameObject;
                    go.transform.position = position;
                    go.transform.rotation = rotation;
                    go.SetActive(true);
                    return objects[i];
                }
            }
            if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The pool " + poolId + " has ran out of space");
            return null;
        }
    }
}
