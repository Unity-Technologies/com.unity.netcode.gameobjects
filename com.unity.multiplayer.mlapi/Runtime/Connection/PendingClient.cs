namespace MLAPI.Connection
{
    /// <summary>
    /// A class representing a client that is currently in the process of connecting
    /// </summary>
    public class PendingClient
    {
        /// <summary>
        /// The ClientId of the client
        /// </summary>
        public ulong ClientId { get; internal set; }
        
        /// <summary>
        /// Whether or no the client has sent a connection request
        /// </summary>
        public bool HasSentConnectionRequest { get; internal set; }
    }
}