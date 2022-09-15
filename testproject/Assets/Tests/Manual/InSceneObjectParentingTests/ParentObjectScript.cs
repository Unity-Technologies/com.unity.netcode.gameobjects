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
                if (Input.GetKeyDown(KeyCode.F))
                {
                    PickupItemServerRpc(false);
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    DropItemServerRpc();
                }
            }
        }

        private Vector3 m_OriginalLocalPosition;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_OriginalLocalPosition = transform.localPosition;
            }
            base.OnNetworkSpawn();
        }

        private void PickUpItem(NetworkObject player, bool worldPositionStays = true)
        {
            if (transform.parent == null)
            {
                if (!worldPositionStays)
                {
                    // We do this because when not parented the local position is the "world space position".
                    transform.localPosition = m_OriginalLocalPosition;
                }
                if (NetworkObject.TrySetParent(player.transform, worldPositionStays))
                {
                    Debug.Log($"{name} is now parented under {player.name}!");
                }
                else
                {
                    Debug.Log($"{name} failed to get parented under {player.name}!");
                }
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
        public void PickupItemServerRpc(bool wordlPositionStays = true, ServerRpcParams serverRpcParams = default)
        {
            if (NetworkManager.ConnectedClients.ContainsKey(serverRpcParams.Receive.SenderClientId))
            {
                PickUpItem(NetworkManager.ConnectedClients[serverRpcParams.Receive.SenderClientId].PlayerObject, wordlPositionStays);
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
