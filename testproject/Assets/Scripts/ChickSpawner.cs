using MLAPI;
using UnityEngine;

public class ChickSpawner : MonoBehaviour
{
    public GameObject chickPrefab;
    
    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += () =>
        {
            var chickGameObj = Instantiate(chickPrefab);
            var chickNetObj = chickGameObj.GetComponent<NetworkedObject>();
            chickNetObj.Spawn();
        };
    }
}