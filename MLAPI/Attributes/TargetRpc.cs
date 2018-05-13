using System;

namespace MLAPI.Attributes
{
    /// <summary>
    /// This attribute is used to specify that this is a remote Target RPC
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpc : Attribute
    {
        /// <summary>
        /// The channelName to send this Target
        /// </summary>
        public string channelName = "MLAPI_INTERNAL";
    }
}
