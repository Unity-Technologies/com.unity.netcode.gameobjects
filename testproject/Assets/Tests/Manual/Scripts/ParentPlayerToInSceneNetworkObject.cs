using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace TestProject.ManualTests
{
    public class ParentPlayerToInSceneNetworkObject : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (IsServer && IsOwner)
            {
#if NGO_DAMODE
                // Client-Server mode, the server handles parenting the players
                if (!NetworkManager.DistributedAuthorityMode)
                {
                    NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
                }
#endif
                // Server player is parented under this NetworkObject
                SetPlayerParent(NetworkManager.LocalClientId);
            }
        }

        private void NetworkManager_OnClientConnectedCallback(ulong clientId)
        {
            if (clientId != NetworkManager.LocalClientId)
            {
                // Set the newly joined and synchronized client-player as a child of this in-scene placed NetworkObject
                SetPlayerParent(clientId);
            }
        }

        private void SetPlayerParent(ulong clientId)
        {
            if (IsSpawned)
            {
#if NGO_DAMODE
                var playerObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject.gameObject.scene != gameObject.scene)
                {
                    SceneManager.MoveGameObjectToScene(playerObject.gameObject, gameObject.scene);
                }
                playerObject.TrySetParent(NetworkObject, false);
#else
                // As long as the client (player) is in the connected clients list
                if (NetworkManager.ConnectedClients.ContainsKey(clientId))
                {
                    var playerObject = NetworkManager.ConnectedClients[clientId].PlayerObject;
                    if (playerObject.gameObject.scene != gameObject.scene)
                    {
                        SceneManager.MoveGameObjectToScene(playerObject.gameObject, gameObject.scene);
                    }
                    // Set the player as a child of this in-scene placed NetworkObject
                    // We parent in local space by setting the WorldPositionStays value to false
                    NetworkManager.ConnectedClients[clientId].PlayerObject.TrySetParent(NetworkObject, false);
                }
#endif
            }
        }

#if NGO_DAMODE
        [ServerRpc(RequireOwnership = false)]
        private void SetParentServerRpc()
        {
            SetPlayerParent(NetworkManager.LocalClientId);
        }
#endif

        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            // OnSceneEvent is very useful for many things
            switch (sceneEvent.SceneEventType)
            {
                // The SceneEventType event tells the server that a client-player has:
                // 1.) Connected and Spawned
                // 2.) Loaded all scenes that were loaded on the server at the time of connecting
                // 3.) Synchronized (instantiated and spawned) all NetworkObjects in the network session
                case SceneEventType.SynchronizeComplete:
                    {
                        // As long as we are not the server-player
                        if (sceneEvent.ClientId != NetworkManager.LocalClientId)
                        {
                            // Set the newly joined and synchronized client-player as a child of this in-scene placed NetworkObject
                            SetPlayerParent(sceneEvent.ClientId);
                        }
                        break;
                    }
            }
        }
    }
}
