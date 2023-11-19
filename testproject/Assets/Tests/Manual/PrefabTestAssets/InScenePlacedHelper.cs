using System.Collections.Generic;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class InScenePlacedHelper : NetworkBehaviour
    {
        public static List<NetworkObject> ServerSpawnedInScenePlaced = new List<NetworkObject>();

        public static List<NetworkObject> ServerInSceneDefined = new List<NetworkObject>();

        public static void Reset()
        {
            ServerSpawnedInScenePlaced.Clear();
            ServerInSceneDefined.Clear();
        }

        public bool IsInSceneDefined;

        public override void OnNetworkSpawn()
        {
            if (IsServer && !NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                if (!IsInSceneDefined)
                {
                    ServerSpawnedInScenePlaced.Add(NetworkObject);
                }
                else
                {
                    ServerInSceneDefined.Add(NetworkObject);
                }
            }
            base.OnNetworkSpawn();
        }
    }
}
