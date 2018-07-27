using System.Collections.Generic;

namespace MLAPI.Data
{
    /// <summary>
    /// A NetworkedClient
    /// </summary>
    public class NetworkedClient
    {
        /// <summary>
        /// The Id of the NetworkedClient
        /// </summary>
        public uint ClientId;
        /// <summary>
        /// The PlayerObject of the Client
        /// </summary>
        public NetworkedObject PlayerObject;
        /// <summary>
        /// The NetworkedObject's owned by this Client
        /// </summary>
        public List<NetworkedObject> OwnedObjects = new List<NetworkedObject>();
        /// <summary>
        /// The encryption key used for this client
        /// </summary>
        public byte[] AesKey;
    }
}
