using System;

namespace Unity.Netcode
{
    /// <summary>
    /// Exception thrown when the operation require NetworkManager to be listening.
    /// </summary>
    public class NotListeningException : Exception
    {
        /// <summary>
        /// Constructs a NotListeningException
        /// </summary>
        public NotListeningException() { }

        /// <summary>
        /// Constructs a NotListeningException with a message
        /// </summary>
        /// <param name="message">The exception message</param>
        public NotListeningException(string message) : base(message) { }

        /// <summary>
        /// Constructs a NotListeningException with a message and a inner exception
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="inner">The inner exception</param>
        public NotListeningException(string message, Exception inner) : base(message, inner) { }
    }
}
