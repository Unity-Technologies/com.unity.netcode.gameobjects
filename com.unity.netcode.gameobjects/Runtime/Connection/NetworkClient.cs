using System.Collections.Generic;

namespace Unity.Netcode
{
    /// <summary>
    /// A NetworkClient
    /// </summary>
    public class NetworkClient
    {
        /// Gets Whether or not a server is running
        /// </summary>
        public bool IsServer { get; internal set; }

        /// <summary>
        /// Gets Whether or not a client is running
        /// </summary>
        public bool IsClient { get; internal set; }

        public bool IsHost => IsClient && IsServer;

        /// <summary>
        /// The ClientId of the NetworkClient
        /// </summary>
        public ulong ClientId { get; internal set; }

        /// <summary>
        /// The PlayerObject of the Client
        /// </summary>
        public NetworkObject PlayerObject { get; internal set; }

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
        internal void AssignPlayerObject(ref NetworkObject networkObject)
        {
            PlayerObject = networkObject;
        }

        public NetworkClient(bool isServer, bool isClient, ulong clientId)
        {
            IsServer = isServer;
            IsClient = isClient;
            ClientId = clientId;
        }

        public NetworkClient(bool isServer, bool isClient)
        {
            IsServer = isServer;
            IsClient = isClient;
        }

        public NetworkClient()
        {
            IsServer = false;
            IsClient = false;
            ClientId = ulong.MaxValue;
            PlayerObject = null;
        }
    }
}
