using Unity.Netcode;
using UnityEngine;

namespace TestProject.ManualTests
{
    public class ChildObjectScript : NetworkBehaviour
    {
        private bool m_WorldPositionStays;
        private NetworkObject m_LastParent;
        private Vector3 m_OriginalLocalPosition;
        private Quaternion m_OriginalLocalRotation;
        private Vector3 m_OriginalLocalScale;

        private void Update()
        {
            if (IsSpawned)
            {
                // Parents with WorldPositionStays set to true
                if (Input.GetKeyDown(KeyCode.G))
                {
                    PickupItemServerRpc();
                }

                // Parents with WorldPositionStays set to false
                if (Input.GetKeyDown(KeyCode.F))
                {
                    PickupItemServerRpc(false);
                }

                // Drops the object using the last WorldPositionStays setting
                if (Input.GetKeyDown(KeyCode.D))
                {
                    DropItemServerRpc();
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                m_OriginalLocalPosition = transform.localPosition;
                m_OriginalLocalRotation = transform.localRotation;
                m_OriginalLocalScale = transform.localScale;
            }
            base.OnNetworkSpawn();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This is an example of how to adjust the child object's local space transform
        /// values on the server side that will be synchronized with clients without the
        /// need to have a NetworkTransform component attached to the child object.
        /// </remarks>
        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            // If we are not spawned, world position is "staying", or we are not the server
            // then don't do anything
            if (!IsSpawned || m_WorldPositionStays || !IsServer)
            {
                return;
            }

            if (parentNetworkObject != null)
            {
                // This preserves the original position offset of the object when parented with
                // WorldPositionStays set to false.
                transform.localPosition = m_OriginalLocalPosition;

                // Optionally, you can also make other modifications to rotation or scale
                transform.localRotation = m_OriginalLocalRotation;
                transform.localScale = m_OriginalLocalScale;
            }
            else
            if (parentNetworkObject == null && m_LastParent)
            {
                // This example will drop the object at the current world position
                transform.position = m_LastParent.transform.position;
            }

            m_LastParent = parentNetworkObject;
            base.OnNetworkObjectParentChanged(parentNetworkObject);
        }

        private void PickUpDropItem(NetworkObject player, bool worldPositionStays = true)
        {
            if (transform.parent != null && player != null)
            {
                Debug.Log(transform.parent == player.transform ? $"{player.name} already picked up {name}!" : $"{name} cannot be picked up by {player.name}! It is already parented under another player!");
                return;
            }
            m_WorldPositionStays = worldPositionStays;

            NetworkObject.TrySetParent(player, worldPositionStays);

            Debug.Log(player == null ? $"{name} is no longer parented." : $"{name} is now parented under {player.name}!");
        }


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
