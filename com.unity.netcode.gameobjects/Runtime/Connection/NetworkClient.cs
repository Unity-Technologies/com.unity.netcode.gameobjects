using System.Collections.Generic;

namespace Unity.Netcode
{
    /// <summary>
    /// A NetworkClient
    /// </summary>
    public class NetworkClient
    {
        /// <summary>
        /// Returns true if the session instance is considered a server
        /// </summary>
        internal bool IsServer { get; set; }

        /// <summary>
        /// Returns true if the session instance is considered a client
        /// </summary>
        internal bool IsClient { get; set; }

        /// <summary>
        /// Returns true if the session instance is considered a host
        /// </summary>
        internal bool IsHost => IsClient && IsServer;

        /// <summary>
        /// When true, the client is connected, approved, and synchronized with
        /// the server.
        /// </summary>
        internal bool IsConnected { get; set; }

        /// <summary>
        /// Is true when the client has been approved.
        /// </summary>
        internal bool IsApproved { get; set; }

        /// <summary>
        /// The ClientId of the NetworkClient
        /// </summary>
        public ulong ClientId;

        /// <summary>
        /// The PlayerObject of the Client
        /// </summary>
        public NetworkObject PlayerObject;

        /// <summary>
        /// The list of NetworkObject's owned by this client instance
        /// </summary>
        public List<NetworkObject> OwnedObjects => IsConnected ? SpawnManager.GetClientOwnedObjects(ClientId) : new List<NetworkObject>();

        internal NetworkSpawnManager SpawnManager { get; private set; }

        internal void SetRole(bool isServer, bool isClient, NetworkManager networkManager = null)
        {
            IsServer = isServer;
            IsClient = isClient;
            if (!IsServer && !isClient)
            {
                PlayerObject = null;
                ClientId = 0;
                IsConnected = false;
                IsApproved = false;
            }

            if (networkManager != null)
            {
                SpawnManager = networkManager.SpawnManager;
            }
        }

        internal void AssignPlayerObject(ref NetworkObject networkObject)
        {
            PlayerObject = networkObject;
        }
    }
}
