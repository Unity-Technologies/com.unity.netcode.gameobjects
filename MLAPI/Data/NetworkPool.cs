using MLAPI.MonoBehaviours.Core;
using UnityEngine;

namespace MLAPI.Data
{
    internal class NetworkPool
    {
        internal GameObject[] objects;
        internal ushort poolId;

        internal NetworkPool(int prefabId, uint size, ushort poolIndex)
        {
            objects = new GameObject[size];
            poolId = poolIndex;
            for (int i = 0; i < size; i++)
            {
                GameObject go = MonoBehaviour.Instantiate(NetworkingManager.singleton.NetworkConfig.NetworkedPrefabs[prefabId].prefab, Vector3.zero, Quaternion.identity);
                go.GetComponent<NetworkedObject>()._isPooledObject = true;
                go.GetComponent<NetworkedObject>().poolId = poolId;
                go.GetComponent<NetworkedObject>().Spawn();
                go.name = "Pool Id: " + poolId + " #" + i;
                go.SetActive(false);
            }
        }

        internal GameObject SpawnObject(Vector3 position, Quaternion rotation)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].activeInHierarchy)
                {
                    GameObject go = objects[i];
                    go.transform.position = position;
                    go.transform.rotation = rotation;
                    go.SetActive(true);
                }
            }
            Debug.LogWarning("MLAPI: The pool " + poolId + " has ran out of space");
            return null;
        }
    }
}
