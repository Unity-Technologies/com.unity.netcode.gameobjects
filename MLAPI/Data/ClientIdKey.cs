namespace MLAPI.Data
{
    struct ClientIdKey
    {
        internal readonly int hostId;
        internal readonly int connectionId;

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
