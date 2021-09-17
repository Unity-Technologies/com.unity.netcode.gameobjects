using System;

namespace Unity.Netcode
{
    /// <summary>
    /// Exception thrown when a change to a configuration is wrong
    /// </summary>
    public class NetworkConfigurationException : Exception
    {
        /// <summary>
        /// Constructs a NetworkConfigurationException
        /// </summary>
        public NetworkConfigurationException() { }

        /// <summary>
        /// Constructs a NetworkConfigurationException with a message
        /// </summary>
        /// <param name="message">The exception message</param>
        public NetworkConfigurationException(string message) : base(message) { }

        /// <summary>
        /// Constructs a NetworkConfigurationException with a message and a inner exception
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="inner">The inner exception</param>
        public NetworkConfigurationException(string message, Exception inner) : base(message, inner) { }
    }
}
