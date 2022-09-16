using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class ChildObjectScript : NetworkBehaviour
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
                    if (!IsServer)
                    {
                        m_WorldPositionStays = false;
                    }
                }
                if (Input.GetKeyDown(KeyCode.E))
                {
                    DropItemServerRpc();
                }
            }
        }

        private Vector3 m_OriginalLocalPosition;
        private Quaternion m_OriginalLocalRotation;
        private Vector3 m_OriginalLocalScale;

        public override void OnNetworkSpawn()
        {
            m_OriginalLocalPosition = transform.localPosition;
            m_OriginalLocalRotation = transform.localRotation;
            m_OriginalLocalScale = transform.localScale;
            base.OnNetworkSpawn();
        }

        private NetworkObject m_LastParent;

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            if (!IsSpawned || m_WorldPositionStays)
            {
                return;
            }

            if (parentNetworkObject != null)
            {
                transform.localPosition = m_OriginalLocalPosition;
                transform.localRotation = m_OriginalLocalRotation;
                transform.localScale = m_OriginalLocalScale;
            }
            else if (parentNetworkObject == null && m_LastParent)
            {
                transform.position = m_LastParent.transform.position + m_OriginalLocalPosition;
                if (!IsServer)
                {
                    m_WorldPositionStays = true;
                }
            }

            transform.localRotation = m_OriginalLocalRotation;
            transform.localScale = m_OriginalLocalScale;

            m_LastParent = parentNetworkObject;
            base.OnNetworkObjectParentChanged(parentNetworkObject);
        }

        private void PickUpDropItem(NetworkObject player, bool worldPositionStays = true)
        {
            if (transform.parent != null && player != null )
            {
                if (transform.parent == player.transform)
                {
                    Debug.Log($"{player.name} already picked up {name}!");
                }
                else
                {
                    Debug.Log($"{name} cannot be picked up by {player.name} as it is already picked up by another player!");
                }
                return;
            }
            m_WorldPositionStays = worldPositionStays;
            NetworkObject.TrySetParent(player, worldPositionStays);
            if (player == null)
            {
                Debug.Log($"{name} is no longer parented.");
            }
            else
            {
                Debug.Log($"{name} is now parented under {player.name}!");
            }
        }

        private bool m_WorldPositionStays;

        [ServerRpc(RequireOwnership = false)]
        public void PickupItemServerRpc(bool worldPositionStays = true, ServerRpcParams serverRpcParams = default)
        {
            if (NetworkManager.ConnectedClients.ContainsKey(serverRpcParams.Receive.SenderClientId))
            {
                PickUpDropItem(NetworkManager.ConnectedClients[serverRpcParams.Receive.SenderClientId].PlayerObject, worldPositionStays);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DropItemServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (NetworkManager.ConnectedClients.ContainsKey(serverRpcParams.Receive.SenderClientId))
            {
                var player = NetworkManager.ConnectedClients[serverRpcParams.Receive.SenderClientId].PlayerObject;
                if (transform.parent == player.transform)
                {
                    // When dropping, we drop with whatever WorldPositionStays setting we picked up with
                    PickUpDropItem(null, m_WorldPositionStays);
                }
            }
        }
    }
}
