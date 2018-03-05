using UnityEngine;

namespace MLAPI.Data
{
    internal class NetworkPool
    {
        internal GameObject prefab;
        internal GameObject[] objects;
        internal string poolName;

        internal NetworkPool(GameObject prefab, uint size, string name)
        {
            objects = new GameObject[size];
            poolName = name;

            for (int i = 0; i < size; i++)
            {
                GameObject go = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
                go.name = "Pool " + poolName + " #" + i;
                go.SetActive(false);
            }
        }

        internal NetworkPool(GameObject[] prefabs, string name)
        {
            objects = prefabs;
            poolName = name;
            int size = prefabs.Length;

            for (int i = 0; i < size; i++)
            {
                prefabs[i].name = "Pool " + poolName + " #" + i;
                prefabs[i].SetActive(false);
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
            Debug.LogWarning("MLAPI: The pool " + poolName + " has ran out of space");
            return null;
        }
    }
}
