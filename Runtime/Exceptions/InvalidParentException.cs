using System;

namespace Unity.Netcode
{
    /// <summary>
    /// Exception thrown when the new parent candidate of the NetworkObject is not valid
    /// </summary>
    public class InvalidParentException : Exception
    {
        /// <summary>
        /// Constructor for <see cref="InvalidParentException"/>
        /// </summary>
        public InvalidParentException() { }

        /// <inheritdoc/>
        /// <param name="message"></param>
        public InvalidParentException(string message) : base(message) { }

        /// <inheritdoc/>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidParentException(string message, Exception innerException) : base(message, innerException) { }
    }
}
