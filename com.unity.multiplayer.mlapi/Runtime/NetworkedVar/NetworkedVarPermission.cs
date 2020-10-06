namespace MLAPI.NetworkedVar
{
    /// <summary>
    /// Permission type
    /// </summary>
    public enum NetworkedVarPermission
    {
        /// <summary>
        /// Everyone
        /// </summary>
        Everyone,
        /// <summary>
        /// Server-only operation
        /// </summary>
        ServerOnly,
        /// <summary>
        /// Owner-ownly
        /// </summary>
        OwnerOnly,
        /// <summary>
        /// Custom delegate
        /// </summary>
        Custom
    }
}