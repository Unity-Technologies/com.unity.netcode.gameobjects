using Unity.Netcode;
using UnityEngine;

namespace BoostrapSample
{
    /// <summary>
    /// Component attached to NetworkManager's "Player Prefab".
    /// </summary>
    public class BootstrapPlayer : NetworkBehaviour
    {
        /// <summary>
        /// Move local player on network spawn.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                Move();
            }
        }

        /// <summary>
        /// Move requests are funneled through this method.
        /// If this method is invoked on the client instance of this player, it will invoke a ServerRpc.
        /// If this method is invoked on the server instance of this player, it will position the player at a random
        /// point on a plane.
        /// </summary>
        /// <remarks>
        /// Since a NetworkTransform component is attached to this player, and the authority on that component is set to
        /// "Server", this transform's position modification can only be performed on the server, where it will then be
        /// replicated on all other clients.
        /// </remarks>
        public void Move()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                transform.position = GetRandomPositionOnXYPlane();
            }
            else
            {
                SubmitPositionRequestServerRpc();
            }
        }

        [ServerRpc]
        void SubmitPositionRequestServerRpc()
        {
            Debug.Log($"SubmitPositionRequestServerRpc received on server from client: {OwnerClientId}");
            Move();
        }

        static Vector3 GetRandomPositionOnXYPlane()
        {
            return new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0f);
        }
    }
}