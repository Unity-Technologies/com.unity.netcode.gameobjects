using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    /// <summary>
    /// Spawns a single NetworkPrefab
    /// </summary>
    public class DontDestroyOnLoadSpawnHandler : NetworkBehaviour
    {
        public GameObject NetworkPrefabToCreate;

        public override void OnNetworkSpawn()
        {
            if (NetworkPrefabToCreate != null && IsServer)
            {
                var newGameObject = Instantiate(NetworkPrefabToCreate);
                var networkObject = newGameObject.GetComponent<NetworkObject>();
                networkObject.Spawn();
            }

            base.OnNetworkSpawn();
        }
    }
}
