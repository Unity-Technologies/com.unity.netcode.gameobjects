using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class ParentObjectScript : NetworkBehaviour
    {
        private void Update()
        {
            if (IsSpawned)
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    PickupItemServerRpc();
                }
                if (Input.GetKeyDown(KeyCode.D))
                {
                    DropItemServerRpc();
                }
            }
        }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            if (parentNetworkObject != null)
            {
                transform.localPosition = Vector3.up * transform.localPosition.y;
            }

            base.OnNetworkObjectParentChanged(parentNetworkObject);
        }

        private void PickUpItem(NetworkObject player)
        {
            if (transform.parent == null)
            {
                transform.parent = player.transform;

                transform.localPosition = Vector3.up * transform.localPosition.y;

                Debug.Log($"{name} is now parented under {player.name}!");
            }
            else
            {
                if (transform.parent == player.transform)
                {
                    Debug.Log($"{player.name} already picked up {name}!");
                }
                else
                {
                    Debug.Log($"{name} cannot be picked up by {player.name} as it is already picked up by another player!");
                }
            }
        }

        private void DropItem(NetworkObject player)
        {
            if (transform.parent == player.transform)
            {
                transform.parent = null;
                Debug.Log($"{name} is no longer parented.");
            }
            else
            {
                if (transform.parent == null)
                {
                    Debug.Log($"{player.name} is not the parent of {name}!");
                }
                else
                {
                    Debug.Log($"{name} cannot be dropped by {player.name} as it is already picked up by another player!");
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PickupItemServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (NetworkManager.ConnectedClients.ContainsKey(serverRpcParams.Receive.SenderClientId))
            {
                PickUpItem(NetworkManager.ConnectedClients[serverRpcParams.Receive.SenderClientId].PlayerObject);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DropItemServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (NetworkManager.ConnectedClients.ContainsKey(serverRpcParams.Receive.SenderClientId))
            {
                DropItem(NetworkManager.ConnectedClients[serverRpcParams.Receive.SenderClientId].PlayerObject);
            }
        }
    }
}
