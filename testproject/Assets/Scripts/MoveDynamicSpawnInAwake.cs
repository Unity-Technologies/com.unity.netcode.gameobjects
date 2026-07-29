using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_6000_6_OR_NEWER && UNITY_EDITOR
[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
#endif
public partial class MoveDynamicSpawnInAwake : MonoBehaviour
{
    public NetworkObject MovedObject { get; private set; }
    private static readonly HashSet<NetworkObject> k_AlreadyMovedObjects = new();
    private void Awake()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
        {
            return;
        }

        foreach (var spawnedObject in networkManager.SpawnManager.SpawnedObjects.Values)
        {
            if (spawnedObject == null || !spawnedObject.IsSpawned || !spawnedObject.HasAuthority || spawnedObject.IsPlayerObject || spawnedObject.InScenePlaced)
            {
                continue;
            }

            if (!k_AlreadyMovedObjects.Add(spawnedObject))
            {
                continue;
            }

            MovedObject = spawnedObject;
            SceneManager.MoveGameObjectToScene(spawnedObject.gameObject, gameObject.scene);
            return;
        }
    }
}
