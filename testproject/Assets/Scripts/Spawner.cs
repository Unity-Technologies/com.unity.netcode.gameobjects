using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject Prefab;
    public float Frequency;
    [Range(1, 100)]
    public int MaxInstances = 40;

    private void OnServerStarted()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        var instanceCount = 0;
        while (true)
        {            
            var go = Instantiate(Prefab, transform.position, Quaternion.identity);
            var networkObject = go.GetComponent<NetworkObject>();
            networkObject.Spawn();
            instanceCount++;
            // If frequency == 0 then it is a single spawner
            if (Frequency > 0)
            {
                yield return new WaitForSeconds(Frequency);
                if (instanceCount >= MaxInstances)
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
}
