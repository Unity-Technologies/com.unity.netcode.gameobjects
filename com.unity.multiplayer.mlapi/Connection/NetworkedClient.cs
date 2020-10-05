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
        public NetworkedObject PlayerObject;
        /// <summary>
        /// The NetworkedObject's owned by this Client
        /// </summary>
        public readonly List<NetworkedObject> OwnedObjects = new List<NetworkedObject>();
        /// <summary>
        /// The encryption key used for this client
        /// </summary>
        public byte[] AesKey;
    }
}
