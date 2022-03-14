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
        /// Returns the objects that are owned by this player
        /// You can also check owned objects by using <see cref="NetworkSpawnManager.GetClientOwnedObjects(ulong)"/>
        /// on both the client and server side.
        /// </summary>
        /// <returns>List of <see cref="NetworkObject"/>s owned by the client</returns>
        public List<NetworkObject> GetOwnedObjects()
        {
            if (PlayerObject != null && PlayerObject.NetworkManager != null && PlayerObject.NetworkManager.IsListening)
            {
                return PlayerObject.NetworkManager.SpawnManager.GetClientOwnedObjects(ClientId);
            }
            return null;
        }
    }
}
