namespace Unity.Netcode
{
    /// <summary>
    /// The permission types for reading a var
    /// </summary>
    /// <remarks>
    /// Only relevant when using the client/server network topology.
    /// In distributed authority mode everyone can always read.
    /// </remarks>
    public enum NetworkVariableReadPermission
    {
        /// <summary>
        /// Everyone can read
        /// </summary>
        Everyone,
        /// <summary>
        /// Only the owner and the server can read
        /// </summary>
        Owner,
    }

    /// <summary>
    ///  The permission types for writing a var
    /// </summary>
    public enum NetworkVariableWritePermission
    {
        /// <summary>
        /// Only the server can write
        /// </summary>
        Server,
        /// <summary>
        /// Only the owner can write
        /// </summary>
        Owner
    }
}
