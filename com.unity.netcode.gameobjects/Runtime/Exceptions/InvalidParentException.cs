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

        /// <inheritdoc cref="Exception(string)"/>
        /// <param name="message">The message that describes the invalid parent operation</param>
        public InvalidParentException(string message) : base(message) { }

        /// <inheritdoc cref="Exception(string, Exception)"/>
        /// <param name="message">The message that describes the invalid parent operation</param>
        /// <param name="innerException">The exception that caused the current exception</param>
        public InvalidParentException(string message, Exception innerException) : base(message, innerException) { }
    }
}
