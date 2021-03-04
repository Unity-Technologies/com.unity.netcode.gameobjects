using System;

namespace MLAPI.Exceptions
{
    /// <summary>
    /// Exception thrown when a visibility change fails
    /// </summary>
    public class VisibilityChangeException : Exception
    {
        /// <summary>
        /// Constructs a VisibilityChangeException
        /// </summary>
        public VisibilityChangeException() { }

        /// <summary>
        /// Constructs a VisibilityChangeException with a message
        /// </summary>
        /// <param name="message">The exception message</param>
        public VisibilityChangeException(string message) : base(message) { }

        /// <summary>
        /// Constructs a VisibilityChangeException with a message and a inner exception
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="inner">The inner exception</param>
        public VisibilityChangeException(string message, Exception inner) : base(message, inner) { }
    }
}