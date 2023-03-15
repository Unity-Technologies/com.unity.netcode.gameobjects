using System.Collections.Generic;

namespace Unity.Netcode
{
    /// <summary>
    /// A NetworkClient
    /// </summary>
    public class NetworkClient
    {
        /// <summary>
        /// The ClientId of the NetworkClient
        /// </summary>
        public ulong ClientId;

        /// <summary>
        /// The PlayerObject of the Client
        /// </summary>
        public NetworkObject PlayerObject;

        /// <summary>
        /// The NetworkObject's owned by this Client
        /// </summary>
        public List<NetworkObject> OwnedObjects
        {
            get
            {
                if (PlayerObject != null && PlayerObject.NetworkManager != null && PlayerObject.NetworkManager.IsListening)
                {
                    return PlayerObject.NetworkManager.SpawnManager.GetClientOwnedObjects(ClientId);
                }

                return new List<NetworkObject>();
            }
        }
    }
}
