using UnityEngine.Scripting.APIUpdating;

namespace Unity.Netcode
{
    /// <summary>
    /// Permission type
    /// </summary>
    [MovedFrom("MLAPI.NetworkVariable")]
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
