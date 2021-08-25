using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class DontDestroyOnLoadTestor : NetworkBehaviour
    {
        public GameObject MoveToDontDestroyOnLoad;

        public override void OnNetworkSpawn()
        {
            if (MoveToDontDestroyOnLoad != null && IsServer)
            {
                var newGameObject = Instantiate(MoveToDontDestroyOnLoad);
                var networkObject = newGameObject.GetComponent<NetworkObject>();
                networkObject.Spawn();
            }

            base.OnNetworkSpawn();
        }
    }
}
