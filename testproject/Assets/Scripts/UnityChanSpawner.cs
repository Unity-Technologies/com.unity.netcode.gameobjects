using MLAPI;
using UnityEngine;

public class UnityChanSpawner : MonoBehaviour
{
    public GameObject unityChanPrefab;
    
    private void Start()
    {
        NetworkingManager.Singleton.OnServerStarted += () =>
        {
            var unityChanGameObj = Instantiate(unityChanPrefab);
            var unityChanNetObj = unityChanGameObj.GetComponent<NetworkedObject>();
            unityChanNetObj.Spawn();
        };
    }
}