using MLAPI;
using UnityEngine;

public class UnityChanSpawner : MonoBehaviour
{
    public GameObject unityChanPrefab;
    
    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += () =>
        {
            var unityChanGameObj = Instantiate(unityChanPrefab);
            var unityChanNetObj = unityChanGameObj.GetComponent<NetworkObject>();
            unityChanNetObj.Spawn();
        };
    }
}