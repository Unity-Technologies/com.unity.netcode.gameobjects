namespace MLAPI.Transports.UNET
{
    /// <summary>
    /// Represents a ClientId structure
    /// </summary>
    public struct NetId
    {
        //The NetId uses a ushort as connectionId and byte as hostId. Explanation: https://blogs.unity3d.com/2014/06/11/all-about-the-unity-networking-transport-layer/#comment-167515


        /// <summary>
        /// The hostId this client is on
        /// </summary>
        public byte HostId;
        /// <summary>
        /// The connectionId this client is assigned
        /// </summary>
        public ushort ConnectionId;
        /// <summary>
        /// Meta data about hte client
        /// </summary>
        public byte Meta;

        /// <summary>
        /// Returns wheter or not the clientId represents a -1
        /// </summary>
        /// <returns><c>true</c>, if server, <c>false</c> otherwise.</returns>
        public bool IsServer()
        {
            return Meta == 1;
        }
        /// <summary>
        /// Initializes a new instance of the netId struct from transport values
        /// </summary>
        /// <param name="hostId">Host identifier.</param>
        /// <param name="connectionId">Connection identifier.</param>
        /// <param name="isServer">If set to <c>true</c> is isServer.</param>
        public NetId(byte hostId, ushort connectionId, bool isServer)
        {
            HostId = hostId;
            ConnectionId = connectionId;
			Meta = isServer ? (byte)1 : (byte)0;
        }
        /// <summary>
        /// Initializes a new instance of the netId struct from a clientId
        /// </summary>
        /// <param name="clientId">Client identifier.</param>
        public NetId(uint clientId)
        {
            HostId = (byte)(clientId & 0xFF);
            ConnectionId = (ushort)((byte)((clientId >> 8) & 0xFF) | (ushort)(((clientId >> 16) & 0xFF) << 8));
            Meta = (byte)((clientId >> 24) & 0xFF);
        }
        /// <summary>
        /// Gets the clientId.
        /// </summary>
        /// <returns>The client identifier.</returns>
        public uint GetClientId()
        {
            return HostId | (uint)((ConnectionId & 0xFF) << 8) | (uint)(((ConnectionId >> 8) & 0xFF) << 16) | (uint)(Meta << 24);
        }
        /// <summary>
        /// Checks if two NetId's are equal
        /// </summary>
        /// <param name="obj">NetId to compare to</param>
        /// <returns>Wheter or not the two NetIds are equal</returns>
        public override bool Equals (object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            NetId key = (NetId)obj;
            return (HostId == key.HostId) && (ConnectionId == key.ConnectionId);
        }
        /// <summary>
        /// Returns a hash code for the instance
        /// </summary>
        /// <returns>Returns a hash code for the instance</returns>
        public override int GetHashCode()
        {
            return (int)GetClientId();
        }
        /// <summary>
        /// Checks if two NetId's are equal
        /// </summary>
        /// <param name="client1">First netId</param>
        /// <param name="client2">Second netId</param>
        /// <returns>Wheter or not the two NetIds are equal</returns>
        public static bool operator ==(NetId client1, NetId client2)
        {
			return (client1.HostId == client2.HostId && client1.ConnectionId == client2.ConnectionId) || (client1.IsServer() == client2.IsServer());
        }
        /// <summary>
        /// Checks if two NetId's are not equal
        /// </summary>
        /// <param name="client1">First netId</param>
        /// <param name="client2">Second netId</param>
        /// <returns>Wheter or not the two NetIds are not equal</returns>
        public static bool operator !=(NetId client1, NetId client2)
        {
            return !(client1 == client2);
        }
    }
}
