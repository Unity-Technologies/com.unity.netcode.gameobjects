namespace Unity.Netcode
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
        /// The state of the connection process for the client
        /// </summary>
        public State ConnectionState { get; internal set; }

        /// <summary>
        /// The states of a connection
        /// </summary>
        public enum State
        {
            /// <summary>
            /// Waiting for client to send it's initial connection request
            /// </summary>
            PendingConnection,
            /// <summary>
            /// Waiting for client connection request to be approved
            /// </summary>
            PendingApproval
        }
    }
}
