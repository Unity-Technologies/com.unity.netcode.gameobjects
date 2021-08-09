using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Netcode
{
    /// <summary>
    /// A NetworkClient
    /// </summary>
    [MovedFrom("MLAPI.Connection")]
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
        public readonly List<NetworkObject> OwnedObjects = new List<NetworkObject>();
    }
}
