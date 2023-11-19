using System.Collections.Generic;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class InScenePlacedHelper : NetworkBehaviour
    {
        public static List<NetworkObject> ServerSpawnedInScenePlaced = new List<NetworkObject>();

        public override void OnNetworkSpawn()
        {
            if (IsServer && !NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                ServerSpawnedInScenePlaced.Add(NetworkObject);
            }
            base.OnNetworkSpawn();
        }
    }
}
