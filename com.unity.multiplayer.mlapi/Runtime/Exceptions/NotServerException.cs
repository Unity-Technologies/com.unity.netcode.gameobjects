using System;

namespace MLAPI.Exceptions
{
    /// <summary>
    /// Exception thrown when the operation can only be done on the server
    /// </summary>
    public class NotServerException : Exception
    {
        /// <summary>
        /// Constructs a NotServerException
        /// </summary>
        public NotServerException() { }

        /// <summary>
        /// Constructs a NotServerException with a message
        /// </summary>
        /// <param name="message">The exception message</param>
        public NotServerException(string message) : base(message) { }

        /// <summary>
        /// Constructs a NotServerException with a message and a inner exception
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="inner">The inner exception</param>
        public NotServerException(string message, Exception inner) : base(message, inner) { }
    }
}