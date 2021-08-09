using UnityEngine.Scripting.APIUpdating;

namespace Unity.Netcode
{
    /// <summary>
    /// Represents a netEvent when polling
    /// </summary>
    [MovedFrom("MLAPI.Transports")]
    public enum NetworkEvent
    {
        /// <summary>
        /// New data is received
        /// </summary>
        Data,

        /// <summary>
        /// A client is connected, or client connected to server
        /// </summary>
        Connect,

        /// <summary>
        /// A client disconnected, or client disconnected from server
        /// </summary>
        Disconnect,

        /// <summary>
        /// No new event
        /// </summary>
        Nothing
    }
}
