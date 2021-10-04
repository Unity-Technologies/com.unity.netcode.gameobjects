using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject Prefab;
    public float Frequency;

    private void OnServerStarted()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Frequency);
            var go = Instantiate(Prefab, transform.position, Quaternion.identity);
            var networkObject = go.GetComponent<NetworkObject>();
            networkObject.Spawn();
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
