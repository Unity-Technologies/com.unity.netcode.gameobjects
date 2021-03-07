using MLAPI;
using UnityEngine;

public class UnityChanSpawner : MonoBehaviour
{
    public GameObject unityChanPrefab;

    [SerializeField]
    private NetworkManager m_NetworkManager;
    
    private void Start()
    {
        m_NetworkManager.OnServerStarted += () =>
        {
            var unityChanGameObj = Instantiate(unityChanPrefab);
            var unityChanNetObj = unityChanGameObj.GetComponent<NetworkObject>();
            unityChanNetObj.Spawn();
        };
    }
}
