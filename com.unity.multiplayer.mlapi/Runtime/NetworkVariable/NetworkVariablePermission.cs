namespace MLAPI.NetworkVariable
{
    /// <summary>
    /// Permission type
    /// </summary>
    public enum NetworkVariablePermission
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