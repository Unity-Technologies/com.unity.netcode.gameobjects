#if !DISABLE_CRYPTOGRAPHY
using MLAPI.Cryptography;
#endif

namespace MLAPI.Data
{
    /// <summary>
    /// A class representing a client that is currently in the process of connecting
    /// </summary>
    public class PendingClient
    {
        /// <summary>
        /// The ClientId of the client
        /// </summary>
        public uint ClientId;
        
#if !DISABLE_CRYPTOGRAPHY
        internal EllipticDiffieHellman KeyExchange;
#endif
        /// <summary>
        /// The current AesKey
        /// </summary>
        public byte[] AesKey;

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
            /// Client is in the process of doing the hail handshake
            /// </summary>
            PendingHail,
            /// <summary>
            /// Client is in the process of doing the connection handshake
            /// </summary>
            PendingConnection
        }
    }
}