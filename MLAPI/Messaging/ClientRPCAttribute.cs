using System;

namespace MLAPI.Messaging
{
    /// <summary>
    /// Attribute used on methods to me marked as ClientRPC
    /// ClientRPC methods can be requested from the server and will execute on a client
    /// Remember that a host is a server and a client
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ClientRPCAttribute : Attribute
    {
        internal ReflectionMethod reflectionMethod;
#if ENABLE_IL2CPP
        internal System.Reflection.MethodInfo perfMethod;
#else
        internal RpcDelegate rpcDelegate;
#endif
  }
}
