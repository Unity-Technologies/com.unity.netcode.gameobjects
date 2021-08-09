using System;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Netcode
{
    /// <summary>
    /// Exception thrown when the new parent candidate of the NetworkObject is not valid
    /// </summary>
    [MovedFrom("MLAPI.Exceptions")]
    public class InvalidParentException : Exception
    {
        public InvalidParentException() { }
        public InvalidParentException(string message) : base(message) { }
        public InvalidParentException(string message, Exception innerException) : base(message, innerException) { }
    }
}
