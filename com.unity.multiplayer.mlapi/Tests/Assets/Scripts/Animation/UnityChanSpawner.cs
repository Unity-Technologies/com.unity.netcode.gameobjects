using UnityEngine;

namespace MLAPI.TestAssets.Animation
{

    [AddComponentMenu("MLAPI/TestAssets/Animation/UnityChan")]
    public class UnityChanSpawner : MonoBehaviour
    {
        public GameObject UnityChanPrefab;

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += () =>
            {
                var unityChanGameObj = Instantiate(UnityChanPrefab);
                var unityChanNetObj = unityChanGameObj.GetComponent<NetworkObject>();
                unityChanNetObj.Spawn();
            };
        }
    }
}
