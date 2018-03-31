namespace MLAPI.Data
{
    /// <summary>
    /// A struct representing a client. Contains a hostId and a connectionId.
    /// </summary>
    internal struct ClientIdKey
    {
        /// <summary>
        /// The NetworkTransport hostId
        /// </summary>
        internal readonly int hostId;
        /// <summary>
        /// The NetworkTransport connectionId
        /// </summary>
        internal readonly int connectionId;

        /// <summary>
        /// Creates a new ClientIdKey
        /// </summary>
        /// <param name="hostId">The NetworkTransport hostId</param>
        /// <param name="connectionId">The NetworkTransport connectionId</param>
        internal ClientIdKey (int hostId, int connectionId)
        {
            this.hostId = hostId;
            this.connectionId = connectionId;
        }

        public override bool Equals (object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            ClientIdKey key = (ClientIdKey)obj;
            return (hostId == key.hostId) && (connectionId == key.hostId);
        }

        public override int GetHashCode()
        {
            return hostId ^ connectionId;
        }

        public static bool operator ==(ClientIdKey x, ClientIdKey y)
        {
            return x.hostId == y.hostId && x.connectionId == y.connectionId;
        }

        public static bool operator !=(ClientIdKey x, ClientIdKey y)
        {
            return !(x == y);
        }
    }
}
