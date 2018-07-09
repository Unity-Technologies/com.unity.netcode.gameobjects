using System;

namespace MLAPI.Attributes
{
    /// <summary>
    /// This attribute is used to specify that this is a remote Command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class Command : Attribute
    {
        /// <summary>
        /// The channelName to send this Command
        /// </summary>
        public string channelName { get; set; } = "MLAPI_INTERNAL";
    }
}
