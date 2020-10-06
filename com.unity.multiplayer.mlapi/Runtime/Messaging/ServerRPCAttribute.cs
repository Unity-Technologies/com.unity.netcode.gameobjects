using System;

namespace MLAPI.Messaging
{
    /// <summary>
    /// Attribute used on methods to me marked as ServerRPC
    /// ServerRPC methods can be requested from a client and will execute on the server
    /// Remember that a host is a server and a client
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ServerRPCAttribute : RPCAttribute
    {
        /// <summary>
        /// Whether or not the ServerRPC should only be run if executed by the owner of the object
        /// </summary>
        public bool RequireOwnership = true;
    }
}
