using MLAPI;
using UnityEngine;

public class ChanSpawner : MonoBehaviour
{
    public GameObject ChanPrefab;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += () =>
        {
            var unityChanGameObj = Instantiate(ChanPrefab);
            var unityChanNetObj = unityChanGameObj.GetComponent<NetworkObject>();
            unityChanNetObj.Spawn();
        };
    }
}
