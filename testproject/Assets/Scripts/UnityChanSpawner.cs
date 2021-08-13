using Unity.Netcode;
using UnityEngine;

public class UnityChanSpawner : MonoBehaviour
{
    public GameObject UnityChanPrefab;

    private void OnServerStarted()
    {
        var unityChanGameObj = Instantiate(UnityChanPrefab);
        var unityChanNetObj = unityChanGameObj.GetComponent<NetworkObject>();
        unityChanNetObj.Spawn();
    }

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }
}
