using System.Collections.Generic;

namespace Unity.Netcode
{
    /// <summary>
    /// A NetworkClient
    /// </summary>
    public class NetworkClient
    {
        internal NetworkManager NetworkManager { get; private set; }

        /// Gets Whether or not a server is running
        /// </summary>
        public bool IsServer { get; internal set; }

        /// <summary>
        /// Gets Whether or not a client is running
        /// </summary>
        public bool IsClient { get; internal set; }

        public bool IsHost => IsClient && IsServer;

        internal void SetRole(bool isServer, bool isClient, NetworkManager networkManager = null)
        {
            IsServer = isServer;
            IsClient = isClient;
            if (!IsServer && !isClient)
            {
                PlayerObject = null;
                ClientId = ulong.MaxValue;
            }
            NetworkManager = networkManager;
        }

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
                if (NetworkManager != null && NetworkManager.IsListening)
                {
                    return NetworkManager.SpawnManager.GetClientOwnedObjects(ClientId);
                }

                return new List<NetworkObject>();
            }
        }
        internal void AssignPlayerObject(ref NetworkObject networkObject)
        {
            PlayerObject = networkObject;
        }

        public NetworkClient(bool isServer, bool isClient, ulong clientId, NetworkManager networkManager)
        {
            SetRole(isServer, isClient, networkManager);
            ClientId = clientId;
        }

        public NetworkClient()
        {
            SetRole(false, false);
        }
    }
}
