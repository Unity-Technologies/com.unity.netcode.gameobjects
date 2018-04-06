using System;
namespace MLAPI.Data
{
    /// <summary>
    /// Represents a MLAPI message type
    /// </summary>
    [Serializable]
    public class MessageType
    {
        /// <summary>
        /// The name of the messageType
        /// </summary>
        public string Name;
        /// <summary>
        /// Wheter or not the channel should have passthrough support.
        /// </summary>
        public bool Passthrough;
    }
}
