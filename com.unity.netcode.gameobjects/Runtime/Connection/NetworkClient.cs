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
        public bool IsServer { get; internal set; }

        /// <summary>
        /// Returns true if the session instance is considered a client
        /// </summary>
        public bool IsClient { get; internal set; }

        /// <summary>
        /// Returns true if the session instance is considered a host
        /// </summary>
        public bool IsHost => IsClient && IsServer;

        /// <summary>
        /// When true, the client is connected, approved, and synchronized with
        /// the server.
        /// </summary>
        public bool IsConnected { get; internal set; }

        /// <summary>
        /// Is true when the client has been approved.
        /// </summary>
        public bool IsApproved { get; internal set; }

        /// <summary>
        /// The ClientId of the NetworkClient
        /// </summary>
        // TODO-2023-Q2: Determine if we want to make this property a public get and internal/private set
        // There is no reason for a user to want to set this, but this will fail the package-validation-suite
        public ulong ClientId;

        /// <summary>
        /// The PlayerObject of the Client
        /// </summary>
        // TODO-2023-Q2: Determine if we want to make this property a public get and internal/private set
        // There is no reason for a user to want to set this, but this will fail the package-validation-suite
        public NetworkObject PlayerObject;

        /// <summary>
        /// The list of NetworkObject's owned by this client instance
        /// </summary>
        public List<NetworkObject> OwnedObjects
        {
            get
            {
                if (IsConnected)
                {
                    return SpawnManager.GetClientOwnedObjects(ClientId);
                }

                return new List<NetworkObject>();
            }
        }

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
            SpawnManager = networkManager?.SpawnManager;
        }

        internal void AssignPlayerObject(ref NetworkObject networkObject)
        {
            PlayerObject = networkObject;
        }
    }
}
