namespace Unity.Netcode
{
    /// <summary>
    /// Permission type
    /// </summary>
    public enum NetworkVariableReadPermission
    {
        /// <summary>
        /// Everyone
        /// </summary>
        Everyone,

        /// <summary>
        /// Owner-ownly
        /// </summary>
        Owner,
    }

    public enum NetworkVariableWritePermission
    {
        Server,
        Owner
    }
}
