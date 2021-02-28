using System.Collections.Generic;

namespace MLAPI.Connection
{
    /// <summary>
    /// A NetworkedClient
    /// </summary>
    public class NetworkedClient
    {
        /// <summary>
        /// The Id of the NetworkedClient
        /// </summary>
        public ulong ClientId;

        /// <summary>
        /// The PlayerObject of the Client
        /// </summary>
        public NetworkObject PlayerObject;

        /// <summary>
        /// The NetworkObject's owned by this Client
        /// </summary>
        public readonly List<NetworkObject> OwnedObjects = new List<NetworkObject>();
    }
}