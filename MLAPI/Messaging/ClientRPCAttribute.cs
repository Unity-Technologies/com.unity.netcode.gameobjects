using System;

namespace MLAPI.Messaging
{
    /// <summary>
    /// Attribute used on methods to me marked as ClientRPC
    /// ClientRPC methods can be requested from the server and will execute on a client
    /// Remember that a host is a server and a client
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ClientRPCAttribute : RPCAttribute
    {
        /// <summary>
        /// Whether or not the ClientRPC should only be run if the request comes straight from the server
        /// </summary>
        public bool AllowPassthrough = true;
    }
}
