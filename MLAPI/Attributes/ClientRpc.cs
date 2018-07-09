using System;

namespace MLAPI.Attributes
{
    /// <summary>
    /// This attribute is used to specify that this is a remote Client RPC
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpc : Attribute
    {
        /// <summary>
        /// The channelName to send this Rpc
        /// </summary>
        public string channelName { get; set; } = "MLAPI_INTERNAL";
    }
}
