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
        public ulong ClientId;

        /// <summary>
        /// The state of the connection process for the client
        /// </summary>
        public State ConnectionState;

        /// <summary>
        /// The states of a connection
        /// </summary>
        public enum State
        {
            /// <summary>
            /// Client is in the process of doing the connection handshake
            /// </summary>
            PendingConnection
        }
    }
}